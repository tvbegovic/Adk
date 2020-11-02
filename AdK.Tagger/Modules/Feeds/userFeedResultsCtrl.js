angular.module('app')
	.controller('userFeedResultsCtrl', ['$scope','$rootScope', 'Service', 'confirmPopup', 'focus', 'CachedApiCalls', '$attrs', 'Pager', 'ValueFormatter','$http', '$routeParams', '$location', 'cookieService','LocalStorage',
        function ($scope,$rootScope, Service, confirmPopup, focus, CachedApiCalls, $attrs, Pager, ValueFormatter,$http, $routeParams, $location, cookieService,LocalStorage) {

            var observeFirstTrigger = true;
            var watchGroupTriggerCounter = 0;

            var startDate = new Date((new Date()).setDate((new Date()).getDate() - 7));
            startDate.setHours(0);
            startDate.setMinutes(0);
            startDate.setSeconds(0);

            $scope.customDate = startDate;
            $scope.minDate = moment().subtract(30, 'days').toDate();

            $scope.feedFilter = {};

            $scope.pager = new Pager();

            //$scope.current = { spot: null, playing: null };

            $scope.feedList = [];

            $scope.keyChain = {
                //advertiserBrand: 'advertiserBrand',
                brand: 'brand',
                channel: 'channel',
                regionMarket: 'regionMarket',
                firstAiring: 'firstAiring'
            };

            $scope.isVisible = true;
            $scope.hourParts = [];
            $scope.selectedHourPart = { hours: 0, minutes: 0 };

            $scope.reportId = '';            


            $scope.backButton = function () {
                //history.go(-1);
                if ($scope.returnTo)
                {
                    if ($scope.returnTo == 'reports')
                        $location.path('client-feeds/reports');
                    if ($scope.returnTo == 'userFeeds')
                        $location.path('/add-feed/');
                }
            };

            function init() {

                if ($routeParams.reportId) {
                    $scope.reportId = $routeParams.reportId;
                }
                else
                    $routeParams.reportId = '';

                if ($routeParams.returnTo)
                {
                    $scope.returnTo = $routeParams.returnTo;
                    cookieService.put('userFeedResults_returnTo', $scope.returnTo);
                }
                else
                {
                    $scope.returnTo = cookieService.get('userFeedResults_returnTo');
                }

                if ($routeParams.name)
                {
                    $scope.clientName = decodeURIComponent($routeParams.name);
                    cookieService.put('userFeedResults_client', $scope.clientName);
                }
                else {
                    $scope.clientName = cookieService.get('userFeedResults_client');
                }            
                    
                    

                //console.log('init');
                for (var i = 0; i < 24; i++) {
                    for (var j = 0; j < 60; j += 1) {
                        $scope.hourParts.push({ hours: i, minutes: j });
                    }
                }

                if ($scope.reportId.length <= 0)
                {
                    var id = $routeParams.id != null ? $routeParams.id : null;

                    //Retrieve feed filter


                    Service('GetFeedFilterForCurrentUser', {
                        id: id
                    }).then(function (data) {
                        //console.log('GetFeedFilterForCurrentUser');
                        $scope.feedFilter = data;
                        $scope.feedFilterId = data.Id;
                        $scope.clientName = data.ClientName;
                        var date = {};
                        if ($scope.feedFilter.Timestamp) {
                            date = moment.max(moment($scope.feedFilter.Timestamp), moment().subtract(30, 'days')).toDate();
                        }
                        else {
                            date = startDate;
                        }
                        $scope.selectedHourPart = { hours: date.getHours(), minutes: date.getMinutes() };
                        //console.log('GetFeedFilter 2');
                        $scope.customDate = date;
                        $scope.getFeed();
                    });
                }
                else
                {
                    date = startDate;
                    $scope.selectedHourPart = { hours: date.getHours(), minutes: date.getMinutes() };
                    $scope.customDate = date;
                    $scope.getFeed();
                }
            }

            $scope.changeSelectedHourPart = function (hourPart) {
                $scope.selectedHourPart = hourPart;
                $scope.getFeed();
            }

            $scope.exportToExcel = function () {
                $rootScope.loading += 1;
                var req = {
                    feedFilterId: $scope.feedFilterId,
                    cutOffDate: $scope.customDate,
                    baseUrl: $location.$$protocol + '://' + location.host,
                    filter: $scope.filter
                };

                $http.post('/service.asmx/GenerateFeedResults', req, { responseType: 'arraybuffer' }
                ).success(function (response) {
                    var blob = new Blob([response], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
                    var link = document.createElement('a');
                    link.href = (window.URL || window.webkitURL).createObjectURL(blob);
                    //window.open(link.href, '_blank');
                    link.download = ('FeedResults.xlsx');
                    link.target = '_blank';
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }).finally(function () {
                    $rootScope.loading -= 1;
                });
            };

            $scope.getFeed = getFeed;
            function getFeed(keepPagerIndex) {
                //console.log('GetFeed');
                if (!keepPagerIndex)
                    $scope.pager.reset();

                if ($scope.customDate >= $scope.minDate) {
                    $scope.loadFeed($scope.pager.size, $scope.pager.index - 1)
                        .then(function (res) {
                            if (res && res.feed) {
                                $scope.feedList = res.feed;
                                $scope.pager.setItemCount(res.totalCount);
                            }
                        });
                }
                
            }

            $scope.loadFeed = function (pageSize, pageNum) {
                //console.log($scope.customDate);
                if ($scope.reportId.length <= 0)
                    return Service('GetFeed', {
                        feedFilterId: $scope.feedFilterId,
                        cutOffDate: moment($scope.customDate).format('YYYY-MM-DD HH:mm'),
                        pageSize: pageSize,
                        pageNum: pageNum,
                        sortColumn: $scope.filter.sort.column,
                        ascending: $scope.filter.sort.ascending,
                        filter: $scope.filter
                    });
                else
                    return Service('GetFeedForReportId', {
                        reportId: $scope.reportId,
                        pageSize: pageSize,
                        pageNum: pageNum,
                        sortColumn: $scope.filter.sort.column,
                        ascending: $scope.filter.sort.ascending,
                        filter: $scope.filter
                    });
            };

            $scope.filter = {
                //advertiserBrand: '',
                brand: '',
                channel: '',
                regionMarket: '',
                firstAiring: '',
                adTranscript: '',
                sort: {
                    column: $scope.keyChain.firstAiring,
                    ascending: false
                }
            };

            $scope.setSort = function (column) {
                //console.log('setSort');
                if ($scope.filter.sort.column == column) {
                    $scope.filter.sort.ascending = !$scope.filter.sort.ascending;
                } else {
                    $scope.filter.sort.column = column;
                    $scope.filter.sort.ascending = true;
                }
                $scope.getFeed();
            };

            $scope.setFocus = function (inputName) {
                focus(inputName);
            };

            $scope.$watchGroup(['pager.index', 'pager.size'], function () {
                watchGroupTriggerCounter++;
                //console.log('watchGroup', watchGroupTriggerCounter);
                if (watchGroupTriggerCounter < 3) {
                    return;
                }
                $scope.getFeed(true);
            });

            //Player
            //$scope.playPauseSong = function (feedItem, rowIndex) {
            //    $scope.player.playPauseSong(feedItem.Mp3Url, feedItem.Duration, rowIndex);
            //};

            //function resetPlayer() {
            //    if ($scope.player && $scope.player.reset) {
            //        $scope.player.reset();
            //    }
            //}

            $scope.openPlayback = function (feedItem) {
                LocalStorage.setJson('FeedPlaybackExtraInfo', {
                    Channels: feedItem.Channels,
                    Market: feedItem.Markets,
                    Region: feedItem.Regions
                });
                $location.path('feed-playback/' + feedItem.Mp3Id + '/' + $routeParams.reportId + '/' + $scope.feedFilterId + '/' + encodeURIComponent($scope.clientName) + '/userFeed');
            };

            //Init
            $scope.onDirectivesInit = function () {
                init();
            };


        }]).directive('userFeedResults', [function () {
            return {
                restrict: 'E',
                scope: {
                    //feedFilterId: '@',
                    //clientName: '@'
                },
                templateUrl: '/Modules/Feeds/userFeedResults.html',
                controller: 'userFeedResultsCtrl'
            };
        }]);
