angular.module('app')
.controller('captureCtrl', ['$scope', '$modal', 'Service', function ($scope, $modal, Service) {

    Service('MonitorRciLookupLimit').then(function (res) {
        $scope.rciLookupLimit = res;
    });

    Service('MonitorCaptureStatusAllChannels').then(function (res) {
        $scope.channels = JSON.parse(res);
        $scope.deadChannels = { rows: [], colTypes: [] };
        $scope.unknownChannels = { rows: [], colTypes: [] };
        $scope.otherChannels = { rows: [], colTypes: [] };
        for (var i in $scope.channels.rows) {
            $scope.channels.rows[i].h_clickpath = "/monitor/capture/" + $scope.channels.rows[i].h_channel_id;
            if ($scope.channels.rows[i].h_status == "3") {
                $scope.otherChannels.rows.push($scope.channels.rows[i]);
                $scope.otherChannels.colTypes.push($scope.channels.colTypes[i]);
            } else {
                if ($scope.channels.rows[i].Waiting_For == "") {
                    $scope.channels.rows[i].Waiting_For = "99999999"
                }
                var hold_status = $scope.channels.rows[i].h_hold_status;
                if (hold_status == "0") {
                    $scope.channels.rows[i].Status = "Solved";
                }
                if (hold_status == "1") {
                    $scope.channels.rows[i].Status = "Hold until " + $scope.channels.rows[i].h_hold_until;
                }
                $scope.deadChannels.rows.push($scope.channels.rows[i]);
                $scope.deadChannels.colTypes.push($scope.channels.colTypes[i]);
            }
        }
    });

    var refreshChannelStatus = function (channel) {
        Service('MonitorGetChannelStatus', { channel_id: channel.h_channel_id }).then(function (res) {
            var channelStatus = JSON.parse(res).rows[0];
            channel.Status = channelStatus.status == "0" ? "Solved" : "Hold until " + channelStatus.hold_until;
        });
    };

    $scope.updateStatus = function (status, index, hold) {
        var channel = $scope.deadChannels.rows[index];
        Service('MonitorUpdateRecordingStatus', { channel_id: channel.h_channel_id, status: status, hold: hold, reason: "" })
            .then(function () {
                refreshChannelStatus(channel);
            });
    };

    $scope.holdMore = function (days, index) {
        $scope.channel = $scope.deadChannels.rows[index];

        $scope.hold = {
            days: days,
            reason: ''
        };
        return $modal.open({
            animation: false,
            templateUrl: 'holdModal.html',
            controller: ['$scope', '$modalInstance', 'hold', 'channel', function ($scope, $modalInstance, hold, channel) {
                $scope.hold = angular.copy(hold);
                $scope.channel = channel;

                $scope.ok = function () {
                    console.log($scope.hold);
                    $modalInstance.close();
                    Service('MonitorUpdateRecordingStatus', { channel_id: $scope.channel.h_channel_id, status: 1, hold: 24 * $scope.hold.days, reason: $scope.hold.reason })
                        .then(function () {
                            refreshChannelStatus($scope.channel);
                        });
                };

                $scope.cancel = function () {
                    $modalInstance.close();
                };
            }],
            resolve: {
                hold: function () {
                    return $scope.hold;
                },
                channel: function () {
                    return $scope.channel;
                }
            }
        });
    };




}]);