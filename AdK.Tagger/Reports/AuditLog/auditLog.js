angular.module('app.reports')
	.controller('auditLogCtrl', ['$scope', '$rootScope', '$modal', 'Service', 'CurrentReport', 'ValueFormatter', 'confirmPopup', 'Pager', 'AuditService',
		function ($scope, $rootScope, $modal, Service, CurrentReport, ValueFormatter, confirmPopup, Pager, AuditService) {

			$scope.pager = new Pager();

			$scope.current = CurrentReport.Filter;
			$scope.loading = true;
			$scope.showPartials = false;

			$scope.displayMode = {
				auditList: 1,
				createAudit: 2,
				auditReport: 3
			};

			$scope.display = $scope.displayMode.auditList;

			$scope.changeDisplay = function (display) {
				$scope.selectedChannels = [];
				$scope.selectedSpots = [];
				$scope.display = display;
				$scope.currentAuditId = null;
				$rootScope.$broadcast('clear-audit-report-table');
			};

			$scope.isAuditReportView = function () {
				return $scope.display === $scope.displayMode.auditReport;
			};

			$scope.saveAudit = function () {

				var audit = {
					DateFrom: ValueFormatter.getServerStringDateWithoutTime($scope.current.dateFrom),
					DateTo: ValueFormatter.getServerStringDateWithoutTime($scope.current.dateTo),
					AuditSongs: $scope.selectedSpots.map(function (spot) { return { SongId: spot.Guid, Title: spot.displayName }; }),
					AuditChannels: $scope.selectedChannels.map(function (channel) { return { ChannelId: channel.Id, Name: channel.Name, MatchThreshold: channel.MatchThreshold }; })
				};

				confirmPopup.open('Save audit', null, 'After audit is saved there is no more editing. Do you want to proceed?')
					.then(function () {
						$scope.loading = true;
						return Service('SaveAudit', { audit: audit }, { backgroundLoad: true });
					}).then(function (auditId) {
						getAudits();
						//restore JS date to selected one
						audit.DateFrom = $scope.current.dateFrom;
						audit.DateTo = $scope.current.dateTo;
						audit.Id = auditId;
						audit.AuditChannels.forEach(function (ac) { ac.AuditId = auditId; });
						$scope.showAuditReport(audit);
					});
			};

			$scope.deleteAudit = function (audit) {
				confirmPopup.open('Delete audit', null, 'Are you sure you want to delete audit?')
					.then(function () {
						return Service('DeleteAudit', { auditId: audit.Id }, { backgroundLoad: true });
					}).then(getAudits);
			};

			function getAudits() {
				$scope.loading = true;
				Service('GetUserAudits', null, { backgroundLoad: true })
					.then(function (audits) {
						_.each(audits, function (audit) {
							audit.DateFrom = ValueFormatter.netDateToJsDate(audit.DateFrom);
							audit.DateTo = ValueFormatter.netDateToJsDate(audit.DateTo);
						});

						$scope.audits = audits;

						$scope.pager.reset();
						$scope.pager.setItemCount(audits.length);
					}).finally(function () {
						$scope.loading = false;
					});

			} getAudits();


			$scope.showAuditReport = function (audit) {
				AuditService.filter.showPartials = false;
				$scope.currentAuditId = audit.Id;
				$scope.display = $scope.displayMode.auditReport;
				//POPULATE AUTOCOMPLETE
				$scope.selectedChannels = audit.AuditChannels.map(mapAuditChannelToChannel);
				$scope.selectedSpots = audit.AuditSongs.map(function (s) {
					return { Guid: s.SongId, displayName: s.Title };
				});

				$scope.current.dateFrom = audit.DateFrom;
				$scope.current.dateTo = audit.DateTo;

				$scope.runAuditLogReport();

			};

			$scope.runAuditLogReport = function () {

				$rootScope.$broadcast('load-audit-report-table', {
					channels: $scope.selectedChannels,
					spots: $scope.selectedSpots,
					dateFrom: $scope.current.dateFrom,
					dateTo: $scope.current.dateTo,
					auditId: $scope.currentAuditId
				});

			};

			$scope.toggleAuditDetails = function () {

				$scope.showDetails = !$scope.showDetails;
				var filter = {
					channels: $scope.selectedChannels,
					spots: $scope.selectedSpots,
					dateFrom: $scope.current.dateFrom,
					dateTo: $scope.current.dateTo,
					auditId: $scope.currentAuditId
				};

				if ($scope.showDetails) {
					$rootScope.$broadcast('load-audit-details', filter);
				} else {
					$rootScope.$broadcast('load-audit-report-table', filter);
				}

			};

			function mapAuditChannelToChannel(auditChannel) {
				return {
					Id: auditChannel.ChannelId,
					Name: auditChannel.Name,
					MatchThreshold: auditChannel.MatchThreshold,
					AuditChannelId: auditChannel.Id,
					AuditId: auditChannel.AuditId
				};
			}

			$scope.print = function () {
				window.print();
			};

		}]);
