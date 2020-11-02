angular.module('app')
  .directive('loginForm', function () {
      return {
          restrict: 'AE',
          templateUrl: '/Modules/Security/loginForm.html',
		  controller: ['$scope', '$modal', 'Authenticate', 'UrlQuery', function ($scope, $modal, Authenticate, UrlQuery) {
              $scope.user = { email: '', password: '' };
              $scope.mode = { sign: 'in' };
			  $scope.authenticate = function () { Authenticate.authenticate($scope.user); };
			  $scope.forms = {};
              Authenticate.promise().then(function (user) {
                  $scope.authenticated = true;
                  $scope.user = user;
              }, null, function (notification) {
                  $scope.authFailed = notification.authFailed;
                  function endsWith(s, t) { return s.substr(s.length - t.length) === t; }

                  var isUserPage = endsWith(window.location.pathname, 'login.html');
                  if ((notification.authFailed || !notification.authenticated) && !isUserPage) {
                      if (UrlQuery['href'])
                          window.location.href = 'login.html?href=' + UrlQuery['href'];
                      else
                          window.location.href = 'login.html';
                  }
                  if (notification.authenticated && isUserPage) {
                      if (UrlQuery['href'])
                          window.location.href = 'index.html#/' + UrlQuery['href'];
                      else
                          window.location.href = 'index.html';
                  }
                  
                  $scope.passwordValidated = notification.passwordValidated;

                  if (notification.emailValidated) {
                      $scope.user = Authenticate.user;
                  }
              });

              Authenticate.verifyToken();

              if (UrlQuery.verification) {
                  Authenticate.verifyEmail(UrlQuery.verification);
              } if (UrlQuery.passwordRecovery) {
                  $scope.mode.sign = 'passwordRecovery';
              }

              $scope.lostPassword = function () {
                  return Authenticate.lostPassword($scope.user.email);
              };

              $scope.validationLink = function () {
                  return Authenticate.validationLink($scope.user.email);
              };

			  $scope.changePassword = function () {
				  if ($scope.forms.changepassform.$valid) {
					  $scope.passwordsdontmatch = $scope.user.password != $scope.user.password2;
					  if (!$scope.passwordsdontmatch) {
						  Authenticate.changePassword($scope.user.password, UrlQuery.passwordRecovery);
					  }
				  }
                  
              };

			  $scope.logout = Authenticate.logout;

			  $scope.signup = function () {
				  var modalInstance = $modal.open({
					  animation: false,					  
					  templateUrl: 'SignupModal.html',
					  controller: ['$scope', '$modalInstance', 'Authenticate',
						  function ($scope, $modalInstance, Authenticate) {
							  $scope.user = { email: '', password: '' };
							  $scope.forms = {};
							  $scope.createAccount = function () {
								  if ($scope.forms.signupform.$valid) {
									  Authenticate.createAccount($scope.user);
								  }								  
							  };
							  Authenticate.promise().then(function (user) {
								  $scope.authenticated = true;								  
								  $scope.user = user;
							  }, null, function (notification) {
								  $scope.authFailed = notification.authFailed;
								  $scope.validationMailSent = notification.validationMailSent;
								  $scope.userExist = notification.userExist;
								  });

							  $scope.cancel = function () {
								  $modalInstance.dismiss();
							  }
						  }
					  ]
				  });
				  modalInstance.result.then(function (data) {

				  });
			  }

			  $scope.lostPassword = function () {
				  var modalInstance = $modal.open({
					  animation: false,
					  templateUrl: 'ForgotPassModal.html',
					  controller: ['$scope', '$modalInstance', 'Authenticate',
						  function ($scope, $modalInstance, Authenticate) {
							  $scope.user = { email: '', password: '' };
							  $scope.changePassword = function () { Authenticate.createAccount($scope.user); };
							  $scope.forms = {};
							  Authenticate.promise().then(function (user) {
								  $scope.authenticated = true;
								  $scope.user = user;
							  }, null, function (notification) {
								  $scope.authFailed = notification.authFailed;
								  $scope.validationMailSent = notification.validationMailSent;
								  $scope.userExist = notification.userExist;
								  $scope.linkSent = notification.recoveryLinkSent;
							  });

							  $scope.cancel = function () {
								  $modalInstance.dismiss();
							  }

							  $scope.sendLink = function () {
								  if ($scope.forms.forgotpassform.$valid) {
									  Authenticate.lostPassword($scope.user.email);
								  }								  
							  };
						  }
					  ]
				  });
				  modalInstance.result.then(function (data) {

				  });
			  }
          }]
      };
  });
