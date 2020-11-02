angular.module('app.reports')
    .directive('reportBrandOrChannelFilter', ['CurrentReport', 'UserSettings', 'ReportFilterBase', function(CurrentReport, UserSettings, ReportFilterBase) {
        return {
            restrict: 'AE',
            scope: {
                onChange: '&',
                onInit: '&'
            },
            templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
            link: function(scope) {
                var filterKey = 'groupByFilter';

                scope.filterName = 'Group By';

                scope.dropDownValues = [
                    { Id: 'Brand', Name: 'Brand' },
                    { Id: 'Advertiser', Name: 'Advertiser' },
                    { Id: 'Channel', Name: 'Channel' }
                ];

                ReportFilterBase.getDefaultDropDownValue(filterKey, scope.dropDownValues, CurrentReport.Filter.groupBy.Id).then(function(value) {
                    setValue(value);
                    scope.onInit();
                });

                scope.changeValue = function(value) {
                    ReportFilterBase.onDropDownChange(filterKey, value, CurrentReport.Filter.groupBy.Id, function(value) {
                        setValue(value);
                        scope.onChange();
                    });
                };

                function setValue(value) {
                    scope.selectedValue = CurrentReport.Filter.groupBy = value;
                }

            }
        };
    }]);


