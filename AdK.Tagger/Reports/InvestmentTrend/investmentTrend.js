angular.module('app.reports')
  .controller('investmentTrendCtrl', ['$scope', 'Service', 'ValueFormatter', 'BaseChartConfig', 'MyChannels',
    function($scope, Service, ValueFormatter, BaseChartConfig, MyChannels) {
      var initPageLoad = _.once(load);
      $scope.loading = true;

      $scope.nvd3Options = _.merge(BaseChartConfig.getHorizontalMultiBarOptions(), {
        chart: {}
      });

      $scope.onDirectivesInit = function() {
        if ($scope.haveId($scope.current.media) && $scope.haveId($scope.current.industry)
          && $scope.haveId($scope.current.value) && $scope.haveId($scope.current.brandOrAdvertiser)
          && $scope.haveId($scope.current.limit) && $scope.haveId($scope.current.market)) {
          initPageLoad();
        }
      };

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
              if ($scope.current.value.Id === 'Duration') {
                return ValueFormatter.convertSecondsToHourFormat(d);
              } else {
                return ValueFormatter.toLocalString(d, true);
              }
            }
          }
        }
      });

      MyChannels.getChannels().then($scope.onChannelsLoad);

      $scope.load = load;
      function load() {
        $scope.loading = true;
        $scope.hideMessage();

        var request = {
          value: $scope.current.value.Id,
          industryId: $scope.current.industry.Id,
          media: $scope.current.media.Id,
          shareBy: $scope.current.brandOrAdvertiser.Id,
          categories: $scope.current.categories,
          marketId: $scope.current.market.Id,
          limit: $scope.current.limit.Id
        };

        Service('MediaHouseInvestmentTrend', request)
          .then(function(response) {
            if (response.InvestmentTrendLineChartData.length) {
              $scope.lineChartData = ValueFormatter.convertServerLineChartData(response.InvestmentTrendLineChartData);
            } else {
              $scope.showMessage('NoData');
            }
          }).catch(function() {
            $scope.lineChartData = [];
            $scope.showMessage('Error');
          }).finally(function() {
            $scope.loading = false;
          });
      }

    }]);
