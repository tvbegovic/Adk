angular.module('app.reports')
  .directive('reportSpentFilter', ['CurrentReport', 'UserSettings', function(CurrentReport, UserSettings) {
    return {
      restrict: 'AE',
      scope: {
        onChange: '&',
        onInit: '&'
      },
      templateUrl: 'Reports/_Directives/reportSpentFilter.html',
      link: function(scope) {
        var filterKey = 'spentFilter';

        scope.dropDownValues = [
          { Id: '1000', Name: '1000' },
          { Id: '5000', Name: '5000' },
          { Id: '10000', Name: '10000' },
          { Id: '20000', Name: '20000' },
          { Id: '50000', Name: '50000' }
        ];

        UserSettings.getReportFilters(filterKey).then(function(lastFilterValue) {
          var spentFilter = lastFilterValue ? angular.fromJson(lastFilterValue.Value) : {};
          scope.lessthan = CurrentReport.Filter.lessthan = Boolean(spentFilter.lessthan);
          setValueIfExists(CurrentReport.Filter.spent.Id) ||
            setValueIfExists(spentFilter.spentId) ||
            setValue(scope.dropDownValues[0]);

          scope.onInit();

        });

        scope.changeValue = function(spent) {
          if (spent.Id !== CurrentReport.Filter.spent.Id) {
            setValue(spent);
            updateFilterOnServer();
            scope.onChange();
          }
        };

        scope.toggleLesThen = function() {
          scope.lessthan = CurrentReport.Filter.lessthan = !scope.lessthan;
          updateFilterOnServer();
          scope.onChange();
        };

        function updateFilterOnServer() {
          UserSettings.updateReportFilter(filterKey, angular.toJson({
            spentId: CurrentReport.Filter.spent.Id,
            lessthan: scope.lessthan
          }));
        }

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.spent = value;
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


