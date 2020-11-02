angular.module('app')
.controller('captureChannelCtrl', ['$scope', 'Service', '$routeParams', '$location', '$window', function ($scope, Service, $routeParams, $location, $window) {

    $scope.channel = $routeParams.channel;

    Service('MonitorGetChannelById', { channel: $scope.channel }).then(function (res) {
        $scope.channel = JSON.parse(res).rows[0];
    });

    Service('MonitorCaptureHolesChannel', { channel: $scope.channel }).then(function (res) {
        $scope.captureHolesChannel = JSON.parse(res);
    });

    Service('MonitorCaptureChannel', { channel: $scope.channel }).then(function (res) {
        $scope.captureChannel = JSON.parse(res);
    });

    Service('RecordingGapsChannel', { channel: $scope.channel }).then(function (res) {
        $scope.recordingGapsChannel = JSON.parse(res);
        $scope.recordingGapsChannel.colTypes[2] = "TimeSpan";

        console.log(res);
    });

    $scope.goback = function () {
        $window.history.back();
    }
}]);