angular.module('app.reports')
  .directive('reportDisplayTypeFilter', ['CurrentReport', 'UserSettings', 'ReportFilterBase', function(CurrentReport, UserSettings, ReportFilterBase) {
    return {
      restrict: 'AE',
      scope: {
        onChange: '&',
        onInit: '&'
      },
      templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
      link: function(scope) {
        var filterKey = 'displayTypeFilter';
        scope.filterName = '';

        scope.dropDownValues = [
          { Id: 'Total', Name: 'Total' },
          { Id: 'Percentage', Name: 'Percentage' }
        ];

        ReportFilterBase.getDefaultDropDownValue(filterKey, scope.dropDownValues, CurrentReport.Filter.displayType.Id).then(function(value) {
          setValue(value);
          scope.onInit();
        });

        scope.changeValue = function(value) {
          ReportFilterBase.onDropDownChange(filterKey, value, CurrentReport.Filter.displayType.Id, function(value) {
            setValue(value);
            scope.onChange();
          });
        };

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.displayType = value;
        }

      }

    };
  }]);
