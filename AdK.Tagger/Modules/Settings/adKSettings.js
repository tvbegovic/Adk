angular.module('app')
	.controller('adKSettingsCtrl', ['$scope', 'Service', 'emailComposer',
		function ($scope, Service, emailComposer) {

			Service('GetAdKSettings').then(function (adKontrolSettings) {
				$scope.adKontrolSettings = adKontrolSettings;
			});

			$scope.editRegistrationEmail = function () {

				emailComposer.open(
					'Adkontrol Registration Email', null,
					$scope.adKontrolSettings.RegistrationMailSubject,
					$scope.adKontrolSettings.RegistrationMailBody,
					['verificationLink']
				).then(function (template) {

					$scope.adKontrolSettings.RegistrationMailSubject = template.subject;
					$scope.adKontrolSettings.RegistrationMailBody = template.body;

					$scope.saveSettings();

				});
			};


			$scope.saveSettings = function () {
				Service('SaveAdKontrolSettings', { settings: $scope.adKontrolSettings }, { backgroundLoad: true })
					.catch(function () {
						$scope.showError = true;
					});
			};
		}]).directive('adKSettings', [function () {
			return {
				restrict: 'E',
				scope: {
				},
				templateUrl: '/Modules/Settings/adKSettings.html',
				controller: 'adKSettingsCtrl'
			};
		}]);
