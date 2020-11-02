angular.module('app')
.factory('emailComposer', ['$modal', function ($modal) {
    return {
        open: function (title, subtitle, subject, body, tags) {
            return $modal.open({
                animation: false,
                templateUrl: '/Modules/EmailComposer/emailComposer.html',
                controller: 'emailComposerCtrl',
                size: 'lg',
				backdrop: 'static',
                resolve: {
                    template: function () {
                        return {
                            title: title,
                            subtitle: subtitle,
                            subject: subject,
                            body: body,
                            tags: tags
                        };
                    }
                }
            }).result;
        }
    }
}])
.controller('emailComposerCtrl', ['$scope', '$modalInstance', 'template', function ($scope, $modalInstance, template) {
    $scope.template = template;
    $scope.tags = '[' + template.tags.join('], [') + ']';
    $scope.save = function () {
        $modalInstance.close(template);
    };
    $scope.cancel = function () {
        $modalInstance.dismiss();
    };
}])
