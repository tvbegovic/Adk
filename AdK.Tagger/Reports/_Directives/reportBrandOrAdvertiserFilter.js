angular.module('app.reports')
  .directive('reportBrandOrAdvertiserFilter', ['CurrentReport', 'UserSettings', 'ReportFilterBase', function(CurrentReport, UserSettings, ReportFilterBase) {
    return {
      restrict: 'AE',
      scope: {
        onChange: '&',
        onInit: '&'
      },
      templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
      link: function(scope) {
        var filterKey = 'brandOrAdvertiserFilter';
        scope.filterName = 'By';

        scope.dropDownValues = [
          { Id: 'Brand', Name: 'Brand' },
          { Id: 'Advertiser', Name: 'Advertiser' }
        ];

        ReportFilterBase.getDefaultDropDownValue(filterKey, scope.dropDownValues, CurrentReport.Filter.brandOrAdvertiser.Id).then(function(value) {
          setValue(value);
          scope.onInit();
        });

        scope.changeValue = function(value) {
          ReportFilterBase.onDropDownChange(filterKey, value, CurrentReport.Filter.brandOrAdvertiser.Id, function(value) {
            setValue(value);
            scope.onChange();
          });
        };

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.brandOrAdvertiser = value;
        }

      }
    };

  }]);
