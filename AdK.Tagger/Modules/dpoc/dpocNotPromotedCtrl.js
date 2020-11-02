angular.module('app')
.controller('dpocNotPromotedCtrl', ['$scope', 'Service', function ($scope, Service) {
    Service('DpocNotPromotedClips').then(function (res) {
        $scope.NotPromotedClips = res;
    });
}]);