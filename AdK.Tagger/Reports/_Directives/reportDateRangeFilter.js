angular.module('app.reports')
	.directive('reportDateRangeFilter', ['CurrentReport', 'CachedApiCalls', function(CurrentReport, CachedApiCalls) {
		return {
			restrict: 'AE',
			scope: {
				onChange: '&',
				onInit: '&',
				disabled: '='
			},
			templateUrl: '/Reports/_Directives/reportDateRangeFilter.html',

			link: function(scope, element) {

				scope.dateOptions = {
					format: localStorage.UserDateFormat,
					maxDate: new Date(),
					dp1Opened: false,
					dp2Opened: false
				};

				scope.openDp1 = function() {
					scope.dateOptions.dp1Opened = true;
				};

				scope.openDp2 = function() {
					scope.dateOptions.dp2Opened = true;
				};

				if (CurrentReport.Filter.dateFrom && CurrentReport.Filter.dateTo) {
					scope.dateTo = CurrentReport.Filter.dateTo;
					scope.dateFrom = CurrentReport.Filter.dateFrom;
					scope.onInit();
				} else {
					//set default date to last full month
					var dateFrom = new Date();
					var dateTo = new Date();

					if (isLastDayOfMonth(dateTo)) {
						scope.dateTo = CurrentReport.Filter.dateTo = dateTo;
					} else {
						dateFrom.setMonth(dateFrom.getMonth() - 1);
						//last day of previous month,
						scope.dateTo = CurrentReport.Filter.dateTo = new Date(dateTo.setDate(0));
					}

					dateFrom.setDate(1);
					scope.dateFrom = CurrentReport.Filter.dateFrom = dateFrom;
					scope.onInit();


				}

				scope.$watch('dateTo', function(newDate, oldDate) {
					if (newDate && newDate !== oldDate) {
						CurrentReport.Filter.dateTo = newDate;
						scope.onChange();
					}
				});
				scope.$watch('dateFrom', function(newDate, oldDate) {
					if (newDate && newDate !== oldDate) {
						CurrentReport.Filter.dateFrom = newDate;
						scope.onChange();
					}
				});


				function isLastDayOfMonth(dt) {
					var test = new Date(dt.getTime());
					test.setDate(test.getDate() + 1);
					return test.getDate() === 1;
				}
			}
		};
	}]);


