angular.module('app.reports')
  .directive('reportLimitFilter', ['CurrentReport', 'UserSettings', 'ReportFilterBase', function(CurrentReport, UserSettings, ReportFilterBase) {
    return {
      restrict: 'AE',
      scope: {
        onChange: '&',
        onInit: '&'
      },
      templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
      link: function(scope) {
        var filterKey = 'limitFilter';
        scope.filterName = 'Top';

        scope.dropDownValues = [
          { Id: '10', Name: '10' },
          { Id: '20', Name: '20' },
          { Id: '100', Name: '100' }
        ];

        ReportFilterBase.getDefaultDropDownValue(filterKey, scope.dropDownValues, CurrentReport.Filter.limit.Id).then(function(value) {
          setValue(value);
          scope.onInit();
        });

        scope.changeValue = function(value) {
          ReportFilterBase.onDropDownChange(filterKey, value, CurrentReport.Filter.limit.Id, function(value) {
            setValue(value);
            scope.onChange();
          });
        };

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.limit = value;
        }

      }
    };
  }]);
