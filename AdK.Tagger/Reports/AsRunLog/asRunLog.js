angular.module('app.reports')
    .controller('asRunLogCtrl', ['$scope', 'Service', 'ValueFormatter',
        function($scope, Service, ValueFormatter) {
            $scope.selectedBrand;
            $scope.selectedAdvertiser = '';
            var initPageLoad = _.once(load);

            var getAsRunLogData = function(asRunData) {

                var focusIndex = null;
                for (var i = 0; i < asRunData.length; i++) {
                    if (asRunData[i].ChannelId == $scope.current.channel.Id) { focusIndex = i; }
                }

                if (focusIndex) {
                    var temp = asRunData[focusIndex];
                    asRunData.splice(focusIndex, 1);
                    asRunData.unshift(temp);
                }

                return asRunData;

            };

            $scope.load = load;
            function load() {
                resetPlayer();
                $scope.hideMessage();
                $scope.asRunLog = null;

                var id = null;
                if (($scope.selectedBrand && $scope.selectedBrand.Id) || ($scope.selectedAdvertiser && $scope.selectedAdvertiser.Id)) {
                    id = $scope.current.brandOrAdvertiser.Id == 'Brand' ? $scope.selectedBrand.Id : $scope.selectedAdvertiser.Id;
                }

                Service('MediaHouseAsRunLog', {
                    channelId: $scope.current.channel.Id,
                    include: $scope.current.include.Id,
                    date: ValueFormatter.getServerStringDateWithoutTime($scope.current.customDate),
                    brandOrAdvertiserId: id,
                    brandOrAdvertiser: $scope.current.brandOrAdvertiser.Id,
                    showDuplicates: Boolean($scope.current.showDuplicates)
                }).then(function(asRunLog) {
                    $scope.asRunLog = getAsRunLogData(asRunLog.AsRunData);
                    setPriceDefinitionWarningTooltips();
                    if (!$scope.asRunLog.length) {
                        $scope.showMessage('NoData');
                    }
                }).catch(function() {
                    $scope.showMessage('Error');
                });
            }
            $scope.onDirectivesInit = function() {
                if ($scope.haveId($scope.current.channel) && $scope.haveId($scope.current.include) && $scope.current.customDate
                    && $scope.haveId($scope.current.brandOrAdvertiser)) {
                    initPageLoad();
                }
            };

            $scope.$on('channels-loaded', $scope.onDirectivesInit);

            $scope.advertiserFilterChange = function(selectedAdvertiser) {
                $scope.selectedAdvertiser = selectedAdvertiser;
                load();
            };

            $scope.brandFilterChange = function(selectedBrand) {
                $scope.selectedBrand = selectedBrand;
                load();
            };

            $scope.displayWarningTooltip = function(row) {
                return row.SongId && (!row.ProductId || !row.HavePriceDefinition);
            };

            $scope.getWarningTooltip = function(row) {
                if (!$scope.displayWarningTooltip(row)) { return ''; }
                else if (!row.ProductId) { return 'Product is not defined.'; }
                else if (row.PriceDefinitionTooltip) { return row.PriceDefinitionTooltip; }
            };

            function setPriceDefinitionWarningTooltips() {
                if ($scope.asRunLog) {
                    $scope.asRunLog.forEach(function(channel) {
                        channel.AsRunDetailData.forEach(function(row) {
                            if (row.ProductId && $scope.displayWarningTooltip(row)) {
                                var request = {
                                    channelId: row.ChannelId,
                                    productId: row.ProductId
                                };

                                return Service('GetPriceDefinitions', request).then(function(pricedef) {
                                    row.PriceDefinitionTooltip = pricedef.length
                                        ? 'Price for this hour is not defined.'
                                        : 'Prices for this channel are not defined.';
                                });
                            }

                        });
                    });
                }

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

        }
    ]);
