angular.module('app.reports')
  .directive('reportDayOfWeekFilter', ['CurrentReport', 'UserSettings', function(CurrentReport, UserSettings) {
    return {
      restrict: 'AE',
      scope: {
        onChange: '&',
        onInit: '&'
      },
      templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
      link: function(scope) {
        var filterKey = 'dayOfWeekFilter';
        scope.filterName = 'Day of Week';

        scope.dropDownValues = [
          { Id: 'All', Name: 'All' },
          { Id: 'M_F', Name: 'M-F' },
          { Id: 'Weekends', Name: 'Weekends' },
          { Id: 'Monday', Name: 'Monday' },
          { Id: 'Tuesday', Name: 'Tuesday' },
          { Id: 'Wednesday', Name: 'Wednesday' },
          { Id: 'Thursday', Name: 'Thursday' },
          { Id: 'Friday', Name: 'Friday' },
          { Id: 'Saturday', Name: 'Saturday' },
          { Id: 'Sunday', Name: 'Sunday' }
        ];


        UserSettings.getReportFilters(filterKey).then(function(lastFilterValue) {

          setValueIfExists(CurrentReport.Filter.dayOfWeek.Id) ||
            setValueIfExists(lastFilterValue ? lastFilterValue.Value : null) ||
            setValue(scope.dropDownValues[0]);

          scope.onInit();

        });

        scope.changeValue = function(value) {
          if (value.Id !== CurrentReport.Filter.dayOfWeek.Id) {
            setValue(value);
            UserSettings.updateReportFilter(filterKey, value.Id);
            scope.onChange();
          }
        };

        scope.onInit();

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.dayOfWeek = value;
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














