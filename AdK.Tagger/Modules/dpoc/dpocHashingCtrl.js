angular.module('app')
.controller('dpocHashingCtrl', ['$scope', '$interval', 'Service', function ($scope, $interval, Service) {

    $scope.getHashingDetailsHr = function () {
        Service('DpocHashingDetails', { whichWeb: "hr" }).then(function (res) {
            $scope.hashingDetailsHr = res;
        });
    }
    $scope.getHashingDetailsMis = function () {
        Service('DpocHashingDetails', { whichWeb: "mis" }).then(function (res) {
            $scope.hashingDetailsMis = res;
        });
    }

    var reloadHashing = $interval(function () {
        $scope.getHashingDetailsHr();
        $scope.getHashingDetailsMis();
    }, 60000);

    $scope.$on('$destroy', function () {
        $interval.cancel(reloadHashing)
    });

    $scope.getHashingDetailsHr();
    $scope.getHashingDetailsMis();
}]);