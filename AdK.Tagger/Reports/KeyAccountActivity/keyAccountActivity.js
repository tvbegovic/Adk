angular.module('app.reports')
  .controller('keyAccountActivityCtrl', ['$scope', '$window', 'Service', 'CurrentReport', 'd3ChartLabels', 'ValueFormatter', 'BaseChartConfig',
    function ($scope, $window, Service, CurrentReport, d3ChartLabels, ValueFormatter, BaseChartConfig) {
        var keyAccountActivityWithTotal = null;
        var keyAccountActivityWithPercentage = null;
        var initPageLoad = _.once(load);
        var CHART_LABEL_UPDATE_TIMEOUT = 200;
        $scope.MaxTotal = 0;
        $scope.MaxPercentage = 100;
        $scope.loading = true;

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
                    for(var i in $scope.keyAccountActivity) {
                        var chData = $scope.keyAccountActivity[i];
                        list.push(_.find(chData.values, function(val) { return val.BrandName == d.data.BrandName; }));
                    }
                    var title = d.value;
                    return BaseChartConfig.buildBarChartTooltip(d, list, title, yAxisTickFormat);
                };

        function setNvd3Options() {
            var options = _.merge(BaseChartConfig.getHorizontalMultiBarOptions(), {
                chart: {
                    noData: '',
                    x: function (d) { return d.BrandName; },
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
                    },
                    dispatch: {
                        stateChange: updateChartLabels.bind(null, CHART_LABEL_UPDATE_TIMEOUT)
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

        $scope.toggleChartLabels = function () {
            if ($scope.current.showChartLabels) {
                updateChartLabels();
            } else {
                d3ChartLabels.removeAllLabels();
            }
        };

        angular.element($window).on('resize', _.debounce(updateChartLabels, 50));

        $scope.onDirectivesInit = function () {
            if ($scope.haveId($scope.current.channel) && $scope.haveId($scope.current.include) && $scope.haveId($scope.current.value)
              && $scope.current.periodInfo.PeriodKind) {
                initPageLoad();
            }
        };

        $scope.$on('channels-loaded', $scope.onDirectivesInit);

        $scope.load = load;
        function load() {
            $scope.loading = true;
            $scope.hideMessage();
            keyAccountActivityWithTotal = null;
            keyAccountActivityWithPercentage = null;
            var request = {
                channelId: $scope.current.channel.Id,
                include: $scope.current.include.Id,
                value: $scope.current.value.Id,
                period: $scope.current.periodInfo,
                showAllAccounts: Boolean($scope.current.showAllAccounts)
            };

            Service('MediaHouseKeyAccountActivity', request)
              .then(function (keyAccountActivity) {
                  $scope.kaa = {
                      PeriodStart: keyAccountActivity.PeriodStart,
                      PeriodEnd: keyAccountActivity.PeriodEnd
                  };


                  if (keyAccountActivity.ChartData.length) {

                      keyAccountActivityWithTotal = keyAccountActivity.ChartData;
                      keyAccountActivityWithPercentage = keyAccountActivity.PercentageChartData;

                      $scope.MaxTotal = keyAccountActivity.MaxTotalValue;
                      $scope.MaxPercentage = keyAccountActivity.MaxPercentageValue;

                      setKeyAccountActivity();


                  } else {
                      $scope.showMessage('NoData');
                  }
              }).catch(function () {
                  $scope.showMessage('Error');
              })
              .finally(function () {
                  $scope.loading = false;
              });
        }

        $scope.onDisplayTypeChange = function () {
            setKeyAccountActivity();
            updateChartLabels();
        };

        function setKeyAccountActivity() {
            if ($scope.current.displayType.Id === 'Percentage') {
                $scope.keyAccountActivity = keyAccountActivityWithPercentage;
            } else {
                $scope.keyAccountActivity = keyAccountActivityWithTotal;
            }

            setNvd3Options();


            if ($scope.keyAccountActivity[0]) {
                var noBrands = $scope.keyAccountActivity[0].values.length;
                var heightFactor = noBrands > 15 ? (noBrands > 25 ? 22 : 30) : 40;
                var chartHeight = noBrands * heightFactor;
                $scope.nvd3Options.chart.height = chartHeight > 200 ? chartHeight : 200;
                updateChartLabels(CHART_LABEL_UPDATE_TIMEOUT);
            }

        }

        function updateChartLabels(timeout) {
            timeout = timeout || 0;
            var displayType = $scope.current.displayType.Id;
            if ($scope.current.showChartLabels) {
                d3ChartLabels.removeAllLabels();
                d3ChartLabels.addLabelsToChart({
                    selector: '.nv-multiBarHorizontalChart',
                    type: $scope.nvd3Options.chart.type,
                    animationTime: $scope.nvd3Options.chart.duration + timeout,
                    value: displayType === 'Percentage' ? displayType : $scope.current.value.Id
                });
            }
        }

    }]);
