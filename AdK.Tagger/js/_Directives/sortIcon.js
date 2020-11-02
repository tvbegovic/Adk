angular.module('app')
  .directive('sortIcon', [function() {
    return {
      restrict: 'E',
      scope: {
        ascending: '=',
        show: '='
      },
      template: '<i class="glyphicon" ng-class="{\'glyphicon-chevron-up\': ascending, \'glyphicon-chevron-down\': !ascending}" ng-if="show"></i>'
    };
  }]);
