angular.module('app')
.controller('coverageCtrl', ['$scope', 'Service', '$routeParams', function ($scope, Service, $routeParams) {

    Service('MonitorMondayOfWeek', { week: $routeParams.week }).then(function (res) {
        $scope.week = res;
    });

    var tag = $routeParams.tag == null ? "" : $routeParams.tag;
    Service('MonitorCoverageByChannel', { week: $routeParams.week, channel_tag: tag }).then(function (res) {
        $scope.coverageByChannel = JSON.parse(res);
        $scope.coverageByChannel.colTypes[2] = "Decimal";
        $scope.coverageByChannel.colTypes[3] = "Decimal";
        for (var i in $scope.coverageByChannel.rows) {
            $scope.coverageByChannel.rows[i].h_clickpath = "/monitor/capture/" + $scope.coverageByChannel.rows[i].h_channel_id;
        }

    });

}]);