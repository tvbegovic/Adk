angular.module('app')
	.controller('dokazniceSettingsCtrl', ['$scope', 'Service', 'emailComposer',
		function ($scope, Service, emailComposer) {

			Service('GetDokazniceSettings').then(function (dokazniceSettings) {
				$scope.dokazniceSettings = dokazniceSettings;
			});

			$scope.editRegistrationEmail = function () {

				emailComposer.open(
					'Dokaznice Registration Email', null,
					$scope.dokazniceSettings.RegistrationMailSubject,
					$scope.dokazniceSettings.RegistrationMailBody,
					['verificationLink']
				).then(function (template) {

					$scope.dokazniceSettings.RegistrationMailSubject = template.subject;
					$scope.dokazniceSettings.RegistrationMailBody = template.body;

					$scope.saveSettings();

				});
			};


			$scope.saveSettings = function () {
				Service('SaveDokazniceSettings', { settings: $scope.dokazniceSettings }, { backgroundLoad: true })
					.catch(function () {
						$scope.showError = true;
					});
			};

		}]).directive('dokazniceSettings', [function () {
			return {
				restrict: 'E',
				scope: {
				},
				templateUrl: '/Modules/Settings/dokazniceSettings.html',
				controller: 'dokazniceSettingsCtrl'
			};
		}]);
