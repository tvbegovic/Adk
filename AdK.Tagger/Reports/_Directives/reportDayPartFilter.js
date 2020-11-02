angular.module('app.reports')
  .directive('reportDayPartFilter', ['CurrentReport', 'UserSettings', function(CurrentReport, UserSettings) {
    return {
      restrict: 'AE',
      scope: {
        onChange: '&',
        onInit: '&'
      },
      templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
      link: function(scope) {
        var filterKey = 'dayPartFilter';
        scope.filterName = 'Time';

        scope.dropDownValues = [
          { Id: 'AllDay', Name: 'All Day' },
          { Id: 'MyDayparts', Name: 'My Dayparts' }
        ];

        UserSettings.getReportFilters(filterKey).then(function(lastFilterValue) {

          setValueIfExists(CurrentReport.Filter.dayPart.Id) ||
            setValueIfExists(lastFilterValue ? lastFilterValue.Value : null) ||
            setValue(scope.dropDownValues[0]);

          scope.onInit();

        });

        scope.changeValue = function(value) {
          if (value.Id !== CurrentReport.Filter.dayPart.Id) {
            setValue(value);
            UserSettings.updateReportFilter(filterKey, value.Id);
            scope.onChange();
          }
        };

        scope.onInit();

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.dayPart = value;
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


