angular.module('app.reports')
  .directive('reportShowFilter', ['CurrentReport', 'UserSettings', function(CurrentReport, UserSettings) {
    return {
      restrict: 'AE',
      scope: {
        onChange: '&',
        onInit: '&'
      },
      templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
      link: function(scope) {
        var filterKey = 'showFilter';
        scope.filterName = 'Show';

        scope.dropDownValues = [
          { Id: 'AllMediaSpend', Name: 'All Media Spend' },
          { Id: 'RadioTimeSold', Name: 'Radio Time Sold' },
          { Id: 'RadioSpend', Name: 'Radio Spend' },
          { Id: 'TVTimeSold', Name: 'TV Time Sold' },
          { Id: 'TVSpend', Name: 'TV Spend' }
        ];

        UserSettings.getReportFilters(filterKey).then(function(lastFilterValue) {

          setValueIfExists(CurrentReport.Filter.show.Id) ||
            setValueIfExists(lastFilterValue ? lastFilterValue.Value : null) ||
            setValue(scope.dropDownValues[0]);

          scope.onInit();

        });

        scope.changeValue = function(include) {
          if (include.Id !== CurrentReport.Filter.show.Id) {
            UserSettings.updateReportFilter(filterKey, include.Id);
            setValue(include);
            scope.onChange();
          }
        };

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.show = value;
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


