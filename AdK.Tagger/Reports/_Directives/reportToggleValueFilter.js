angular.module('app.reports')
  .directive('reportToggleValueFilter', ['CurrentReport', 'UserSettings', 'ReportFilterBase', function(CurrentReport, UserSettings, ReportFilterBase) {
    return {
      restrict: 'AE',
      scope: {
        onChange: '&',
        onInit: '&'
      },
      templateUrl: 'Reports/_Directives/reportToggleButtonsTemplate.html',
      link: function(scope) {
        var filterKey = 'toggleValueFilter';
        scope.filterName = 'Show';

        scope.toggleValues = [
          { Id: 'Count', Name: 'Count' },
          { Id: 'Duration', Name: 'Air-Time' },
          { Id: 'Spend', Name: 'Spend' }
        ];

        ReportFilterBase.getDefaultDropDownValue(filterKey, scope.toggleValues, CurrentReport.Filter.value.Id).then(function(value) {
          setValue(value);
          scope.onInit();
        });

        scope.changeValue = function(value) {
          ReportFilterBase.onDropDownChange(filterKey, value, CurrentReport.Filter.value.Id, function(value) {
            setValue(value);
            scope.onChange();
          });
        };

        scope.onInit();

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.value = value;
        }

      }
    };
  }]);
