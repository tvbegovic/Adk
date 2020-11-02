angular.module('app.reports')
    .controller('advertisingActivityTrendCtrl', ['$scope', '$window', 'Service', 'd3ChartLabels', 'ValueFormatter', 'BaseChartConfig', 'Pager',
        function($scope, $window, Service, d3ChartLabels, ValueFormatter, BaseChartConfig, Pager) {
            $scope.AdvertisingActivities = null;
            $scope.pager = new Pager();
            $scope.loading = true;
            $scope.displayTable = false;
            $scope.tableData = null;
            $scope.totalRow = null;
            var initPageLoad = _.once(load);

            $scope.onDirectivesInit = function() {
                if ($scope.haveId($scope.current.channel) && $scope.haveId($scope.current.include) && $scope.haveId($scope.current.value)
                    && $scope.current.periodInfo.PeriodKind && $scope.current.timeFrom != null && $scope.current.timeTo != null) {
                    initPageLoad();
                }
            };

            $scope.$on('channels-loaded', $scope.onDirectivesInit);

            $scope.load = load;
            function load() {
                $scope.loading = true;
                $scope.hideMessage();
                var data = {
                    channelId: $scope.current.channel.Id,
                    include: $scope.current.include.Id,
                    value: $scope.current.value.Id,
                    period: $scope.current.periodInfo,
                    timeFrom: $scope.current.timeFrom,
                    timeTo: $scope.current.timeTo
                };

                Service('MediaHouseAdvertisingActivityTrend', { reportMetadata: data })
                    .then(function(activityTrend) {
                        if (activityTrend.AdvertisingActivities.length) {
                            $scope.activityTrend = {
                                PeriodStart: activityTrend.PeriodStart,
                                PeriodEnd: activityTrend.PeriodEnd,
                                Channels: activityTrend.Channels
                            };

                            for (var i = 0; i < activityTrend.AdvertisingActivities.length; i++) {
                                activityTrend.AdvertisingActivities[i].Key = getChannelName(activityTrend.AdvertisingActivities[i].ChannelId);
                            }
                            $scope.tableData = getTableData(activityTrend.AdvertisingActivities);
                            $scope.pager.reset();
                            $scope.pager.setItemCount($scope.tableData.Data ? $scope.tableData.Data.length : 0);
                            $scope.AdvertisingActivities = ValueFormatter.convertServerLineChartData(activityTrend.AdvertisingActivities);
                            $scope.barChartOptions.chart.reduceXTicks = $scope.AdvertisingActivities[0].values.length > 100;
                            updateChartLabels(450);
                        } else {
                            $scope.showMessage('NoData');
                        }
                    }).catch(function() {
                        $scope.showMessage('Error');
                    }).finally(function() {
                        $scope.loading = false;
                    });
            }

            function updateChartLabels(timeout) {
                if ($scope.current.showChartLabels && $scope.barChartOptions.chart.stacked) {
                    timeout = timeout || 0;
                    d3ChartLabels.removeAllLabels();
                    d3ChartLabels.addLabelsToChart({
                        selector: '.nv-multiBarWithLegend',
                        type: $scope.barChartOptions.chart.type,
                        animationTime: $scope.barChartOptions.chart.duration + timeout,
                        value: $scope.current.value.Id
                    });
                } else {
                    d3ChartLabels.removeAllLabels();
                }
            }

            angular.element($window).on('resize', _.debounce(updateChartLabels, 50));

            function getChannelName(id) {
                var chanel = _.find($scope.activityTrend.Channels, function(chanel) { return chanel.Id == id; });
                return chanel ? chanel.Name : '';
            }

            //Groups data by date
            function getTableData(data) {
                var grandTotal = 0;
                channelTotal = [];
                var totalRow = { Values: [], Total: null }
                var tableData = {TotalRow: null, Data: []};

                //Set focused channel as first in channel list
                var focusIndex = null;
                for (var i = 0; i < data.length; i++) {
                    if (data[i].ChannelId == $scope.current.channel.Id) { focusIndex = i; break;}
                }
                if (focusIndex) {
                    var temp = data[focusIndex];
                    data.splice(focusIndex, 1);
                    data.unshift(temp);
                }

                for(var i in data[0].Values) {
                    var date = {Date: data[0].Values[i].Key, Values: [], Total: null};
                    var total = 0;
                    for(var ch in data) {
                        var item = data[ch];
                        //Sum values for channel
                        channelTotal[ch] = channelTotal[ch] ? channelTotal[ch] + item.Values[i].Value : item.Values[i].Value;
                        grandTotal += item.Values[i].Value;
                        //Get total value
                        total += item.Values[i].Value;
                        date.Values.push({Value: item.Values[i].Value ? yAxisTickFormat(item.Values[i].Value) : '', ChannelName: item.Key});
                    }
                    date.Total = total ? yAxisTickFormat(total) : '';
                    tableData.Data.push(date);
               }
               //Fill total row values
               for(var t in channelTotal) {
                totalRow.Values[t] = channelTotal[t] ?  yAxisTickFormat(channelTotal[t]) : '';
               }
               totalRow.Total = grandTotal ?  yAxisTickFormat(grandTotal) : '';
               tableData.TotalRow = totalRow;

               return tableData;
            }

            function yAxisTickFormat(d) {
                switch ($scope.current.value.Id) {
                    case 'Duration':
                        return ValueFormatter.convertSecondsToHourFormat(d);
                    default:
                        return ValueFormatter.toLocalString(d, true);
                }
            }

            $scope.lineChartOptions = _.merge(BaseChartConfig.getLineChartOptions(), {
                chart: {
                    xScale: d3.time.scale(),
                    reduceXTicks: false,
                    xAxis: {
                        showMaxMin: false,
                        tickFormat: function(d) {
                            return d3.time.format('%b %d')(new Date(d));
                        }
                    },
                    yAxis: {
                        showMaxMin: false,
                        axisLabelDistance: -10,
                        tickFormat: function(d) {
                            if ($scope.current.value.Id == 'Duration') {
                                return ValueFormatter.convertSecondsToHourFormat(d);
                            } else {
                                return ValueFormatter.toLocalString(d, true);
                            }
                        }
                    }
                }
            });
           
            var buildTooltip = function(d, channels) {
                var openDate = '<table><thead><tr><td colspan="3"><strong class="x-value">';
                var date = d3.time.format('%b %d')(new Date(d.data.x._i));;
                var closeDate = '</strong></td></tr></thead>'
                var tbody = '<tbody>';
                var colors = d3.scale.category20().range();
                var channelsRows = '';
                for(var c in channels) {
                    var ch = channels[c];
                    var highlight = ch.series == d.data.series ? 'class="highlight"' : '';
                    channelsRows += '<tr ' + highlight + '> <td class="legend-color-guide" style="border-bottom-color: rgb(121, 173, 210); border-top-color: rgb(121, 173, 210);">';
                    channelsRows += '<div style="background-color:' + colors[c] +';"></div></td>';
                    channelsRows += '<td class="key" style="border-bottom-color: rgb(121, 173, 210); border-top-color: rgb(121, 173, 210);">';
                    channelsRows += ch.key + '</td><td class="value" style="border-bottom-color: rgb(121, 173, 210); border-top-color: rgb(121, 173, 210);">' + yAxisTickFormat(ch.y) + '</td></tr>';
                }
                var closeTable = '</tbody></table>';
                return openDate + date + closeDate + tbody + channelsRows + closeTable;
            }

            var barChartTooltipContent = function (d) {
                var channels = [];
                for(var i in $scope.AdvertisingActivities) {
                    var chData = $scope.AdvertisingActivities[i];
                    channels.push(_.find(chData.values, function(val) { return val.x._i == d.data.x._i; }));
                }
                return buildTooltip(d, channels);
            }

            $scope.barChartOptions = {
                chart: {
                    type: 'multiBarChart',
                    height: 550,
                    reduceXTicks: false,
                    clipEdge: true,
                    duration: 500,
                    stacked: true,
                    showControls: false,
                    tooltip: {
                      contentGenerator: barChartTooltipContent
                    },
                    xAxis: {
                        showMaxMin: false,
                        rotateLabels: -45,
                        tickFormat: function(d) {
                            return d3.time.format('%b %d')(new Date(d));
                        }
                    },
                    forceY: $scope.AdvertisingActivities && $scope.AdvertisingActivities.length ? [0, d3.max($scope.AdvertisingActivities, function(d) {
                        return d.y;
                    })] : [0, 5],
                    yAxis: {
                        axisLabelDistance: -20,
                        tickFormat: yAxisTickFormat
                    },
                    legend: {
                        dispatch: {
                            stateChange: function() {
                                updateChartLabels(450);
                            }
                        }
                    }
                }
            };
            $scope.toggleChartLabels = function() {
                if ($scope.current.showChartLabels) {
                    updateChartLabels();
                } else {
                    d3ChartLabels.removeAllLabels();
                }
            };
            $scope.chart = 0;
            $scope.chartOptions = $scope.barChartOptions;
            $scope.changeChartType = function(val) {
                switch (val) {
                    case 0:
                        $scope.barChartOptions.chart.stacked = true;
                        $scope.chartOptions = $scope.barChartOptions;
                        updateChartLabels(450);
                        break;
                    case 1:
                        $scope.barChartOptions.chart.stacked = false;
                        $scope.chartOptions = $scope.barChartOptions;
                        updateChartLabels();
                        break;
                    case 2:
                        $scope.chartOptions = $scope.lineChartOptions;
                        updateChartLabels();
                        break;
                }
            }

        }]);
