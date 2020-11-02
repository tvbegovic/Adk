angular.module('app.reports')
    .controller('market12MonthTrendCtrl', [
        '$scope', '$timeout', 'Service', 'MyChannels', 'BaseChartConfig', 'ValueFormatter',
        function($scope, $timeout, Service, MyChannels, BaseChartConfig, ValueFormatter) {
            var initPageLoad = _.once(load);
            $scope.valueId = 'Spend';
            $scope.mediaType = '';
            $scope.loading = true;
            $scope.show = 0;
            $scope.onDirectivesInit = function() {
                if ($scope.haveId($scope.current.show) && $scope.haveId($scope.current.market)) {
                    initPageLoad();
                }
            }

            $scope.lineChartOptions = _.merge(BaseChartConfig.getLineChartOptions(), {
                chart: {
                    xScale: d3.time.scale(),
                    xAxis: {
                        showMaxMin: false,
                        tickFormat: function(d) {
                            return d3.time.format('%b-%Y')(new Date(d));
                        }
                    },
                    yAxis: {
                        showMaxMin: false,
                        axisLabelDistance: -10,
                        tickFormat: function(d) {
                            if ($scope.isTimeSpan) {
                                return ValueFormatter.convertSecondsToHourFormat(d);
                            } else {
                                return ValueFormatter.toLocalString(d, true);
                            }
                        }
                    }
                }
            });

            var reportData = null;
            $scope.load = load;
            function load() {
                $scope.loading = true;
                $scope.hideMessage();
                $scope.marketOverviewTrendList = [];

                switch ($scope.current.show.Id) {
                    //case 'AllMediaSpend':
                    //    $scope.isTimeSpan = false;
                    //    $scope.valueId = 'Spend';
                    //    break;
                    case 'RadioTimeSold':
                        $scope.isTimeSpan = true;
                        $scope.valueId = 'Duration';
                        $scope.mediaType = 'Radio';
                        break;
                    case 'RadioSpend':
                        $scope.isTimeSpan = false;
                        $scope.valueId = 'Spend';
                        $scope.mediaType = 'Radio';
                        break;
                    case 'TVTimeSold':
                        $scope.isTimeSpan = true;
                        $scope.valueId = 'Duration';
                        $scope.mediaType = 'TV';
                        break;
                    case 'TVSpend':
                        $scope.isTimeSpan = false;
                        $scope.valueId = 'Spend';
                        $scope.mediaType = 'TV';
                        break;
                    default:
                        $scope.isTimeSpan = false;
                        $scope.valueId = 'Spend';
                        $scope.mediaType = '';
                        break;
                }

                Service('MediaHouseMarketTrend', {
                    value: $scope.valueId,
                    mediaType: $scope.mediaType,
                    marketId: $scope.current.market.Id
                }).then(function(data) {
                    reportData = data;
                    $scope.refreshChart($scope.show);
                    if (!data.MarketOverviewTrendList || !data.MarketOverviewTrendList.length) {
                        $scope.showMessage('NoData');
                    }
                }).catch(function() {
                    $scope.showMessage('Error');
                }).finally(function() {
                    $scope.loading = false;
                });
            }
            var tempReportData = null;
            $scope.refreshChart = function(val) {
                $scope.show = val;

                if (val == 1) {
                    tempReportData = angular.copy(reportData.MarketOverviewTrendList);
                    for (var c in tempReportData) {
                        var channel = tempReportData[c];
                        for (var v in channel.Values) {
                            var value = channel.Values[v];
                            //get days in month
                            var date = new Date(value.Key);
                            var daysInMonth = new Date(date.getFullYear(), date.getMonth(), 0).getDate();
                            value.Value = value.Value / daysInMonth;
                        }
                    }
                    $scope.marketOverviewTrendList = ValueFormatter.convertServerLineChartData(tempReportData);
                }
                else {
                    $scope.marketOverviewTrendList = ValueFormatter.convertServerLineChartData(reportData.MarketOverviewTrendList);
                }
            }

            $scope.hideChartNoData = function() {
                return $scope.loading ||
                    ($scope.allMediaBySales && $scope.allMediaBySales.length)
                    || ($scope.radioBySales && $scope.radioBySales.length)
                    || ($scope.televisionBySales && $scope.televisionBySales.length)
                    || ($scope.allMediaBySpotTime && $scope.allMediaBySpotTime.length)
                    || ($scope.radioBySpotTime && $scope.radioBySpotTime.length)
                    || ($scope.televisionBySpotTime && $scope.televisionBySpotTime.length);
            };

            MyChannels.getChannels().then(function(channels) {
                $scope.channels = channels;
                if (channels.length) {
                    $scope.onDirectivesInit();
                }
            });
        }
    ]);
