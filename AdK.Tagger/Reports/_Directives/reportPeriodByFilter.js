angular.module('app.reports')
  .directive('reportPeriodByFilter', ['CurrentReport', 'UserSettings', function(CurrentReport, UserSettings) {
    return {
      restrict: 'AE',
      scope: {
        onChange: '&',
        onInit: '&'
      },
      templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
      link: function(scope) {
        var filterKey = 'periodByFilter';
        scope.filterName = 'By';

        scope.dropDownValues = [
          { Id: 'Owner', Name: 'Owner', Title: 'Owner', Subtitle: 'Share of' },
          { Id: 'MediaHouse', Name: 'Media House', Title: 'Media', Subtitle: 'Houses by' }
        ];

        UserSettings.getReportFilters(filterKey).then(function(lastFilterValue) {

          setValueIfExists(CurrentReport.Filter.periodBy.Id) ||
            setValueIfExists(lastFilterValue ? lastFilterValue.Value : null) ||
            setValue(scope.dropDownValues[0]);

          scope.onInit();

        });

        scope.changeValue = function(include) {
          if (include.Id !== CurrentReport.Filter.periodBy.Id) {
            UserSettings.updateReportFilter(filterKey, include.Id);
            setValue(include);
            scope.onChange();
          }
        };

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.periodBy = value;
        }

        function setValueIfExists(value) {
          if (value) {
            var ddValue = _.find(scope.dropDownValues, function(dd) { return dd.Id === value; });
            if (ddValue) {
              setValue(ddValue);
              return true;
            }
          }
          return false;
        }

      }
    };
  }]);


