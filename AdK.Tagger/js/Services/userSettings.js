angular.module('app')
	.factory('UserSettings', ['$q', '$timeout', 'Service', function ($q, $timeout, Service) {
		var settingsLoaded = false;
		//moudule in user setting should be same as UserSettingModule enum on server
		var userSettings;

		function defaultSettings() {
			return {
				ReportFilter: {
					module: 'ReportFilter',
					values: []
				},
				Pagination: {
					module: 'Pagination',
					values: []
				},
				UserDateFormat: {
					module: 'UserDateFormat',
					values: []
				},
				Defaults: {
					module: 'Defaults',
					values: []
				}
			}
		}

		function getUserSettings() {
			settingsLoaded = false;
			userSettings = defaultSettings();

			Service('GetUserSettings', null, { backgroundLoad: true }).then(function (settings) {
				if (settings) {
					settings.forEach(function (setting) {
						if (userSettings[setting.Module]) {
							userSettings[setting.Module].values.push(setting);
						}
					});
				}
				settingsLoaded = true;
			}).catch(function () {
				settingsLoaded = true;
			});
		}

		function waitForSettings() {
			var deferred = $q.defer();
			resolveDefferedOnSettingLoaded(deferred);
			return deferred.promise;
		}

		getUserSettings();

		function resolveDefferedOnSettingLoaded(deferred) {
			if (settingsLoaded) {
				deferred.resolve();
				return;
			}

			$timeout(function () {
				resolveDefferedOnSettingLoaded(deferred);
			}, 1);
		}


		return {
			getSettings: function (module, key) {
				var deferred = $q.defer();
				waitForSettings().then(function () {
					var response = {};
					if (key && userSettings[module]) {
						response = _.find(userSettings[module].values, function (setting) {
							return setting.Key === key;
						});
					} else if (userSettings[module]) {
						response = userSettings[module].values;
					}

					deferred.resolve(response);
				});
				return deferred.promise;
			},
			updateSettings: function (module, key, value) {
				if (userSettings[module]) {
					var request = {
						module: userSettings[module].module,
						key: key,
						value: value
					};

					Service('UpdateUserSetting', request);
				}
			},
			refreshSettings: getUserSettings,
			getReportFilters: function (key) {
				var deferred = $q.defer();
				waitForSettings().then(function () {
					var response = {};
					if (key) {
						response = _.find(userSettings.ReportFilter.values, function (setting) {
							return setting.Key === key;
						});
					} else {
						response = userSettings.ReportFilter.values;
					}

					deferred.resolve(response);
				});
				return deferred.promise;
			},
			updateReportFilter: function (key, value) {
				var request = {
					module: userSettings.ReportFilter.module,
					key: key,
					value: value
				};

				Service('UpdateUserSetting', request);
			}
		};

	}]);
