angular.module('app.reports')
  .directive('chartTooltip', function() {
    return {
      restrict: 'AE',
      scope: {},
      templateUrl: 'Reports/_Directives/chartTooltipTemplate.html',
      link: function(scope) {
      }
    };
  });


