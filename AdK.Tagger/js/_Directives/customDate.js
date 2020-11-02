angular.module('app')
	.directive('customDate', ['UserSettings', function (UserSettings) {
	    return {
	        restrict: 'AE',
	        scope: {
	            onChange: '&',
	            onInit: '&',
                customDateModel: '=',
				minDate: '='
	        },
	        templateUrl: 'js/_Directives/customDate.html',
	        link: function (scope) {
	            var firstTrigger = true;
	            var filterKey = 'customDateFilter';

	            scope.dateOptions = {
	                formatYear: 'yy',
	                maxDate: new Date(),
	                startingDay: 1,
                    dropDownOpened: false,
					minDate: scope.minDate
	            };

	            var customDate = new Date();
	            customDate.setDate(customDate.getDate());

	            scope.customDateModel = scope.customDateModel || customDate;

	            scope.onInit();

	            scope.openDropDown = function () {
	                scope.dateOptions.dropDownOpened = true;
	            };

	            scope.$watch('customDateModel', function (newDate, oldDate) {
	                if (firstTrigger == true) {
	                    firstTrigger = !firstTrigger;
	                    return;
	                }
                    if (newDate && newDate !== oldDate) {
                        if (scope.minDate != null && newDate < scope.minDate)
                            scope.customDateModel = scope.minDate;

	                    scope.onChange();
	                }
	            });


	        }
	    };
	}]);


