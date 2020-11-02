angular.module('app.reports')
  .directive('reportIndustryFilter', ['$q', 'CurrentReport', 'Service', 'UserSettings', 'CachedApiCalls', 'ReportFilterBase',
    function($q, CurrentReport, Service, UserSettings, CachedApiCalls, ReportFilterBase) {
      return {
        restrict: 'AE',
        scope: {
          onInit: '&',
          onChange: '&'
        },
        templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
        link: function(scope) {
          var filterKey = 'industryFilter';
          scope.filterName = 'Industry';

					CachedApiCalls.getAllIndustries().then(function(response) {
            var industries = _.clone(response);

            if (!industries || !industries.length) {
              CurrentReport.Filter.industry = {};
              scope.selectedValue = null;
              scope.onInit();
              return;
            }

            industries.unshift({
              Id: 'All',
              Name: 'All'
            });

            scope.dropDownValues = industries || [];

            ReportFilterBase.getDefaultDropDownValue(filterKey, scope.dropDownValues, CurrentReport.Filter.industry.Id).then(function(value) {
              setValue(value);
              scope.onInit();
            });

            scope.changeValue = function(value) {
              ReportFilterBase.onDropDownChange(filterKey, value, CurrentReport.Filter.industry.Id, function(value) {
                setValue(value);
                scope.onChange();
              });
            };

            function setValue(value) {
              scope.selectedValue = CurrentReport.Filter.industry = value;
            }

          });

        }
      };
    }]);


