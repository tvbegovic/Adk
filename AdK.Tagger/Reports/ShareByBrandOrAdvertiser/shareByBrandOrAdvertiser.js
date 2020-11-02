angular.module('app.reports')
    .controller('shareByBrandOrAdvertiser', ['$scope', '$window', 'Service', 'ValueFormatter', 'BaseChartConfig',
        function($scope, $window, Service, ValueFormatter, BaseChartConfig) {
            var initPageLoad = _.once(load);
            $scope.loading = true;
            var serverResponse = null;
            var discreteBarChartOptions = _.merge(BaseChartConfig.getDiscreteBarChartOptions({ percentGraph: true }), {
                chart: {
                    x: function(d) { return d.Name; },
                    y: function(d) { return ValueFormatter.roundWithDecimalPlaces(d.Value, 1); }
                }
            });
            $scope.allMediaChartOptions = angular.copy(discreteBarChartOptions);
            $scope.tvChartOptions = angular.copy(discreteBarChartOptions);
            $scope.radioChartOptions = angular.copy(discreteBarChartOptions);
            var pieChartOptions = _.merge(BaseChartConfig.getPieChartOptions({ percentGraph: true }), {
                chart: {
                    x: function(d) { return d.Name + ' ' + ValueFormatter.toPercentageString(d.Value, 1); },
                    y: function(d) { return ValueFormatter.roundWithDecimalPlaces(d.Value, 1); }
                }
            });

            $scope.onDirectivesInit = function() {
                if ($scope.haveId($scope.current.industry) && $scope.haveId($scope.current.value) && $scope.haveId($scope.current.limit)
                    && $scope.haveId($scope.current.brandOrAdvertiser) && $scope.haveId($scope.current.market) && $scope.current.periodInfo.PeriodKind) {
                    initPageLoad();
                }
            };

            setChartOptions();
            $scope.load = load;
            function load() {
                $scope.loading = true;
                $scope.hideMessage();
                serverResponse = null;
                Service('MediaHouseShareByBrandOrAdvertiser', {
                    period: $scope.current.periodInfo,
                    industryId: $scope.current.industry.Id,
                    value: $scope.current.value.Id,
                    shareBy: $scope.current.brandOrAdvertiser.Id,
                    categories: $scope.current.categories,
                    marketId: $scope.current.market.Id,
                    limit: $scope.current.limit.Id
                }).then(function(shareData) {
                    $scope.reportData = {
                        PeriodStart: shareData.PeriodStart,
                        PeriodEnd: shareData.PeriodEnd
                    };

                    serverResponse = shareData;
                    setChartData();
                    if (!serverResponse.AllMedia || !serverResponse.AllMedia.length) {
                        $scope.showMessage('NoData');
                    }

                }).catch(function() {
                    $scope.showMessage('Error');
                }).finally(function() {
                    $scope.loading = false;
                });
            }


            $scope.changePieChartDisplay = function() {
                setChartOptions();
                setChartData();
            };

            function setChartData() {
                serverResponse = serverResponse || {};
                if ($scope.current.showPieCharts) {
                    $scope.allMedia = serverResponse.AllMedia;
                    $scope.tvMedia = serverResponse.TvMedia;
                    $scope.radioMedia = serverResponse.RadioMedia;
                } else {
                    //Convert pie chart data to bar chart data
                    $scope.allMedia = ValueFormatter.convertPieChartDataToDiscreteBarData(serverResponse.AllMedia, { sort: true });
                    $scope.tvMedia = ValueFormatter.convertPieChartDataToDiscreteBarData(serverResponse.TvMedia, { sort: true });
                    $scope.radioMedia = ValueFormatter.convertPieChartDataToDiscreteBarData(serverResponse.RadioMedia, { sort: true });
                }
                if($scope.allMedia && $scope.allMedia.length) {
                    $scope.allMediaChartOptions.chart.forceY =  $scope.allMedia[0].values.length ? [0, getMaxValue($scope.allMedia[0].values[0].Value)] : [0,100];
                }
                if($scope.allMedia && $scope.allMedia.length) {
                    $scope.tvChartOptions.chart.forceY =  $scope.tvMedia.length && $scope.tvMedia[0].values.length ? [0, getMaxValue($scope.tvMedia[0].values[0].Value)] : [0,100];
                }
                if($scope.allMedia && $scope.allMedia.length) {
                    $scope.radioChartOptions.chart.forceY =  $scope.radioMedia.length && $scope.radioMedia[0].values.length ? [0, getMaxValue($scope.radioMedia[0].values[0].Value)] : [0,100];
                }
               
            }

            function getMaxValue(val) {
                var max = val + 10;
                max = max - (max % 10)
                return max > 100 ? 100 : max;
            }

            function setChartOptions() {
                $scope.chartOptions = $scope.current.showPieCharts ? pieChartOptions : discreteBarChartOptions;
            }

        }]);
