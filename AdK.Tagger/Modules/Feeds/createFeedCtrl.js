angular.module('app')
    .controller('createFeedCtrl', ['$scope', '$location','$routeParams', 'Service',
        function ($scope, $location, $routeParams, Service) {
            var email_regex = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
            $scope.feed = { mailingList: []};
            $scope.showError = false;
            $scope.title = "New Alert";
            $scope.backLink = '#/client-feeds/view';
            var backPath = '/client-feeds/edit/';
            if ($routeParams.returnTo && $routeParams.returnTo == 'clients')
            {
                backPath = '/clients';
                $scope.backLink = '#/clients';
			}

			$scope.status = {
				opened: false
			};

			$scope.open = function ($event) {
				$scope.status.opened = true;
			};

            //$scope.getContacts = function (text) {
            //    return Service('SearchContacts', { text: text }).then(function (response) {
            //        return _.map(response, function (elem) {
            //            return { contact_id: elem.id, id: elem.id, email: elem.email, name: elem.name };
            //        });
            //    });
            //};


            $scope.save = function () {
                var data = createData();
                if (data) {
                    $scope.showError = false;
                    Service('CreateFeed', data).then(function success(response) {
                        if (response)
                        {
                            if ($routeParams.returnTo && $routeParams.returnTo == 'clients')
                                $location.path(backPath);
							else
								$location.path(backPath + response);
                        }
                            
                    }, function error(response) {
                        console.error('CreateFeed Error', response.status, response.data);
                    });
                } else {
                    $scope.showError = true;
                }
            };

            var createData = function () {
                if ($scope.feed.client) {
                    var obj = {
                        client: $scope.feed.client,
						includeMp3: !!$scope.feed.includeMp3,
						expirationDate: $scope.feed.ExpirationDate != null ? moment($scope.feed.ExpirationDate) .format('YYYY-MM-DD') : null
                    };
					                    
                    return obj;
                } else {
                    return null;
                }
			}

			$scope.expire = function () {
				$scope.feed.ExpirationDate = moment().toDate();
			}
        }]);
