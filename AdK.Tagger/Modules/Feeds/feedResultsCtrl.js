angular.module('app')
	.controller('feedResultsCtrl', ['$scope', '$rootScope', 'Service', 'confirmPopup', 'focus', 'CachedApiCalls', '$attrs', 'Pager',
		'ValueFormatter', '$http', '$location', '$routeParams', '$modal', 'infoModal', 'LocalStorage', 'cookieService', 'Authenticate',
		function ($scope, $rootScope, Service, confirmPopup, focus, CachedApiCalls, $attrs, Pager, ValueFormatter, $http, $location, $routeParams, $modal, infoModal,
			LocalStorage, cookieService, Authenticate) {

            $scope.isPreview = $scope.isPreview == 'true';

            var watchGroupTriggerCounter = 0;

            var startDate = new Date((new Date()).setDate((new Date()).getDate() - 7));
            startDate.setHours(0);
            startDate.setMinutes(0);
            startDate.setSeconds(0);
            $scope.backButtonText = 'Back';
            var fromPublic = false;

            $scope.customDate = startDate;
            $scope.customTime = startDate;

            $scope.minDate = moment().subtract(30, 'days').toDate();
            $scope.loadForCurrentUser = $scope.loadForCurrentUser == 'true';

            $scope.feedFilter = null;

            $scope.pager = new Pager();

            
            if ($routeParams.reportId) {
                $scope.reportId = $routeParams.reportId;
            }
            else
                $scope.reportId = '';

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

			var cutOffTimeNewAds = null;
			var hasNewAds = false;

            for (var i = 0; i < 24; i++) {
                for (var j = 0; j < 60; j += 1) {
                    $scope.hourParts.push({ hours: i, minutes: j });
                }
            }

            function init() {
                Service('IsUserGrantedClientFeeds').then(function (data) {
                    

                    $scope.IsClientFeed = data;
                    if ($scope.IsClientFeed && $scope.reportId.length <= 0 && !$scope.loadForCurrentUser) {
                        //Retrieve feed filter
                        if ($scope.feedFilterId)
                        {
                            Service('GetFeedFilter', {
                                feedFilterId: $scope.feedFilterId
                            }).then(function (data) {
                                //console.log('GetFeedFilter', $scope.feedFilterId);
                                $scope.feedFilter = data;
                                processFeed();
                            });
                        }
                        else if ($scope.feedFilter)
                        {
                            processFeed();
                        }
                    }
                    else {
                        //ad feed for current user
                        
                        if ($routeParams.returnTo) {
                            $scope.returnTo = $routeParams.returnTo;
                            cookieService.put('FeedResults_returnTo', $scope.returnTo);
                        }
                        else {
                            $scope.returnTo = cookieService.get('FeedResults_returnTo');
                        }

                        if ($routeParams.name) {
                            $scope.clientName = decodeURIComponent($routeParams.name);
                            cookieService.put('FeedResults_client', $scope.clientName);
                        }
                        else {
                            $scope.clientName = cookieService.get('FeedResults_client');
                        }

                        if ($scope.reportId.length <= 0) {
                            var id = $routeParams.id != null ? $routeParams.id : null;

                            //Retrieve feed filter


                            Service('GetFeedFilterForCurrentUser', {
                                id: id
                            }).then(function (data) {
                                //console.log('GetFeedFilterForCurrentUser');
								if (data != null)
								{
									$scope.feedFilter = data;
									$scope.feedFilterId = data.Id;
									$scope.clientName = data.ClientName;
									processFeed();
								}
									
                                
                                //var date = {};
                                //if ($scope.feedFilter.Timestamp) {
                                //    date = moment.max(moment($scope.feedFilter.Timestamp), moment().subtract(30, 'days')).toDate();
                                //}
                                //else {
                                //    date = startDate;
                                //}
                                //$scope.selectedHourPart = { hours: date.getHours(), minutes: date.getMinutes() };
                                ////console.log('GetFeedFilter 2');
                                //$scope.customDate = date;
                                //$scope.getFeed();
                            });
                        }
                        else {
                            if ($scope.$parent.user == null)
                            {
								//public site
                                if (LocalStorage.getJson('user') != null)
                                {
                                    fromPublic = true;
                                    $scope.showBackButton = true;
                                    $scope.backButtonText = 'Back to report list';
                                    cookieService.put('FeedResults_returnTo', 'reports_frompublic');
                                }
                                    
                            }
                            
							date = startDate;
                            $scope.selectedHourPart = { hours: date.getHours(), minutes: date.getMinutes() };
                            $scope.customDate = date;
                            $scope.getFeed();
                            
                            
                        }

                    }
                });
                
                
                
            }

            function processFeed()
            {
                var date = {};
                if ($scope.feedFilter.Timestamp) {
                    date = moment.max(moment($scope.feedFilter.Timestamp), moment().subtract(30, 'days')).toDate();
                }
                else {
                    date = startDate;
                }
                if ($scope.feedFilter.LastEmailSent)
                    $scope.feedFilter.LastEmailSent = moment($scope.feedFilter.LastEmailSent).toDate();
                else
					$scope.feedFilter.LastEmailSent = 'Never';
				if ($scope.feedFilter.userTimeNow)
					cutOffTimeNewAds = moment($scope.feedFilter.userTimeNow).toDate();

                $scope.selectedHourPart = { hours: date.getHours(), minutes: date.getMinutes() };
                //console.log('GetFeedFilter 2', $scope.feedFilterId);
                var dateChanged = !moment($scope.customDate).isSame(date);
                $scope.customDate = date;
                $scope.customTime = date;
                //if ($scope.isPreview == 'true') {
                //    $scope.getFeed();
                //}
                if (!dateChanged)
                    $scope.getFeed();
            }

            $scope.changeSelectedHourPart = function (hourPart) {
                $scope.selectedHourPart = hourPart;
                $scope.getFeed();
            }

            $scope.showResults = function () {
                var date = moment($scope.customDate).startOf('day');
                var time = moment($scope.customTime);
                date = date.add(time.hour(), 'hours').add(time.minute(), 'minutes').toDate();
                $scope.customDate = date;
            };

            $scope.setNowAsTimeStamp = function () {
                return Service('SetNowAsFeedFilterTimestamp', {
                    feedFilterId: $scope.feedFilterId
                }).then(function (data) {
                    $scope.selectedHourPart = { hours: (new Date()).getHours(), minutes: (new Date()).getMinutes() };
                    $scope.customDate = new Date();
                    //$scope.getFeed();
                });
            }

            $scope.exportToExcel = function () {
                $rootScope.loading += 1;
                var req = {
                    feedFilterId: $scope.feedFilterId,
                    cutOffDate: ValueFormatter.getServerStringDateWithoutTime($scope.customDate) + ' ' + $scope.selectedHourPart.hours + ':' + $scope.selectedHourPart.minutes,
                    baseUrl: $location.$$protocol + '://' + location.host,
                    filter: $scope.filter
				};

				var fileName = $scope.feedFilter.ClientName;
				var illegalChars = ['\"', '<', '>', '|', '\0', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006',
					'\b', '\t', '\n', '\v', '\f', '\r', '\u000e', '\u000f', '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015',
					'\u0016', '\u0017', '\u0018', '\u0019', '\u001a', '\u001b', '\u001c', '\u001d', '\u001e', '\u001f', ':', '*', '?', '\\', '/'];
				fileName = fileName.replace(new RegExp('[' + illegalChars.join('') + ']','g'), '_');

                $http.post('/service.asmx/GenerateFeedResults', req, { responseType: 'arraybuffer' }
                    ).success(function (response) {
                        var blob = new Blob([response], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
                        var link = document.createElement('a');
                        link.href = (window.URL || window.webkitURL).createObjectURL(blob);
                        //window.open(link.href, '_blank');
                        link.download = (fileName + '.xlsx');
                        link.target = '_blank';
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                    }).finally(function () {
                        $rootScope.loading -= 1;
                    });
            };

			$scope.sendEmail = function () {

				Service('GetFeed',
					{
						feedFilterId: $scope.feedFilterId,
						cutOffDate: cutOffTimeNewAds,
						pageSize: 0,
						pageNum: 0,
						sortColumn: '',
						ascending: true,
						filter: $scope.filter,
						checkForNewAds: true
					})
					.then(function (data) {
						hasNewAds = data.totalCount > 0;
						if (data.totalCount > 0) {
							handleNewAds({
								feedFilterId: $scope.feedFilterId,
								ads: data.feed,
								timeOfCheck: moment(data.nowInLocalTime).toDate(),
								feedList: $scope.feedList,
								filter: $scope.filter
							}).result.then(function (result) {
								if (result == 'refresh') {
									$scope.getFeed();
								}
							});

						}
						else {
							sendFeedMail(null);
						}
					});

			};
			
			function sendFeedMail(dateTo) {
				var mailingList = _.map($scope.feedFilter.MailingList, function (elem) {
					var text = elem.name;
					if (text.length > 0)
						text += ', ';
					if (elem.Client != null)
						text += elem.Client.name;
					var brackets = false;
					if (text.length > 0) {
						text += ' (';
						brackets = true;
					}
					text += elem.email;
					if (brackets)
						text += ')';
					return text;
				}).join(', ');
				Service('SendFeedEmail', {
						feedFilterId: $scope.feedFilterId,
						songs: _.map($scope.feedList, function (f) {
							return { SongId: f.SongId, SongExcluded: f.SongExcluded }
						}),
						dateTo: dateTo
					}
				).then(function (data) {
					infoModal.open('Email notification', 'Email is on its way to: ' + mailingList);
					//refresh feed
					$scope.customDate = moment(data.nowInLocalTime).toDate();
					$scope.getFeed();

				}).catch(function () {
					infoModal.open('Email notification', 'Error sending e-mail. E-mail has NOT been sent.');
				});
			}
			
			function handleNewAds(params) {
				return $modal.open({
					animation: false,
					templateUrl: 'tplNewAds.html',
					controller: ['$scope', '$modalInstance', 'params', function ($scope, $modalInstance, params) {
						$scope.params = params;
						$scope.send = function () {
							sendFeedMail(params.timeOfCheck);
							$modalInstance.close();
						};

						$scope.review = function () {
							
							//Set last check time as new cutoff in case customer again tries to send and there are new ads after that
							cutOffTimeNewAds = params.timeOfCheck;
							$modalInstance.close('refresh');
						};

						$scope.cancel = function () {
							$modalInstance.close();
						};
					}],
					resolve: {
						params: function () {
							return params;
						}
					}
				});
			}

            $scope.getFeed = getFeed;
            function getFeed(keepPagerIndex) {
                if (!keepPagerIndex)
                    $scope.pager.reset();

                if ($scope.customDate >= $scope.minDate)
                {
                    $scope.loadFeed($scope.pager.size, $scope.pager.index - 1)
                        .then(function (res) {
                            if (res && res.feed) {
								$scope.feedList = _.filter(res.feed, function (f) {
									return $scope.IsClientFeed || $routeParams.reportId ? true : f.SongExcluded != true;
								});
								
								$scope.pager.setItemCount(res.totalCount);
								if ($scope.reportId)
									$scope.clientName = res.feedFilter.ClientName;
                                if (fromPublic && res.feedFilter)
                                {
                                    var obj = { clientName: res.feedFilter.ClientName, countryMarket: '' };
                                    if (res.feedFilter.FilterGroups.length > 0)
                                    {
                                        res.feedFilter.FilterGroups.forEach(function (fg) {
                                            if (fg.FeedFilterRulesDomains != null && fg.FeedFilterRulesDomains.length > 0)
                                            {
                                                if (obj.countryMarket.length > 0)
                                                    obj.countryMarket += ',';
                                                obj.countryMarket += _.map(fg.FeedFilterRulesDomains,'DisplayName').join(',');
                                            }
                                                
                                        });
                                    }
                                    obj.id = res.feedFilter.Id;
                                    cookieService.put('userFeedReportsLastParams',JSON.stringify(obj));
                                }
                            }
                        });
                }
                
            }

            $scope.loadFeed = function (pageSize, pageNum) {
                if ($scope.reportId.length <= 0)
                {
                    if ($scope.isPreview == 'true') {
                        pageSize = 10;
                        pageNum = 0;
                    }
                    return Service('GetFeed', {
                        feedFilterId: $scope.feedFilterId,
                        cutOffDate: moment($scope.customDate).format('YYYY-MM-DD HH:mm'),
                        pageSize: pageSize,
                        pageNum: pageNum,
                        sortColumn: $scope.filter.sort.column,
                        ascending: $scope.filter.sort.ascending,
						filter: $scope.filter,
						checkForNewAds: false
                    });
                }
                    
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
                if ($scope.isPreview == 'true')
                    return;
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

            $scope.openPlayback = function (feedItem) {
                LocalStorage.setJson('FeedPlaybackExtraInfo', {
                    Channels: feedItem.Channels,
                    Market: feedItem.Markets,
                    Region: feedItem.Regions
                });
                var reportId = $routeParams.reportId && $routeParams.reportId.length > 0 ? $routeParams.reportId : '0';
                $location.path('feed-playback/' + feedItem.Mp3Id + '/' + reportId + '/' + $scope.feedFilterId + '/' + encodeURIComponent($scope.clientName) + '/' + ($scope.IsClientFeed ? 'clientFeeds' : 'adFeed'));
			};

			$scope.setActive = function (f, value) {
				if (value)
					$scope.active = f;
				else
					$scope.active = {};
			};

			$scope.exclude = function (f) {
				f.SongExcluded = true;
				updateSong(f);
			};

			$scope.include = function (f) {
				f.SongExcluded = false;
				updateSong(f);
			};

			function updateSong(f)
			{			
				Service('UpdateSongFeedStatus', {
					feedFilterId: $scope.feedFilterId,
					songs: [{ SongId: f.SongId, SongExcluded: f.SongExcluded }]
				}, { backgroundLoad: true });
			}

			$scope.active = {};

			$scope.shouldDisableSendMailButton = function () {
				return _.filter($scope.feedList, function (f) { return f.SongExcluded != true; }).length == 0 || ($scope.feedFilter == null) || $scope.feedFilter.MailingList.length == 0;
			};

			$scope.shouldDisableExcelButton = function () {
				return _.filter($scope.feedList, function (f) { return f.SongExcluded != true; }).length == 0 || ($scope.feedFilter == null);
			};


            //Init
            $scope.onDirectivesInit = function () {
                init();

                $attrs.$observe('feedFilterId', function (newValue) {
                    if (newValue) {
                        init();
                    }
                });
				
                $attrs.$observe('feedFilter', function (newValue) {
                    if (newValue) {
                        $scope.feedFilter = JSON.parse(newValue);
                        $scope.feedFilterId = $scope.feedFilter.id;
                        init();
                    }
                });
            };

            $scope.$on('refresh-feed-results', init);

        }]).directive('feedResults', [function () {
            return {
                restrict: 'E',
                scope: {
                    feedFilterId: '@',
                    clientName: '@',
                    isPreview: '@',
					loadForCurrentUser: '@',
                    backButton: '=?'
                },
                templateUrl: '/Modules/Feeds/feedResults.html',
                controller: 'feedResultsCtrl'
            };
        }]);
