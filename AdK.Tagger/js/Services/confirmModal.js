angular.module('app')
  .factory('confirmPopup', ['$modal', function($modal) {
    return {
      open: function(title, subtitle, text) {
        return $modal.open({
          animation: false,
          template: [
            '<div class="modal-header">',
            '  <h3>{{ confirm.title }}</h3>',
            '  <p ng-if="confirm.subtitle">{{ confirm.subtitle }}</p>',
            '</div>',
            '<div class="modal-body">',
            '  <p>{{ confirm.text }}</p>',
            '</div>',
            '<div class="modal-footer">',
            '  <button class="btn btn-default" ng-click="$dismiss()">Cancel</button>',
            '  <button class="btn btn-primary" ng-click="$close()">OK</button>',
            '</div>'
          ].join(' '),
          controller: ['$scope', 'confirm', function($scope, confirm) {
            $scope.confirm = confirm;
          }],
          resolve: {
            confirm: function() {
              return {
                title: title,
                subtitle: subtitle,
                text: text
              };
            }
          }
        }).result;
      }
    }
  }])
