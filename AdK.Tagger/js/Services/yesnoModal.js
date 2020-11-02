angular.module('app')
	.factory('yesnoPopup', ['$modal', function ($modal) {
		return {
			open: function (title, subtitle, text) {
				return $modal.open({
					animation: false,
					template: [
						'<div class="modal-header">',
						'  <h3>{{ data.title }}</h3>',
						'  <p ng-if="data.subtitle">{{ data.subtitle }}</p>',
						'</div>',
						'<div class="modal-body">',
						'  <p>{{ data.text }}</p>',
						'</div>',
						'<div class="modal-footer">',
						'  <button class="btn btn-primary" ng-click="$close(\'yes\')">Yes</button>',
						'  <button class="btn btn-secondary" ng-click="$close(\'no\')">No</button>',
						'  <button class="btn btn-default" ng-click="$dismiss()">Cancel</button>',
						'</div>'
					].join(' '),
					controller: ['$scope', 'data', function ($scope, data) {
						$scope.data = data;
					}],
					resolve: {
						data: function () {
							return {
								title: title,
								subtitle: subtitle,
								text: text
							};
						}
					}
				}).result;
			}
		};
	}]);
