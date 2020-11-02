'use strict';

angular.module('app.reports')
  .controller('shareOfAdvertisingActivityCtrl', ['$scope', '$window', 'Service', 'ValueFormatter', 'BaseChartConfig',
    function($scope, $window, Service, ValueFormatter, BaseChartConfig) {
      var initPageLoad = _.once(load);
      var chartsWithTotal = null;
      var chartsWithPercentage = null;

      $scope.loading = true;
      function formatValue(val) {
        if (isPercentageDisplay()) {
          return ValueFormatter.toPercentageString(val);
        } else if ($scope.current.value.Id === 'Duration') {
          return ValueFormatter.convertSecondsToHourFormat(val);
        } else {
          return ValueFormatter.toLocalString(val, true);
        }
      }

      var discreteBarChartOptions = _.merge(BaseChartConfig.getDiscreteBarChartOptions(), {
        chart: {
          x: function(d) { return d.Name; },
          y: function(d) { return d.Value; },
          tooltip: {
            valueFormatter: function(d) { return formatValue(d); }
          },
          yAxis: {
            tickFormat: function(d) { return formatValue(d); }
          }
        }
      });

      var pieChartOptions = _.merge(BaseChartConfig.getPieChartOptions(), {
        chart: {
          x: function(d) { return d.Name + ' ' + formatValue(d.Value); },
          y: function(d) { return d.Value; },
          tooltip: {
            valueFormatter: function(d) { return formatValue(d); },
            keyFormatter: function(d) {
              //remove value from name in tooltip
              var keys = d.split(' ');
              if (!keys) { return ''; }
              keys.pop();
              return keys.join(' ');
            }
          }
        }
      });

      function isPercentageDisplay() {
        return $scope.current.displayType.Id === 'Percentage';
      }

      $scope.onDirectivesInit = function() {
        if ($scope.haveId($scope.current.channel) && $scope.haveId($scope.current.include) && $scope.haveId($scope.current.value)
          && $scope.haveId($scope.current.dayOfWeek) && $scope.haveId($scope.current.dayPart) && $scope.current.periodInfo.PeriodKind) {
          initPageLoad();
        }
      };

      $scope.$on('channels-loaded', $scope.onDirectivesInit);

      setChartOptions();

      $scope.load = load;
      function load() {
        chartsWithTotal = null;
        chartsWithPercentage = null;
        $scope.loading = true;
        $scope.hideMessage();
        $scope.charts = [];

        var request = {
          channelId: $scope.current.channel.Id,
          include: $scope.current.include.Id,
          value: $scope.current.value.Id,
          period: $scope.current.periodInfo,
          dayOfWeekRange: $scope.current.dayOfWeek.Id,
          dayPart: $scope.current.dayPart.Id
        };

        Service('MediaHouseShareOfAdvertisingActivity', request)
          .then(function(response) {
            $scope.shareOfAdvActivity = {
              PeriodStart: response.PeriodStart,
              PeriodEnd: response.PeriodEnd
            };
            chartsWithTotal = response.Charts;
            chartsWithPercentage = response.PercentageCharts;
            setChartData();

            if (!$scope.charts.length) {
              $scope.showMessage('NoData');
            }
          }).catch(function() {
            $scope.showMessage('Error');
          }).finally(function() {
            $scope.loading = false;
          });
      }

      $scope.getChartName = function(chart) {
        var dateFrom = $scope.shareOfAdvActivity.PeriodStart;
        var dateTo = $scope.shareOfAdvActivity.PeriodEnd;

        return $scope.current.dayOfWeek.Name + ' ' + (chart.Name || '') + ' (' + dateFrom + ' - ' + dateTo + ')';
      };

      $scope.changePieChartDisplay = function() {
        setChartOptions();
        setChartData();
      };

      $scope.onDisplayTypeChange = function() {
        setChartOptions();
        setChartData();
      };


      function setChartData() {
        var charts = isPercentageDisplay() ? chartsWithPercentage : chartsWithTotal;

        if ($scope.current.showPieCharts) {
          $scope.charts = charts;
        } else {
          var barCharts = [];
          if (charts && charts.length) {
            charts.forEach(function(ch) {
              var chart = _.clone(ch);
              chart.Data = ValueFormatter.convertPieChartDataToDiscreteBarData(chart.Data, { sort: true });
              barCharts.push(chart);
            });

          }

          $scope.charts = barCharts;
        }

      }

      function setChartOptions() {
        $scope.chartOptions = $scope.current.showPieCharts ? pieChartOptions : discreteBarChartOptions;
      }

    }]);
