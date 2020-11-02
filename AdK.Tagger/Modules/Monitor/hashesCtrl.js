angular.module('app')
.controller('hashesCtrl', ['$scope', 'Service', function ($scope, Service) {

    Service('MonitorHashesStatusAllChannels').then(function (res) {
        $scope.hashesStatusAllChannels = JSON.parse(res);
        for (var i in $scope.hashesStatusAllChannels.rows) {
            $scope.hashesStatusAllChannels.rows[i].h_class = $scope.hashesStatusAllChannels.rows[i].h_status == 1 ? "danger" : ($scope.hashesStatusAllChannels.rows[i].h_status == 2 ? "warning" : "");
        }
    });

    Service('MonitorRciLookupLimit').then(function (res) {
        $scope.rciLookupLimit = res;
    });

}]);