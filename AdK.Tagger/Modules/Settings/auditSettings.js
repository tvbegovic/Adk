angular.module('app')
	.controller('auditSettingsCtrl', ['$scope', 'Service', 'emailComposer',
		function ($scope, Service, emailComposer) {
			$scope.showError = false;

			Service('GetAuditSettings').then(function (auditSettings) {
				$scope.auditSettings = auditSettings;
			});

			$scope.editSpotScanNotificationEmail = function () {

				emailComposer.open(
					'Spot scan mail', null,
					$scope.auditSettings.SpotScanMailSubject,
					$scope.auditSettings.SpotScanMailBody,
					['numberOfScannedSpots']
				).then(function (template) {
					$scope.auditSettings.SpotScanMailSubject = template.subject;
					$scope.auditSettings.SpotScanMailBody = template.body;

					$scope.saveSettings();

				});
			};


			$scope.saveSettings = function() {
				Service('SaveAuditSettings', {settings: $scope.auditSettings }, { backgroundLoad: true })
					.catch(function () {
						$scope.showError = true;
					});
			}

		}]).directive('auditSettings', [function () {
			return {
				restrict: 'E',
				scope: {
				},
				templateUrl: '/Modules/Settings/auditSettings.html',
				controller: 'auditSettingsCtrl'
			};
		}]);
