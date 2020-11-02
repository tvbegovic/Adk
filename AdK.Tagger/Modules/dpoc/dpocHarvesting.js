angular.module('app')
.controller('dpocHarvestingCtrl', ['$scope', '$interval', 'Service', function ($scope, $interval, Service) {
    $scope.getHarvestingStats = function () {
        Service('DpocHarvestingStats').then(function (res) {
            $scope.HarvestingStats = res;
        });
    };
    $scope.getHarvestingStatsMis = function () {
        Service('DpocHarvestingStatsMis').then(function (res) {
            $scope.HarvestingStatsMis = res;
        });
    };
    var reloadHarvesting = $interval(function () {
        $scope.getHarvestingStats();
        $scope.getHarvestingStatsMis();
    }, 60000);

    $scope.$on('$destroy', function () {
        $interval.cancel(reloadDuplicates)
    });

    $scope.getHarvestingStats();
    $scope.getHarvestingStatsMis();
}]);