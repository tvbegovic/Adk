angular.module('app')
  .directive('alertMessage', function() {
    return {
      restrict: 'AE',
      scope: {
        type: '@',
				text: '@'
      },
      templateUrl: '/js/_Directives/alertMessage.html'
    };
  });


