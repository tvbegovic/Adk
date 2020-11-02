angular.module('app')
	.controller('editFeedCtrl', ['$scope', 'Service', '$routeParams', 'confirmPopup', '$location', '$rootScope','cookieService',
    function ($scope, Service, $routeParams, confirmPopup, $location, $rootScope,cookieService) {
            var email_regex = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
            $scope.feed = {};
            $scope.showError = false;
            $scope.showGroups = true;
            $scope.showGroupForm = { show: false };
            $scope.showPreview = true;
            $scope.title = "Edit Alert";
            $scope.backLink = '#/client-feeds/view';
            var backPath = '/client-feeds/edit/';
            var returnTo = $routeParams.returnTo;
            if (returnTo == null)
                returnTo = cookieService.get('editFeedCtrl_returnTo');
            if (returnTo == 'clients') {
                backPath = '/clients';
                $scope.backLink = '#/clients';                
            }
            cookieService.put('editFeedCtrl_returnTo', returnTo);

            $scope.preview = function () {
                $scope.showPreview = !$scope.showPreview;
			};

			$scope.status = {
				opened: false
			};

			$scope.open = function ($event) {
				$scope.status.opened = true;
			};

            $scope.save = function () {
                var data = createData();
                if (data) {
                    $scope.showError = false;
                    Service('EditFeed', data).then(function success(response) {
                        $scope.previewClient = data.client;
                    }, function error(response) {
                        console.error('EditFeed Error', response.status, response.data);
                    });
                } else {
                    $scope.showError = true;
                }
            };

            $scope.createOrUpdateGroup = function (group, exclude) {
                if (!group) {
                    group = {
                        Exclude: exclude,
                        FeedFilterId: $scope.feed.id
                    }
                }
                $scope.groupToEdit = group;
                $scope.showGroupForm = { show: true };
            };

            $scope.deleteFilterGroup = function (filterGroup) {
                confirmPopup.open('Delete filter group', null, 'Are you sure you want to delete this filter group?')
                   .then(function () {
                       return Service('DeleteFilterGroup', { filterGroupId: filterGroup.Id })
											 	.then(function () {
														$rootScope.$broadcast('refresh-feed-results');
													 init();
												});
                   }).then(function () {
                       $scope.isVisible = false;
                   });
            };

            

            var createData = function () {
                if ($scope.feed.client) {
                    var obj = {
                        filterFeedId: $scope.feed.id,
                        client: $scope.feed.client,
						includeMp3: !!$scope.feed.includeMp3,
						expirationDate: $scope.feed.ExpirationDate != null ? moment($scope.feed.ExpirationDate).format('YYYY-MM-DD') : null
                    };

                                       
                    
                    return obj;
                } else {
                    return null;
                }
			};

			$scope.expire = function () {
				$scope.feed.ExpirationDate = moment().toDate();
			}

            function init() {
                if ($routeParams.id) {
                    Service('GetFeedFilter', { feedFilterId: $routeParams.id }).then(function success(response) {
                        $scope.feed = {
                            id: response.Id,
                            client: response.ClientName,
                            includeMp3: response.IncludeMp3,
							filterGroups: response.FilterGroups,
							ExpirationDate: response.expirationDate != null ? moment(response.expirationDate).toDate() : null
                        };
                        $scope.previewClient = response.ClientName;
                    }, function error(response) {
                        console.error('GetFeedFilter Error', response.status, response.data);
                        $location.path(backPath);
                    });
                } else {
                    $location.path(backPath);
                }
            };

						$scope.onGroupSave = function() {
							init();
							$rootScope.$broadcast('refresh-feed-results');
						};

            init();
        }]);
