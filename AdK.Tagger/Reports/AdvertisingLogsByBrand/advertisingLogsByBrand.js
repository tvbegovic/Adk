angular.module('app.reports')
    .controller('advertisingLogsByBrand', ['$scope', 'Service',
        function($scope, Service) {
            var initPageLoad = _.once(load);

            $scope.onDirectivesInit = function() {
                if ($scope.haveId($scope.current.channel) &&
                    $scope.haveId($scope.current.industry) &&
                    $scope.haveId($scope.current.media) &&
                    $scope.haveId($scope.current.brandOrAdvertiser) &&
                    $scope.current.customDate) {
                    initPageLoad();
                }
            };

            $scope.load = load;
            $scope.reportData = null;
            function load() {
                resetPlayer();
                $scope.hideMessage();
                Service('MediaHouseAdvertisingLogsByBrand', {
                    channelId: $scope.current.channel.Id,
                    industryId: $scope.current.industry.Id,
                    media: $scope.current.media.Id,
                    date: $scope.current.customDate,
                    categories: $scope.current.categories,
                    brandOrAdvertiser: $scope.current.brandOrAdvertiser.Id
                }).then(function(data) {
                    $scope.reportData = data;
                    if (!data.AdvertisingLogs.length) { $scope.showMessage('NoData'); }
                }).catch(function() {
                    $scope.showMessage('Error');
                });
            }

            ///Player
            $scope.playPauseSong = function(rowData, rowIndex) {
                $scope.player.playPauseSong(rowData.SongUrl, rowData.SongDuration, rowIndex);
            };

            function resetPlayer() {
                if ($scope.player && $scope.player.reset) {
                    $scope.player.reset();
                }
            }

        }]);
