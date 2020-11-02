angular.module('app.reports')
    .controller('activityByDaypartCtrl', ['$scope', 'Service', 'CurrentReport', 'ValueFormatter', 'BaseChartConfig',
        function ($scope, Service, CurrentReport, ValueFormatter, BaseChartConfig) {

            var activityByDaypartWithTotal = null;
            var activityByDaypartWithPercentage = null;
            var initPageLoad = _.once(load);
            $scope.loading = true;
			$scope.MaxTotal = 0;
            $scope.MaxPercentage = 100;

            var yAxisTickFormat = function (d) {
                    if ($scope.current.displayType.Id === 'Percentage') {
                        return ValueFormatter.toPercentageString(d);
                    }
                    else if ($scope.current.value.Id === 'Duration') {
                        return ValueFormatter.convertSecondsToHourFormat(d);
                    } else {
                        return ValueFormatter.toLocalString(d, true);
                    }
                };
            var barChartTooltipContent = function (d) {
                    var list = [];
                    for(var i in $scope.activityByDaypart) {
                        var chData = $scope.activityByDaypart[i];
                        list.push(_.find(chData.values, function(val) { return val.ChannelId == d.data.ChannelId; }));
                    }
                    var title = d.value;
                    return BaseChartConfig.buildBarChartTooltip(d, list, title, yAxisTickFormat);
                };

            function setNvd3Options() {
                var options = _.merge(BaseChartConfig.getHorizontalMultiBarOptions(), {
                    chart: {
                        x: function(d) { return d.ChannelName; },
                        y: function(d) {
                            if ($scope.current.displayType.Id === 'Percentage' && d.Value > 100) {
                                return 100;
                            }
                            return d.Value;
                        },
                        tooltip: {
                          contentGenerator: barChartTooltipContent
                        },
                        yAxis: {
                            tickFormat: yAxisTickFormat
                        }
                    }
                });

                if ($scope.current.displayType.Id === 'Percentage') {
                    
                    options.chart.forceY = [0, $scope.MaxPercentage];
                }
                else {

                    options.chart.forceY = [0, $scope.MaxTotal];
                }

                $scope.nvd3Options = options;
            }
            setNvd3Options();

            //Show data in table
            $scope.displayTable = false;
            $scope.showTable = function() {
                $scope.displayTable = true;
                load();
            };
            $scope.showChart = function() {
                $scope.displayTable = false;
                load();
            };
            $scope.tableData = null;
            $scope.tableChannels = null;

            $scope.load = load;
            function load() {
                $scope.loading = true;
                $scope.hideMessage();
                activityByDaypartWithTotal = null;
                activityByDaypartWithPercentage = null;
                $scope.tableData = null;

                var request = {
                    channelId: $scope.current.channel.Id,
                    include: $scope.current.include.Id,
                    value: $scope.current.value.Id,
                    period: $scope.current.periodInfo,
                    dayOfWeekRange: $scope.current.dayOfWeek.Id,
                    dayPart: $scope.current.dayPart.Id,
                    viewData: $scope.displayTable
                };

                Service('MediaHouseActivityByDaypart', request)
                    .then(function(activityByDaypart) {
                        $scope.abd = {
                            PeriodStart: activityByDaypart.PeriodStart,
                            PeriodEnd: activityByDaypart.PeriodEnd
                        };
                        if (activityByDaypart.ChartData.length) { 
                            if(!$scope.displayTable) {
                                activityByDaypartWithTotal = activityByDaypart;

                                activityByDaypartWithTotal = activityByDaypart.ChartData;
                                activityByDaypartWithPercentage = activityByDaypart.PercentageChartData;

                                $scope.MaxTotal = activityByDaypart.MaxTotalValue;
                                $scope.MaxPercentage = activityByDaypart.MaxPercentageValue;

                                setActivityByDaypart();

                            } else {
                                //Set focused channel to first position
                                var focusIndex = null;
                                var daypart = activityByDaypart.ChartData[0];
                                for(var j = 0; j < daypart.values.length; j++) {
                                    var val = daypart.values[j];
                                    if (val.ChannelId == $scope.current.channel.Id) { focusIndex = j; break;}
                                }
                                if(focusIndex) {
                                     for (var i = 0; i < activityByDaypart.ChartData.length; i++) {
                                        var dp = activityByDaypart.ChartData[i].values;
                                        var temp = dp[focusIndex];
                                        dp.splice(focusIndex, 1);
                                        dp.unshift(temp);
                                    }
                                }
                                $scope.total = setTotalTable(activityByDaypart);
                                $scope.tableData = activityByDaypart;
                            }
                        }
                        else {
                                $scope.showMessage('NoData');
                        }

                    }).catch(function() {
                        $scope.showMessage('Error');
                    }).finally(function() {
                        $scope.loading = false;
                    });
            }

            $scope.onDirectivesInit = function() {
                if ($scope.haveId($scope.current.channel) && $scope.haveId($scope.current.include)
                && $scope.haveId($scope.current.value) && $scope.haveId($scope.current.dayOfWeek)
                && $scope.haveId($scope.current.dayPart)  && $scope.current.periodInfo.PeriodKind 
                && $scope.haveId($scope.current.displayType)) {
                    initPageLoad();
                }
            };


            $scope.$on('channels-loaded', $scope.onDirectivesInit);

            function setActivityByDaypart() {
                if ($scope.current.displayType.Id === 'Percentage') {
                    $scope.activityByDaypart = activityByDaypartWithPercentage;
                } else {
                    $scope.activityByDaypart = activityByDaypartWithTotal;
                }

                setNvd3Options();

                if ($scope.activityByDaypart[0]) {
                    var noChannels = $scope.activityByDaypart[0].values.length;
                    var heightFactor = noChannels > 15 ? (noChannels > 25 ? 22 : 30) : 40;
                    var chartHeight = noChannels * heightFactor;
                    $scope.nvd3Options.chart.height = chartHeight > 200 ? chartHeight : 200;
                }

            }

            function setTotalTable(tableData) {

                var total = {
                    values: [],
                    totalCount: 0,
                    totalDuration: 0,
                    totalSpend: 0
                };
               tableData.ChartData.forEach(function(dayPart) {
                    dayPart.values.forEach(function(item, index) {
                        if(!total.values[index])
                            total.values[index] = {
                                Count: 0,
                                Duration: 0,
                                Spend: 0
                            };
                       total.values[index].Count += item.Count
                       total.values[index].Duration += item.Duration;
                       total.values[index].Spend += item.Spend;

                       total.totalCount += item.Count
                       total.totalDuration += item.Duration;
                       total.totalSpend += item.Spend;
                   });
               });
               return total;
            }

            $scope.onDisplayTypeChange = function() {
                if(!$scope.displayTable) {
                    setActivityByDaypart();
                }
            };

            $scope.getValue = function(val, type) {
                if (!val) { return ''; }
                if($scope.current.displayType.Id === 'Percentage') {
                    switch (type) {
                        case 1:
                            return ValueFormatter.toPercentageString((val / $scope.total.totalDuration) * 100);
                        case 2:
                            return ValueFormatter.toPercentageString((val / $scope.total.totalSpend) * 100);
                        default:
                            return ValueFormatter.toPercentageString((val / $scope.total.totalCount) * 100);
                    }
                } else {
                    switch (type) {
                        case 1:
                            return ValueFormatter.convertSecondsToHourFormat(val);
                        case 2:
                            return ValueFormatter.toLocalString(ValueFormatter.roundServerNumberString(val), true);
                        default:
                            return ValueFormatter.toLocalString(val, true);
                    }
                }
            };

            $scope.getTotalCount = function(dayPart) {
               var sum = 0;
               dayPart.values.forEach(function(item) {
                   sum += item.Count;
               });
               return $scope.getValue(sum, 0);
            };
             $scope.getTotalDuration = function(dayPart) {
                var sum = 0;
               dayPart.values.forEach(function(item) {
                   sum += item.Duration;
               });
               return $scope.getValue(sum, 1);
            };
             $scope.getTotalSpend = function(dayPart) {
                var sum = 0;
               dayPart.values.forEach(function(item) {
                   sum += item.Spend;
               });
               return $scope.getValue(sum, 2);
            };

        }]);
