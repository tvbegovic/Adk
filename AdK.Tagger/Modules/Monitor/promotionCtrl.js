angular.module('app')
.controller('promotionCtrl', ['$scope', 'Service', function ($scope, Service) {

    Service('MonitorPromotionDetails').then(function (res) {
        $scope.promotionDetails = JSON.parse(res);
    });
    Service('MonitorPromotionQueue').then(function (res) {
        $scope.promotionQueue = JSON.parse(res);
    });


}]);