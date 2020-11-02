angular.module('app.reports')
  .directive('reportAdBreakDurationFilter', ['CurrentReport', 'UserSettings',
    function(CurrentReport, UserSettings) {
      return {
        restrict: 'AE',
        scope: {
          onChange: '&',
          onInit: '&'
        },
        templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
        link: function(scope) {
          var filterKey = 'adBreakFilter';
          scope.filterName = 'Ad Break Minutes >=';

          scope.dropDownValues = [
            { Id: '30', Name: '0:30' },
            { Id: '60', Name: '1:00' },
            { Id: '90', Name: '1:30' },
            { Id: '120', Name: '2:00' },
            { Id: '150', Name: '2:30' },
            { Id: '180', Name: '3:00' },
            { Id: '210', Name: '3:30' },
            { Id: '240', Name: '4:00' },
            { Id: '270', Name: '4:30' },
            { Id: '300', Name: '5:00' },
            { Id: '330', Name: '5:30' },
            { Id: '360', Name: '6:00' },
            { Id: '390', Name: '6:30' },
            { Id: '420', Name: '7:00' },
            { Id: '450', Name: '7:30' },
            { Id: '480', Name: '8:00' },
            { Id: '510', Name: '8:30' },
            { Id: '540', Name: '9:00' },
            { Id: '570', Name: '9:30' },
            { Id: '600', Name: '10:00' }
          ];

          UserSettings.getReportFilters(filterKey).then(function(lastFilterValue) {

            setValueIfExists(CurrentReport.Filter.adBreakDuration.Id) ||
              setValueIfExists(lastFilterValue ? lastFilterValue.Value : null) ||
              setValue(scope.dropDownValues[5]);

            scope.onInit();

          });

          scope.changeValue = function(value) {
            if (value.Id !== CurrentReport.Filter.adBreakDuration.Id) {
              setValue(value);
              UserSettings.updateReportFilter(filterKey, value.Id);
              scope.onChange();
            }
          };

          function setValue(value) {
            scope.selectedValue = CurrentReport.Filter.adBreakDuration = value;
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


