angular.module('app.reports')
    .controller('brandActivityByMediaHouseCtrl', ['$scope', 'Service', 'CurrentReport', 'ValueFormatter', 'BaseChartConfig', '$window',
        function ($scope, Service, CurrentReport, ValueFormatter, BaseChartConfig, $window) {
            var brandActivityByMediaHouseWithTotal = null;
            var brandActivityByMediaHouseWithPercentage = null;
            $scope.MaxTotal = 0;
            $scope.MaxPercentage = 100;
            var initPageLoad = _.once(load);
            $scope.loading = true;

            $scope.onDirectivesInit = function () {
                if ($scope.haveId($scope.current.industry) && $scope.haveId($scope.current.value)
                    && $scope.haveId($scope.current.limit) && $scope.haveId($scope.current.market)
                    && $scope.current.periodInfo.PeriodKind && $scope.haveId($scope.current.brandOrAdvertiser)) {
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
                    for(var i in $scope.brandActivityByMediaHouseChartData) {
                        var chData = $scope.brandActivityByMediaHouseChartData[i];
                        var item = _.find(chData.values, function(val) { return val.Name == d.data.Name && Boolean(val.y); });
                        if(item)
                            list.push(item);
                    }
                    var title = d.value;
                    return BaseChartConfig.buildBarChartTooltip(d, list, title, yAxisTickFormat);
                };
            function setNvd3Options() {
                var options = _.merge(BaseChartConfig.getHorizontalMultiBarOptions(), {
                    chart: {
                        x: function (d) { return d.Name; },
                        y: function (d) {
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

            $scope.load = load;
            function load() {
                $scope.loading = true;
                $scope.hideMessage();
                brandActivityByMediaHouseWithTotal = null;
                brandActivityByMediaHouseWithPercentage = null;

                var request = {
                    value: $scope.current.value.Id,
                    period: $scope.current.periodInfo,
                    industryId: $scope.current.industry.Id,
                    shareBy: $scope.current.brandOrAdvertiser.Id,
                    categories: $scope.current.categories,
                    marketId: $scope.current.market.Id,
                    limit: $scope.current.limit.Id
                };

                Service('MediaHouseBrandActivityByMediaHouse', request)
                    .then(function (brandActivityByMediaHouse) {


                        $scope.period = {
                            PeriodStart: brandActivityByMediaHouse.PeriodStart,
                            PeriodEnd: brandActivityByMediaHouse.PeriodEnd
                        };

                        if (brandActivityByMediaHouse.ChartData.length) {
                            brandActivityByMediaHouseWithTotal = brandActivityByMediaHouse.ChartData;
                            brandActivityByMediaHouseWithPercentage = brandActivityByMediaHouse.PercentageChartData;

                            $scope.MaxTotal = brandActivityByMediaHouse.MaxTotalValue;
                            $scope.MaxPercentage = brandActivityByMediaHouse.MaxPercentageValue;

                            setBrandActivityByMediaHouse();

                        } else {
                            $scope.showMessage('NoData');
                        }


                    }).catch(function () {
                        $scope.showMessage('Error');
                    }).finally(function () {
                        $scope.loading = false;
                    });
            }

            var onResize = _.debounce(function onResize() {
                $scope.nvd3Options.chart.width = $window.innerWidth > 1100 ? $window.innerWidth - 400 : 1100;
                $scope.$digest();
            }, 200);

            angular.element($window).on('resize', onResize);
            $scope.$on('$destroy', removeResizeEvent);

            function removeResizeEvent() {
                angular.element($window).off('resize', onResize);
            }

            function setBrandActivityByMediaHouse() {
                if ($scope.current.displayType.Id === 'Percentage') {
                    $scope.brandActivityByMediaHouseChartData = brandActivityByMediaHouseWithPercentage;
                } else {
                    $scope.brandActivityByMediaHouseChartData = brandActivityByMediaHouseWithTotal;

                }

                setNvd3Options();

                if ($scope.brandActivityByMediaHouseChartData[0]) {
                    var noBrands = $scope.brandActivityByMediaHouseChartData[0].values.length;
                    var heightFactor = noBrands > 10 ? (noBrands > 20 ? 22 : 50) : 70;
                    var chartHeight = noBrands * heightFactor;
                    $scope.nvd3Options.chart.height = chartHeight > 200 ? chartHeight : 200;
                    $scope.nvd3Options.chart.width = $window.innerWidth > 1100 ? $window.innerWidth - 400 : 1100;
                } else {
                    $scope.showMessage('NoData');
                }
            }


            $scope.onDisplayTypeChange = function () {
                setBrandActivityByMediaHouse();
            };

        }]);
