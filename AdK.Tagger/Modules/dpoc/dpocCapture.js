angular.module('app')
.controller('dpocCaptureCtrl', ['$scope', '$interval', 'Service', '$location', function ($scope, $interval, Service, $location) {

    $scope.actions = [
       { id: '0', name: 'Stream Offline' },
       { id: '1', name: 'Works Now' },
       { id: '2', name: 'New URL Set' }
    ];
    $scope.selectedAction = [];

    $scope.updateAction = function (channel_id, actionType, index) {
        $scope.selectedAction[channel_id] = actionType;
        console.log("Action " + $scope.selectedAction[channel_id] + " for channel " + channel_id);
        Service('DpocUpdateChannelAction', { channel: channel_id, action: actionType })
            .then(function () {
                $scope.deadChannelsHr[index].action = actionType;
                Service('DpocCaptureActionTimestamp', { channel: channel_id }).
                    then(function (res) {
                    $scope.deadChannelsHr[index].actionTimestamp = res;
                });
            });
    };

    $scope.go = function (path) {
        $location.path('/dpoc/capture/' + path);
    };

    $scope.actionName = function (action) {
        switch (action) {
            case -1:
                return "Needs a check";
                break;
            case 0:
                return "Stream Offline";
                break;
        }
    };

    $scope.getCaptureDetailsHr = function () {
        Service('DpocCaptureDetails', { whichWeb: "hr" }).then(function (res) {
            $scope.captureDetailsHr = res;
        });
    }

    $scope.getCaptureDetailsMis = function () {
        Service('DpocCaptureDetails', { whichWeb: "mis" }).then(function (res) {
            $scope.captureDetailsMis = res;
        });
    }    
    $scope.getCaptureDetailsCy = function () {
        Service('DpocCaptureDetails', { whichWeb: "cy" }).then(function (res) {
            $scope.captureDetailsCy = res;
        });
    }
    $scope.getCaptureDetailsRoi = function () {
        Service('DpocCaptureDetails', { whichWeb: "roi" }).then(function (res) {
            $scope.captureDetailsRoi = res;
        });
    }
    $scope.getDeadChannelsHr = function () {
        Service('DpocDeadChannels', { whichWeb: "hr" }).then(function (res) {
            $scope.deadChannelsHr = res;
        });
    }
    $scope.getDeadChannelsMis = function () {
        Service('DpocDeadChannels', { whichWeb: "mis" }).then(function (res) {
            $scope.deadChannelsMis = res;
        });
    }
    $scope.getDeadChannelsCy = function () {
        Service('DpocDeadChannels', { whichWeb: "cy" }).then(function (res) {
            $scope.deadChannelsCy = res;
        });
    }
    $scope.getDeadChannelsRoi = function () {
        Service('DpocDeadChannels', { whichWeb: "roi" }).then(function (res) {
            $scope.deadChannelsRoi = res;
        });
    }
    var reloadCapture = $interval(function () {
        $scope.getDeadChannelsHr();
        $scope.getCaptureDetailsHr();
        $scope.getDeadChannelsMis();
        $scope.getDeadChannelsCy();
        $scope.getDeadChannelsRoi();
        $scope.getCaptureDetailsMis();
        $scope.getCaptureDetailsCy();
        $scope.getCaptureDetailsRoi();
    }, 300000);

    $scope.$on('$destroy', function () {
        $interval.cancel(reloadCapture)
    });

    $scope.getDeadChannelsHr();
    $scope.getCaptureDetailsHr();
    $scope.getDeadChannelsMis();
    $scope.getDeadChannelsCy();
    $scope.getDeadChannelsRoi();
    $scope.getCaptureDetailsMis();
    $scope.getCaptureDetailsCy();
    $scope.getCaptureDetailsRoi();

}]);