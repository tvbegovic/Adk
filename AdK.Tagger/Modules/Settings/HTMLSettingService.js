angular.module('app')
.factory('htmlSettingService', ['$modal', function ($modal) {
    return {
        open: function (title, label, text) {
            return $modal.open({
                animation: false,
				templateUrl: '/Modules/Settings/HTMLSettingModal.html',
				controller: 'htmlSettingCtrl',
                size: 'lg',
				backdrop: 'static',
                resolve: {
                    template: function () {
                        return {
                            title: title,
							label: label,
							text: text
                        };
                    }
                }
            }).result;
        }
    }
}])
	.controller('htmlSettingCtrl', ['$scope', '$modalInstance', 'template', function ($scope, $modalInstance, template) {
    $scope.template = template;
    
    $scope.save = function () {
        $modalInstance.close(template);
    };
    $scope.cancel = function () {
        $modalInstance.dismiss();
    };
}])
