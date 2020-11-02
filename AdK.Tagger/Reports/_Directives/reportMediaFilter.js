angular.module('app.reports')
  .directive('reportMediaFilter', ['CurrentReport', 'UserSettings', 'ReportFilterBase', function(CurrentReport, UserSettings, ReportFilterBase) {
    return {
      restrict: 'AE',
      scope: {
        onChange: '&',
        onInit: '&'
      },
      templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
      link: function(scope) {
        var filterKey = 'mediaFilter';
        scope.filterName = 'Media';

        scope.dropDownValues = [
          { Id: 'All', Name: 'All' },
          { Id: 'Radio', Name: 'Radio' },
          { Id: 'Tv', Name: 'Tv' }
        ];

        ReportFilterBase.getDefaultDropDownValue(filterKey, scope.dropDownValues, CurrentReport.Filter.media.Id).then(function(value) {
          setValue(value);
          scope.onInit();
        });

        scope.changeValue = function(value) {
          ReportFilterBase.onDropDownChange(filterKey, value, CurrentReport.Filter.media.Id, function(value) {
            setValue(value);
            scope.onChange();
          });
        };

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.media = value;
        }

      }
    };
  }]);
