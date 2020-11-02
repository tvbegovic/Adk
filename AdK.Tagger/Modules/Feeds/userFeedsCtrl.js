angular.module('app')
	.controller('userFeedsCtrl', ['$scope', 'Service', '$attrs', 'Pager', 'ValueFormatter', '$routeParams', '$location', 'LocalStorage',
        function ($scope, Service, $attrs, Pager, ValueFormatter, $routeParams, $location, LocalStorage) {

            $scope.showUserFeed = function (feed) {
                $location.path('client-feeds/view/' + feed.Id + '/' + encodeURIComponent(feed.ClientName) + '/userFeeds');
            };

            $scope.showUserReports = function (feed) {
                $location.path('add-feed/reports/' + feed.Id + '/' + encodeURIComponent(feed.ClientName) + '/' + feed.Market + ' ' + feed.Domain);
            };

            function init()
            {
                var feedList = LocalStorage.getJson('userFeeds');
                if (feedList == null) {
                    Service('GetFeedsForCurrentUser').then(function (res) {
                        res.forEach(function (item) {
                            item.Timestamp = item.Timestamp ? moment(item.Timestamp).toDate() : null;
                            item.LastEmailSent = item.LastEmailSent ? moment(item.LastEmailSent).toDate() : null;
                        });
                        $scope.feedList = res;
                        LocalStorage.setJson('userFeeds', res);
                    });
                }
                else
                    $scope.feedList = feedList;
            }

            $scope.pager = new Pager();
                       
            $scope.rowComparator = function (row) {
                switch ($scope.sort.Current) {
                    case $scope.sort.Client:
                        val = row.ClientName;
                        break;
                    case $scope.sort.RegMarket:
                        val = row.Market || row.Domain;
                        break;
                    case $scope.sort.Email:
                        val = row.LastEmailSent;
                        break;
                    default:
                        val = row.LastEmailSent;
                        break;
                }
                return val; //_.map(val, function (item) { return '-' + item; });
            };
			
            $scope.sortBy = function (sorting) {
                if ($scope.sort.Current == sorting) {
                    $scope.sort.reverse = !$scope.sort.reverse;                    
                    $scope.sort.Current = sorting;
                } else {
                    $scope.sort.Current = sorting;
                }
            };

            $scope.sort = {
                Client: 0,
                RegMarket: 1,
                Email: 2,                
                Current: 0,
				reverse: false
            }

            //Init
            init();


        }]).directive('userFeeds', [function () {
            return {
                restrict: 'E',
                scope: {
                    //feedFilterId: '@',
                    //clientName: '@'
                },
                templateUrl: '/Modules/Feeds/userFeeds.html',
                controller: 'userFeedsCtrl'
            };

        }]);
