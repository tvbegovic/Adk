angular.module('app')
	.controller('auth', ['$scope', 'Authenticate', 'UrlQuery', 'Service', function ($scope, Authenticate, UrlQuery, Service) {

		$scope.user = { email: '', password: '' };
		$scope.mode = { sign: 'in' };
		$scope.authenticate = function () { Authenticate.authenticate($scope.user); };
		$scope.createAccount = function () { Authenticate.createAccount($scope.user); };

		$scope.$on('navMenuExpandStateChanged', function (event, args) {
			$scope.$broadcast('navMenuExpandStateChanged', args);
		});

		//Get App version
		Service('GetRevisionNumber', null, { backgroundLoad: true }).then(function (response) {
			var revision = Number(response);
			$scope.appRevision = revision || undefined;
		});

		Service('GetSetting', { module: 'General', key: 'logo' }).then(function (response) {
			$scope.logo = response;
		});

		Authenticate.promise().then(function (user) {
			$scope.authenticated = true;
			$scope.user = user;
		}, null, function (notification) {
			$scope.authFailed = notification.authFailed;
			$scope.userExist = notification.userExist;
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

			$scope.validationMailSent = notification.validationMailSent;

			$scope.emailValidated = notification.emailValidated;
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

		

		$scope.validationLink = function () {
			return Authenticate.validationLink($scope.user.email);
		};
				

		$scope.logout = Authenticate.logout;


	}]);
