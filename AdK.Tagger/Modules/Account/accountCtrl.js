angular.module('app')
	.controller('accountCtrl', ['$scope', '$timeout', '$q', 'Service', 'Authenticate', 'UserSettings',
		function ($scope, $timeout, $q, Service, Authenticate, UserSettings) {

			$scope.today = new Date();
			var defaults = ['defaultCategory', 'defaultBrand', 'defaultAdvertiser', 'groupEmptyAuditHours'];

			$scope.account = {
				formerPassword: '',
				newPassword: '',
				name: Authenticate.user.name,
				userDateFormat: ''
			};

			$scope.userSettings = {};

			defaults.forEach(function (key) {
				$scope.userSettings[key] = {};
			});

			UserSettings.getSettings('Defaults').then(function (defaults) {
				defaults = defaults || [];

				//don't modify original defaults
				defaults = angular.copy(defaults);

				setUserSettingsOriginalValue(defaults);

				defaults.forEach(function (def) {
					$scope.userSettings[def.Key] = def;
				});

			});

			function setUserSettingsOriginalValue(settings) {
				if (settings && settings.length) {
					settings.forEach(function (setting) {
						//Convert string to boolean value
						if (setting.Key === 'groupEmptyAuditHours') {
							setting.Value = setting.Value === true || setting.Value === 'True';
						}

						setting.originalValue = setting.Value;
					});
				}
			}

			$scope.account.userDateFormat = localStorage.UserDateFormat;

			$scope.changePassword = function () {
				$scope.passwordChanged = undefined;
				Service('ChangePassword', {
					formerPassword: $scope.account.formerPassword,
					newPassword: $scope.account.newPassword
				}).then(function (changed) {
					$scope.passwordChanged = changed;
				});
			};

			$scope.saveAccount = function () {
				$scope.savingChanges = true;
				$scope.errorSaving = false;

				localStorage.UserDateFormat = angular.toJson($scope.account.userDateFormat);

				var saveAccount = Service('SaveAccount', { account: $scope.account }, { backgroundLoad: true }).then(function (saved) {
					$scope.errorSaving = !saved;
					$scope.successUpdate = saved;
					$timeout(function () { $scope.successUpdate = false; }, 1500);
				});

				$q.all(
					saveAccount,
					saveDefaults()
				).catch(function () {
					$scope.errorSaving = true;
				}).finally(function () {
					$scope.savingChanges = false;
					UserSettings.refreshSettings();
				});
			};

			function saveDefaults() {
				var promises = [];

				defaults.forEach(function (defaultKey) {
					if ($scope.userSettings[defaultKey].originalValue !== $scope.userSettings[defaultKey].Value) {
						promises.push(updateSetting('Defaults', defaultKey, $scope.userSettings[defaultKey].Value).then(function () {
							$scope.userSettings[defaultKey].originalValue = $scope.userSettings[defaultKey].Value;
						}));
					}
				});

				if (promises.length) {
					return $q.all(promises);
				}

				return true;

			}

			function updateSetting(settingModule, key, value) {
				var request = {
					module: settingModule,
					key: key,
					value: value
				};

				return Service('UpdateUserSetting', request, { backgroundLoad: true });
			}
		}]);
