angular.module('app.reports')
    .controller('brandActivityByWeekdayCtrl', ['$scope', 'Service', 'CurrentReport', 'ValueFormatter', 'BaseChartConfig',
        function ($scope, Service, CurrentReport, ValueFormatter, BaseChartConfig) {
            var brandActivityByWeekdayWithTotal = null;
            var brandActivityByWeekdayWithPercentage = null;
            $scope.MaxTotal = 0;
            $scope.MaxPercentage = 100;
            var initPageLoad = _.once(load);
            $scope.loading = true;

            $scope.onDirectivesInit = function () {
                if ($scope.haveId($scope.current.industry) && $scope.haveId($scope.current.value)
                    && $scope.haveId($scope.current.limit) && $scope.haveId($scope.current.market)
                    && $scope.current.periodInfo.PeriodKind) {
                    initPageLoad();
                }
            };

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
                    for(var i in $scope.brandActivityByWeekday) {
                        var chData = $scope.brandActivityByWeekday[i];
                        list.push(_.find(chData.values, function(val) { return val.BrandName == d.data.BrandName; }));
                    }
                    var title = d.value;
                    return BaseChartConfig.buildBarChartTooltip(d, list, title, yAxisTickFormat);
                };

            function setNvd3Options() {
                var options = _.merge(BaseChartConfig.getHorizontalMultiBarOptions(), {
                    chart: {
                         tooltip: {
                          contentGenerator: barChartTooltipContent
                        },
                        x: function (d) { return d.BrandName; },
                        y: function (d) {
                            if ($scope.current.displayType.Id === 'Percentage' && d.Value > 100) {
                                return 100;
                            }
                            return d.Value;
                        },
                        yAxis: {
                            tickFormat: yAxisTickFormat,
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

            $scope.load = load;
            function load() {
                $scope.loading = true;
                $scope.hideMessage();
                brandActivityByWeekdayWithTotal = null;
                brandActivityByWeekdayWithPercentage = null;

                var request = {
                    value: $scope.current.value.Id,
                    period: $scope.current.periodInfo,
                    industryId: $scope.current.industry.Id,
                    categories: $scope.current.categories,
                    marketId: $scope.current.market.Id,
                    limit: $scope.current.limit.Id
                };

                Service('MediaHouseBrandActivityByWeekday', request)
                    .then(function (brandActivityByWeekday) {
                        $scope.babwd = brandActivityByWeekday;

                        if (brandActivityByWeekday.ChartData.length) {
                            brandActivityByWeekdayWithTotal = brandActivityByWeekday.ChartData;
                            brandActivityByWeekdayWithPercentage = brandActivityByWeekday.PercentageChartData;
                            $scope.MaxTotal = brandActivityByWeekday.MaxTotalValue;
                            $scope.MaxPercentage = brandActivityByWeekday.MaxPercentageValue;
                            setBrandActivityByWeekday();
                        }
                    }).catch(function () {
                        $scope.showMessage('Error');
                    }).finally(function () {
                        $scope.loading = false;
                    });
            }

            function setBrandActivityByWeekday() {
                if ($scope.current.displayType.Id === 'Percentage') {
                    $scope.brandActivityByWeekday = brandActivityByWeekdayWithPercentage;
                } else {
                    $scope.brandActivityByWeekday = brandActivityByWeekdayWithTotal;
                }

                setNvd3Options();


                if ($scope.brandActivityByWeekday[0]) {
                    var noBrands = $scope.brandActivityByWeekday[0].values.length;
                    var heightFactor = noBrands > 15 ? (noBrands > 25 ? 22 : 30) : 40;
                    var chartHeight = noBrands * heightFactor;
                    $scope.nvd3Options.chart.height = chartHeight > 200 ? chartHeight : 200;

                }
            }


            $scope.onDisplayTypeChange = function () {
                setBrandActivityByWeekday();


            };

        }]);
