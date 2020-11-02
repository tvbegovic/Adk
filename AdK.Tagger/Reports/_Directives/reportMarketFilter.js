angular.module('app.reports')
  .directive('reportMarketFilter', ['$q', 'CurrentReport', 'Service', 'UserSettings', 'CachedApiCalls', 'ReportFilterBase',
    function($q, CurrentReport, Service, UserSettings, CachedApiCalls, ReportFilterBase) {
      return {
        restrict: 'AE',
        scope: {
          onInit: '&',
          onChange: '&'
        },
        templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
        link: function(scope) {
          var filterKey = 'marketFilter';
          scope.filterName = 'Market';
          CachedApiCalls.getUserMarkets().then(function(response) {
            var markets = response ? _.clone(response) : [];

            markets.unshift({
              Id: 'All',
              Name: 'All'
            });

            scope.dropDownValues = markets || [];

            ReportFilterBase.getDefaultDropDownValue(filterKey, scope.dropDownValues, CurrentReport.Filter.market.Id).then(function(value) {
              setValue(value);
              scope.onInit();
            });

            scope.changeValue = function(value) {
              ReportFilterBase.onDropDownChange(filterKey, value, CurrentReport.Filter.market.Id, function(value) {
                setValue(value);
                scope.onChange();
              });
            };

            function setValue(value) {
              scope.selectedValue = CurrentReport.Filter.market = value;
            }

          });

        }
      };
    }]);


