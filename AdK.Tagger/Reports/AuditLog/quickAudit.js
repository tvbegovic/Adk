angular.module('app.reports')
	.controller('quickAuditLogCtrl', ['$scope', '$rootScope', 'CurrentReport', 'AuditService',
		function ($scope, $rootScope, CurrentReport, AuditService) {

			AuditService.filter.showPartials = false;

			$scope.current = CurrentReport.Filter;

			$scope.getAudit = function () {
				var filters = {
					channels: $scope.selectedChannels,
					spots: $scope.selectedSpots,
					dateFrom: $scope.current.dateFrom,
					dateTo: $scope.current.dateTo
				};


				$rootScope.$broadcast('load-audit-report-table', filters);
				$rootScope.$broadcast('load-audit-details', filters);
			};

			$scope.toggleAuditDetails = function () {
				$scope.showDetails = !$scope.showDetails;
				$rootScope.$broadcast('audit-view-change');
			};

			$scope.print = function () {
				window.print();
			};

		}]);
