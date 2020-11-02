angular.module('app.reports')
	.controller('auditDetailsCtrl', ['$scope', '$q', 'Service', 'ValueFormatter', '$timeout', 'channelThresholdService', 'AuditService',
		function ($scope, $q, Service, ValueFormatter, $timeout, channelThresholdService, AuditService) {
			$scope.message = { show: false };
			$scope.savedAudit = false;
			var filter = null;

			$scope.showMessage = function (template) {
				$scope.message.template = template;
				$scope.message.show = true;
			};

			$scope.hideMessage = function () {
				$scope.message.show = false;
			};

			$scope.$on('audit-view-change', function () {
				$scope.showPartials = AuditService.filter.showPartials;
			});

			//AUDIT REPORT
			$scope.$on('load-audit-details', function (event, args) {
				$scope.showPartials = AuditService.filter.showPartials;
				if (args.auditId) {
					$scope.savedAudit = true;
					channelThresholdService.getForAudit(args.auditId, args.channels).then(function (auditChannels) {
						$scope.thresholdChannels = auditChannels;
					});
				} else {
					$scope.thresholdChannels = args.channels;
				}

				if (args.channels && args.spots && args.dateFrom && args.dateTo) {
					filter = args;
					runAuditDetailsReport();
				}

			});

			function runAuditDetailsReport(silentLoading) {
				if (!silentLoading) {
					$scope.loading = true;
					$scope.auditReport = null;
				}

				$scope.hideMessage();

				$scope.dateRange = [];
				for (var d = moment(filter.dateFrom); !d.isAfter(filter.dateTo); d.add(1, 'day')) {
					$scope.dateRange.push(d.clone());
				}

				return Service('MediaHouseAuditDetails', {
					channelIds: filter.channels.map(function (channel) { return channel.Id; }),
					songIds: filter.spots.map(function (spot) { return spot.Guid; }),
					dateFrom: ValueFormatter.getServerStringDateWithoutTime(filter.dateFrom),
					dateTo: ValueFormatter.getServerStringDateWithoutTime(filter.dateTo)
				}, { backgroundLoad: true }).then(function (response) {
					var groups = response.Result;
					if (groups && groups.length) {

						groups.forEach(function (group) {
							var channel = filter.channels.filter(function (ch) { return ch.Id == group.ChannelId })[0];
							group.Channel = channel;
							group.Rows.forEach(function (item) {
								item.PlayTime = moment(item.PlayTime).toDate();

								var duration = Math.round(item.Duration * 10) / 10;
								item.Duration = Math.round(item.Duration);
								var start = Math.round(item.Start * 10) / 10;
								item.Start = ValueFormatter.roundWithDecimalPlaces(start, 1);
								var end = Math.round(item.End * 10) / 10;

								var endOffset = Math.round((duration - end) * 10) / 10;
								var played = item.Duration - (start + endOffset);
								item.End = ValueFormatter.roundWithDecimalPlaces(endOffset, 1);
								item.PercentageNum = played / item.Duration;
								item.Percentage = ValueFormatter.toPercentageString(item.PercentageNum * 100);
								item.Treshold = channel.MatchThreshold / 100;

								if (!item.Title) {
									var spot = filter.spots.filter(function (spot) {
										return spot.Guid == item.SongId;
									})[0];
									item.Title = spot ? spot.displayName || spot.Name || spot.fileName : '';
								}
							});
						});

						$scope.auditReport = groups;

					} else {
						$scope.showMessage('NoData');
					}
				}).catch(function () {
					$scope.showMessage('Error');
				}).finally(function () {
					$scope.loading = false;
				});
			}

			$scope.onChannelThresholdUpdate = function () {
				runAuditDetailsReport(true);
			};

			$scope.updateShowPartials = function (value) {
				$scope.showPartials = value;
				AuditService.filter.showPartials = value;
			};

			$scope.getVisibleChannelRows = function (allRows) {
				return allRows.filter(function (row) {
					return $scope.showPartials || (row.PercentageNum > row.Treshold);
				});
			};

		}]).directive('auditDetails', [function () {
			return {
				restrict: 'E',
				scope: {
				},
				templateUrl: '/Reports/AuditLog/auditDetails.html',
				controller: 'auditDetailsCtrl'
			};
		}]);
