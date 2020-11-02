angular.module('app.reports')
    .controller('marketOverviewCtrl', [
        '$scope', 'Service', 'MyChannels', 'BaseChartConfig', 'ValueFormatter',
        function($scope, Service, MyChannels, BaseChartConfig, ValueFormatter) {
            var initPageLoad = _.once(load);
            $scope.isTimeSpan = true;
            $scope.valueId = 'Spend';
            $scope.loading = true;
            var serverResponse = null;


            $scope.onDirectivesInit = function() {
                if ($scope.haveId($scope.current.periodBy) && $scope.haveId($scope.current.market) && $scope.current.periodInfo.PeriodKind) {
                    initPageLoad();
                }
            };

            var discreteBarChartOptions = _.merge(BaseChartConfig.getDiscreteBarChartOptions(), {
                chart: {
                    x: function(d) { return d.Key; },
                    y: function(d) { return d.Value; },
                    yAxis: {
                        tickFormat: function(d) {
                            return ValueFormatter.toLocalString(d, true);
                        }
                    }
                }
            });

            var pieChartOptions = _.merge(BaseChartConfig.getPieChartOptions(), {
                chart: {
                    x: function(d) { return d.Key; },
                    y: function(d) { return d.Value; },
                    labelType: function(d) {
                        var percent = (d.endAngle - d.startAngle) / (2 * Math.PI);
                        return d.data.Key + '\n' + d3.format(' %')(percent);
                    }
                }
            });

            $scope.lineChartOptions = _.merge(BaseChartConfig.getLineChartOptions(), {
                chart: {
                    xScale: d3.time.scale(),
                    xAxis: {
                        showMaxMin: false,
                        tickFormat: function(d) {
                            return d3.time.format('%b %Y')(new Date(d));
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

            setChartOptions();

            $scope.load = load;
            function load() {
                $scope.loading = true;
                $scope.hideMessage();
                serverResponse = null;
                delete $scope.marketOverview;

                switch ($scope.current.show.Id) {
                    //case 'AllMediaSpend':
                    //    $scope.isTimeSpan = false;
                    //    $scope.valueId = 'Spend';
                    //    break;
                    case 'RadioTimeSold':
                        $scope.isTimeSpan = true;
                        $scope.valueId = 'Duration';
                        break;
                    case 'RadioSpend':
                        $scope.isTimeSpan = false;
                        $scope.valueId = 'Spend';
                        break;
                    case 'TVTimeSold':
                        $scope.isTimeSpan = true;
                        $scope.valueId = 'Duration';
                        break;
                    case 'TVSpend':
                        $scope.isTimeSpan = false;
                        $scope.valueId = 'Spend';
                        break;
                    default:
                        $scope.isTimeSpan = false;
                        $scope.valueId = 'Spend';
                        break;
                }

                Service('MediaHouseMarketOverview', {
                    period: $scope.current.periodInfo,
                    sortByPreviousPeriod: $scope.current.sortByPreviousPeriod,
                    value: $scope.valueId,
                    by: $scope.current.periodBy.Id,
                    marketId: $scope.current.market.Id
                }).then(function(marketOverview) {
                    marketOverview = marketOverview || {};
                    $scope.marketOverview = {
                        PeriodStart: marketOverview.PeriodStart,
                        PeriodEnd: marketOverview.PeriodEnd
                    };
                    serverResponse = marketOverview;
                    setChartData();

                    if (!haveData()) { $scope.showMessage('NoData'); }
                }).catch(function() {
                    $scope.showMessage('Error');
                }).finally(function() {
                    $scope.loading = false;
                });
            }

            MyChannels.getChannels().then($scope.onChannelsLoad);

            $scope.changePieChartDisplay = function() {
                setChartOptions();
                setChartData();
            };

            $scope.toggleOthersDisplay = setChartData;

            function haveData() {
                return ($scope.allMediaBySales && $scope.allMediaBySales.length)
                    || ($scope.radioBySales && $scope.radioBySales.length)
                    || ($scope.televisionBySales && $scope.televisionBySales.length)
                    || ($scope.allMediaBySpotTime && $scope.allMediaBySpotTime.length)
                    || ($scope.radioBySpotTime && $scope.radioBySpotTime.length)
                    || ($scope.televisionBySpotTime && $scope.televisionBySpotTime.length);
            }

            function getServerResponseWithoutOthers() {
                if (!serverResponse) { return serverResponse; }

                var response = _.cloneDeep(serverResponse);
                removeOthersFromArray(response.AllMediaBySales);
                removeOthersFromArray(response.RadioBySales);
                removeOthersFromArray(response.TelevisionBySales);
                removeOthersFromArray(response.AllMediaBySpotTime);
                removeOthersFromArray(response.RadioBySpotTime);
                removeOthersFromArray(response.TelevisionBySpotTime);
                return response;
            }

            function removeOthersFromArray(array) {
                if (array && array.length) {
                    for (var i = 0; i < array.length; i++) {
                        if (array[i].Key === 'Others') {
                            array.splice(i, 1);
                            break;
                        }
                    }
                }

                return null;
            }

            function setChartData() {
                var data = $scope.current.showOthers ? serverResponse : getServerResponseWithoutOthers();

                if ($scope.current.showPieCharts) {
                    $scope.allMediaBySales = data.AllMediaBySales;
                    $scope.radioBySales = data.RadioBySales;
                    $scope.televisionBySales = data.TelevisionBySales;
                    $scope.allMediaBySpotTime = data.AllMediaBySpotTime;
                    $scope.radioBySpotTime = data.RadioBySpotTime;
                    $scope.televisionBySpotTime = data.TelevisionBySpotTime;
                } else {
                    $scope.allMediaBySales = ValueFormatter.convertPieChartDataToDiscreteBarData(data.AllMediaBySales, { sort: true });
                    $scope.radioBySales = ValueFormatter.convertPieChartDataToDiscreteBarData(data.RadioBySales, { sort: true });
                    $scope.televisionBySales = ValueFormatter.convertPieChartDataToDiscreteBarData(data.TelevisionBySales, { sort: true });
                    $scope.allMediaBySpotTime = ValueFormatter.convertPieChartDataToDiscreteBarData(data.AllMediaBySpotTime, { sort: true });
                    $scope.radioBySpotTime = ValueFormatter.convertPieChartDataToDiscreteBarData(data.RadioBySpotTime, { sort: true });
                    $scope.televisionBySpotTime = ValueFormatter.convertPieChartDataToDiscreteBarData(data.TelevisionBySpotTime, { sort: true });
                }

            }

            function setChartOptions() {
                $scope.chartOptions = $scope.current.showPieCharts ? pieChartOptions : discreteBarChartOptions;
            }
        }
    ]);
