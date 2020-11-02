angular.module('app')
	.factory('Authenticate', ['$q', 'LocalStorage', 'Service', 'Cookie', function ($q, LocalStorage, Service, Cookie) {
		var _a = {
			user: null,
			deferred: $q.defer(),
			promise: function () {
				return _a.deferred.promise;
			},
			verifyToken: function () {
				var deviceId = _a.getDeviceId();
				var token = _a.getToken();
				if (deviceId && token)
					Service('TestToken').then(_a.saveUser);
				else
					_a.deferred.notify({ authenticated: false });
			},
			authenticate: function (user) {
				_a.deferred.notify({ authFailed: false });
				Service('Authenticate', user).then(function (user) {
					if (user)
						_a.saveUser();
					else
						_a.deferred.notify({ authFailed: true });
				});
			},
			saveUser: function (user) {
				_a.user = user;
				if (user) {
					user.granted = user.granted.reduce(function (map, g) {
						map[g] = true;
						return map;
					}, {});
					moment.locale(user.locale);
				}

				LocalStorage.setJson('user', _a.user);
				_a.authenticated = !!_a.getToken();
				if (_a.authenticated) {
					_a.deferred.notify({ authenticated: true });
					_a.deferred.resolve(_a.user);
				}
			},
			getDeviceId: function () {
				return Cookie.get('deviceId');
			},
			getToken: function () {
				return Cookie.get('token');
			},
			createAccount: function (user) {
				Service('CreateAccount', { user: user }).then(function (value) {
					_a.deferred.notify({ authFailed: false, validationMailSent: value, userExist: !value });
				});
			},
			verifyEmail: function (emailToken) {
				return Service('VerifyEmail', { emailToken: emailToken }).then(function (user) {
					_a.deferred.notify({ emailValidated: !!user });
					if (user)
						_a.saveUser();
				});
			},
			logout: function () {
				return Service('LogOut').then(function () {
					_a.saveUser();
					window.location.href = 'login.html';
				});
			},
			lostPassword: function (email) {
				return Service('LostPassword', { email: email }).then(function (sent) {
					_a.deferred.notify({ recoveryLinkSent: sent });
					return sent;
				});
			},
			validationLink: function (email) {
				return Service('ResendValidationEmail', { email: email }).then(function (sent) {
					_a.deferred.notify({ validationMailSent: sent });
				});
			},
			changePassword: function (password, passwordToken) {
				return Service('ChangeRecoveredPassword', { password: password, passwordToken: passwordToken }).then(function (user) {
					_a.deferred.notify({ passwordValidated: !!user });
					if (user) {
						_a.saveUser();
					}
				});
			}
		};
		return _a;
	}])
	.service('Cookie', [function () {
		return {
			get: function (name) {
				var value = '; ' + document.cookie;
				var parts = value.split('; ' + name + '=');
				if (parts.length == 2) { return parts.pop().split(';').shift(); }
			}
		};
	}])
	.factory('UrlQuery', ['$window', function ($window) {
		function parseUrlQuery() {
			var obj = {};

			var pairs = $window.location.search.substring(1).split("&");
			for (var i in pairs) {
				if (pairs[i] === '') {
					continue;
				}

				var pair = pairs[i].split('=');
				obj[$window.decodeURIComponent(pair[0])] = $window.decodeURIComponent(pair[1]);
			}

			return obj;
		}
		return parseUrlQuery();
	}]);
