angular.module('app.reports')
	.directive('reportTimeSpanFilter', ['CurrentReport', 'UserSettings', function(CurrentReport, UserSettings) {
		return {
			restrict: 'AE',
			scope: {
				onChange: '&',
				onInit: '&'
			},
			templateUrl: 'Reports/_Directives/reportTimeSpanFilter.html',
			link: function(scope) {
				var timeFromFilterKey = 'timeFromFilter';
				var timeToFilterKey = 'timeToFilterKy';

				UserSettings.getReportFilters().then(function(filters) {
					var timeFromFilter = _.find(filters, function(setting) { return setting.Key === timeFromFilterKey; }) || {};
					var timeToFilter = _.find(filters, function(setting) { return setting.Key === timeToFilterKey; }) || {};

					scope.timeFrom = CurrentReport.Filter.timeFrom = CurrentReport.Filter.timeFrom || parseInt(timeFromFilter.Value) || 0;
					scope.timeTo = CurrentReport.Filter.timeTo = CurrentReport.Filter.timeToFilter || parseInt(timeToFilter.Value) || 24;

					scope.onInit();

					scope.$watch('timeFrom', function(newTime, oldTime) {
						if (!_.isUndefined(newTime) && newTime !== oldTime) {
							UserSettings.updateReportFilter(timeFromFilterKey, newTime);
							CurrentReport.Filter.timeFrom = newTime;
							scope.onChange();
						}
					});

					scope.$watch('timeTo', function(newTime, oldTime) {
						if (!_.isUndefined(newTime) && newTime !== oldTime) {
							UserSettings.updateReportFilter(timeToFilterKey, newTime);
							CurrentReport.Filter.timeTo = newTime;
							scope.onChange();
						}
					});
        });

			}
		};
	}]);


