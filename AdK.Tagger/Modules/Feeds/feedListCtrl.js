angular.module('app')
	.controller('feedListCtrl', ['$scope', 'Service', 'Pager', 'confirmPopup','cookieService',
        function ($scope, Service, Pager, confirmPopup,cookieService) {

            $scope.pager = new Pager();
            cookieService.put('editFeedCtrl_returnTo', '');

            //Sorting
            $scope.sortBy = function (sorting) {
                if ($scope.sort.Current == sorting) {
                    $scope.sort.Direction = $scope.sort.Direction == 'ASC' ? 'DESC' : 'ASC';
                    $scope.sort.Current = sorting;
                } else {
                    $scope.sort.Current = sorting;
                }
			};

			$scope.hasMaster = false;
			$scope.master = null;

            $scope.sort = {
                Client: 0,
                RegMarket: 1,
                Email: 2,
                LastTimestamp: 3,
                Current: 0,
                Direction: 'ASC'
            }

            $scope.rowComparator = function (row) {
                switch ($scope.sort.Current) {
                    case $scope.sort.Client:
                        val = row.Client;
                        break;
                    case $scope.sort.RegMarket:
                        val = row.Market || row.Domain;
                        break;
                    case $scope.sort.Email:
                        val = row.EmailSent;
                        break;
                    default:
                        val = row.LastTimestamp;
                        break;
                }
                return $scope.sort.Direction == 'ASC' ? val : -val;
			};

			$scope.rowFilter = function (row) {
				return row.Id > 0;
			}


            $scope.deleteFeed = function (feed) {
                confirmPopup.open('Delete feed', null, 'Are you sure you want to delete this feed? You will also lose all feed\'s reports')
					.then(function () {
					    Service('DeleteFeed', { id: feed.Id }).then(function success(response) {
					        var index = $scope.feedList.indexOf(feed);
					        $scope.feedList.splice(index, 1);
					    }, function error(response) {
					        console.error('DeleteFeed Error', response.status, response.data);
					    });
					});
               
            }

            $scope.feedRowClass = function (item) {
                if (moment(item.LastTimestamp).isSame($scope.NowInLocalTime, 'day'))
                    return 'row-today';
                return '';
			};

			$scope.feedRowTextClass = function (item) {
				if (item.expired) {
					return 'feed-expired';
				}
				return '';
			}

            var init = function () {
                Service('GetFeeds', { checkNew: true}).then(function success(response) {
                    $scope.NowInLocalTime = moment(response.NowInLocalTime).toDate();
                    $scope.feedList = response.feeds.map(function (item) {
                        item.LastTimestamp = item.LastTimestamp ? moment(item.LastTimestamp).toDate() : null;
						item.EmailSent = item.EmailSent ? moment(item.EmailSent).toDate() : null;
						item.expired = item.ExpirationDate != null ? moment(item.ExpirationDate).isBefore(moment()) : false;
                        return item;
					});
					$scope.master = $scope.feedList.find(f => f.Id == 0);
					

					/*if ($scope.feedList.length > 0)
						getFeedCount();*/

                    $scope.pager.reset();
                    $scope.pager.setItemCount($scope.feedList ? $scope.feedList.length : 0);

                }, function error(response) {
                    console.error('GetFeeds Error', response.status, response.data);
                });
            }
			init();

			/*function getFeedCount() {
				if (countIndex < $scope.feedList.length) {
					Service('GetCountForFeed', { feedId: $scope.feedList[countIndex].Id, lastTimeStamp: $scope.feedList[countIndex].LastTimestamp },
						{ backgroundLoad: true }).then(function (response) {
						$scope.feedList[countIndex].NewMatchCount = response;
						countIndex++;
						getFeedCount();
					},
						function (error) {
							$scope.feedList[countIndex].NewMatchCount = -1;
							countIndex++;
							getFeedCount();
						})
				}
			}*/

        }]).directive('feedList', [function () {
            return {
                restrict: 'E',
                scope: {
                    showFeed: '='
                },
                templateUrl: '/Modules/Feeds/feedList.html',
                controller: 'feedListCtrl'
            };
        }]);
