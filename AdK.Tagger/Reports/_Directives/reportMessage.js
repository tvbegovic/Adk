angular.module('app.reports')
  .directive('reportMessage', function() {
    return {
      restrict: 'AE',
      scope: {
        template: '@',
				text: '@'
      },
      templateUrl: '/Reports/_Directives/reportMessagesTemplate.html',
      link: function(scope) {
      }
    };
  });


