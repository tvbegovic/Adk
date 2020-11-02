angular.module('app.reports')
	.directive('reportCustomDateFilter', ['CurrentReport', 'UserSettings', function(CurrentReport, UserSettings) {
		return {
			restrict: 'AE',
			scope: {
				onChange: '&',
				onInit: '&'
			},
			templateUrl: 'Reports/_Directives/reportCustomDateFilter.html',
			link: function(scope) {
				var filterKey = 'customDateFilter';

				scope.dateOptions = {
					format: localStorage.UserDateFormat,
					maxDate: new Date(),
					dropDownOpened: false
				};

				UserSettings.getReportFilters(filterKey).then(function(lastFilterValue) {

					var customDate = null;

					if (CurrentReport.Filter.customDate || lastFilterValue) {
						customDate = CurrentReport.Filter.customDate || new Date(lastFilterValue.Value);
					} else {
						//set default custom date to yesterday
						customDate = new Date();
						customDate.setDate(customDate.getDate() - 1);
					}

					scope.customDate = CurrentReport.Filter.customDate = customDate;
					scope.onInit();

					scope.openDropDown = function() {
						scope.dateOptions.dropDownOpened = true;
					};

					scope.$watch('customDate', function(newDate, oldDate) {
						if (newDate && newDate !== oldDate) {
							CurrentReport.Filter.customDate = newDate;
							UserSettings.updateReportFilter(filterKey, CurrentReport.Filter.customDate);
							scope.onChange();
						}
					});

				});

			}
		};
	}]);


