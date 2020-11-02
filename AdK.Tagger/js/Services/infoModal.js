angular.module('app')
  .factory('infoModal', ['$modal', function($modal) {
    return {
      open: function(title, text) {
        return $modal.open({
          animation: false,
          template: [
            '<div class="modal-header">',
            '  <h3>{{ info.title }}</h3>',
            '</div>',
            '<div class="modal-body">',
            '  <p>{{ info.text }}</p>',
            '</div>',
            '<div class="modal-footer">',
            '  <button class="btn btn-primary" ng-click="$close()">OK</button>',
            '</div>'
          ].join(' '),
          controller: ['$scope', 'info', function($scope, info) {
            $scope.info = info;
          }],
          resolve: {
            info: function() {
              return {
                title: title,
                text: text
              };
            }
          }
        }).result;
      }
    }
  }])
