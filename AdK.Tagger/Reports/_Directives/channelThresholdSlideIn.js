angular.module('app.reports')
	.directive('channelThresholdSlideIn', ['$q', '$timeout', 'Service', function ($q, $timeout, Service) {
		return {
			restrict: 'AE',
			scope: {
				editMode: '=',
				show: '=',
				channels: '=',
				onUpdate: '='
			},
			templateUrl: '/Reports/_Directives/channelThresholdSlideIn.html',
			link: function (scope) {
				scope.showError = false;

				scope.saveAuditChannelThreshold = function () {
					scope.showError = false;

					if (scope.channels.length) {

						//Validate that all channels have goot threshold set
						if (scope.channels.some(function (channel) {
							return !channel.MatchThreshold || channel.MatchThreshold < 50 || channel.MatchThreshold > 100;
						})) {
							return;
						}

						scope.inProgress = true;

						var promises = scope.channels.map(function (ch) {
							var request = {
								threshold: ch.MatchThreshold / 100
							};

							var serviceCall = 'UpsertAuditChannelThresholdById';

							if(ch.AuditChannelId) {
								request.auditChannelId = ch.AuditChannelId;
							} else {
								request.auditId = ch.AuditId;
								request.channelId = ch.Id;
								serviceCall = 'UpsertAuditChannelThreshold';
							}

							return Service(serviceCall, request, { backgroundLoad: true });
						});

						$q.all(promises)
							.then(function () {
								showSuccessUpdateIndicator();
								scope.onUpdate();
							}).catch(function () {
								scope.showError = true;
								$timeout(function () {
									scope.showError = false;
								}, 4000);
							}).finally(function () {
								scope.inProgress = false;
							});

					}

					function showSuccessUpdateIndicator() {
						scope.successUpdate = true;
						$timeout(function () {
							scope.successUpdate = false;
						}, 1500);
					}

				};
			}
		};
	}]);


