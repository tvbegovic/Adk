angular.module('app')
.controller('harvestingCtrl', ['$scope', 'Service', function ($scope, Service) {

    Service('MonitorHarvestingStatus').then(function (res) {
        $scope.harvested = JSON.parse(res);

        $scope.maxDelay = function (data) {
            var max = -1;
            console.log(data);
            for (var i in data) {
                max = data[i].delay > max ? data[i].delay : max;
            }
            return max;
        };
    });

}]);