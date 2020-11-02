angular.module('app')
.controller('dpocCaptureChannelCtrl', ['$scope', 'Service', '$routeParams', '$location', function ($scope, Service, $routeParams, $location) {

    $scope.channel = $routeParams.channel;
    $scope.whichWeb = $routeParams.whichWeb;

    Service('DpocGetChannelById', { channel: $scope.channel, whichWeb: $scope.whichWeb }).then(function (res) {
        $scope.channelName = res.name;
        $scope.channelDomain = res.domain;
    });

    Service('DpocCaptureChannel', { channel: $scope.channel, whichWeb: $scope.whichWeb }).then(function (res) {
        $scope.captureChannel = res;
    });

    Service('DpocCaptureHolesChannel', { channel: $scope.channel, whichWeb: $scope.whichWeb }).then(function (res) {
        $scope.captureHolesChannel = res;
    });

}]);