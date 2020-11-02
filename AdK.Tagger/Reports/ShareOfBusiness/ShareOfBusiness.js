angular.module('app.reports')
  .controller('shareOfBusinessCtrl', ['$scope', '$window', 'Service', 'ValueFormatter', 'BaseChartConfig',
    function($scope, $window, Service, ValueFormatter, BaseChartConfig) {
      var initPageLoad = _.once(load);
      $scope.loading = true;
      $scope.horizontalLabel = true;
      $scope.totalSummary = null;

      $scope.totalCount = null;
      $scope.totalSpend = null;
      $scope.totalAirTime = null;

      var serverResponse = null;
      var discreteBarChartOptions = _.merge(BaseChartConfig.getDiscreteBarChartOptions({ percentGraph: true }), {
        chart: {
          x: function(d) { return d.Name; },
          y: function(d) { return ValueFormatter.roundWithDecimalPlaces(d.Value, 1); },
        }
      });
      var pieChartOptions = _.merge(BaseChartConfig.getPieChartOptions({ percentGraph: true }), {
        chart: {
          x: function(d) { return d.Name + ' ' + ValueFormatter.toPercentageString(d.Value); },
          y: function(d) { return ValueFormatter.roundWithDecimalPlaces(d.Value, 1); }
        }
      });

      $scope.onDirectivesInit = function() {
        if ($scope.haveId($scope.current.channel) && $scope.haveId($scope.current.include)
          && $scope.current.periodInfo.PeriodKind && $scope.haveId($scope.current.dayPart)) {
          initPageLoad();
        }
      };

      $scope.$on('channels-loaded', $scope.onDirectivesInit);


      setChartOptions();

      $scope.load = load;
      function load() {
        $scope.loading = true;
        $scope.hideMessage();
        serverResponse = null;
        $scope.charts = [];
        var request = {
          channelId: $scope.current.channel.Id,
          include: $scope.current.include.Id,
          period: $scope.current.periodInfo,
          dayPart: $scope.current.dayPart.Id
        };

        Service('MediaHouseShareOfBusiness', request)
          .then(function(response) {
            serverResponse = response;

            setChartData();
            setChartOptions();
            $scope.reportData = response;
            $scope.countTable = response.ShareTables[0];
            $scope.spendTable = response.ShareTables[1];
            $scope.airTimeTable = response.ShareTables[2];

            $scope.indexesToSortBy = [$scope.countTable.Headers[0].length - 1, $scope.spendTable.Headers[0].length - 1, $scope.airTimeTable.Headers[0].length - 1];

            $scope.totalSummary = $scope.reportData.SummaryTable.Rows.pop();

            $scope.totalCount = $scope.countTable.Rows.pop();
            $scope.totalSpend = $scope.spendTable.Rows.pop();
            $scope.totalAirTime = $scope.airTimeTable.Rows.pop();

            if (!response.CountChart.Data.length && !response.SpendChart.Data.length && !response.DurationChart.Data.length) {
              $scope.showMessage('NoData');
            }

          }).catch(function() {
            $scope.showMessage('Error');
          }).finally(function() {
            $scope.loading = false;
          });
      }

      $scope.getColumnClass = function(index) {
        if (index === 0) {
          return 'text';
        }
        return 'number';
      };

      $scope.changePieChartDisplay = function() {
        setChartData();
        setChartOptions();
      };

      //Sorting
      //Summary
      $scope.directionEnum = {
        Asc: 'Asc',
        Desc: 'Desc'
      };
      $scope.summarySortEnum = {
        Count: 1,
        Spend: 2,
        AirTime: 3
      };

      $scope.summarySorting = $scope.summarySortEnum.Spend;
      $scope.summarySortDirection = $scope.directionEnum.Asc;

      //Sorting comparator
      $scope.summaryComparator = function(row) {
        var value = parseFloat(row[$scope.summarySorting]);
        return $scope.summarySortDirection == $scope.directionEnum.Asc ? value : -value;
      };

      $scope.summarySortBy = function(val) {
        if (val == 0)// no sorting by channel name
          return;

        if ($scope.summarySorting == val) {
          $scope.summarySortDirection = $scope.summarySortDirection == $scope.directionEnum.Asc ? $scope.directionEnum.Desc : $scope.directionEnum.Asc
        }
        else {
          $scope.summarySortDirection = $scope.directionEnum.Asc;
          $scope.summarySorting = val;
        }
      }

      //Sorting
      //Shared tables

      $scope.indexesToSortBy = [0, 0, 0];
      $scope.shareTableDirection = [$scope.directionEnum.Asc, $scope.directionEnum.Asc, $scope.directionEnum.Asc];

      //Sorting comparator
      $scope.countTableComparator = function(row) {
        var value = parseFloat(row[$scope.indexesToSortBy[0]]);
        return $scope.shareTableDirection[0] == $scope.directionEnum.Asc ? value : -value;
      };
      $scope.spendTableComparator = function(row) {
        var value = parseFloat(row[$scope.indexesToSortBy[1]]);
        return $scope.shareTableDirection[1] == $scope.directionEnum.Asc ? value : -value;
      };
      $scope.airTimeTableComparator = function(row) {
        var value = parseFloat(row[$scope.indexesToSortBy[2]]);
        return $scope.shareTableDirection[2] == $scope.directionEnum.Asc ? value : -value;
      };
      $scope.shareTableSortBy = function(tableIndex, columnIndex) {
        if (columnIndex == 0)// no sorting by channel name
          return;
        if ($scope.indexesToSortBy[tableIndex] == columnIndex) {
          $scope.shareTableDirection[tableIndex] = $scope.shareTableDirection[tableIndex] == $scope.directionEnum.Asc ? $scope.directionEnum.Desc : $scope.directionEnum.Asc;
        }
        else {
          $scope.indexesToSortBy[tableIndex] = columnIndex;
          $scope.shareTableDirection[tableIndex] = $scope.directionEnum.Asc;
        }
      }

      $scope.getValue = function(val, index) {
        if (!val) return '';

        switch (index) {
          case $scope.summarySortEnum.AirTime:
            return ValueFormatter.convertSecondsToHourFormat(val);
          case $scope.summarySortEnum.Count:
            return ValueFormatter.toLocalString(ValueFormatter.roundServerNumberString(val), true);
          default:
            return ValueFormatter.toLocalString(val, true);
        }
      };

      function setChartData() {
        serverResponse = serverResponse || {};
        if ($scope.current.showPieCharts) {
          $scope.countChart = serverResponse.CountChart ? serverResponse.CountChart.Data : null;
          $scope.spendChart = serverResponse.SpendChart ? serverResponse.SpendChart.Data : null;
          $scope.durationChart = serverResponse.DurationChart ? serverResponse.DurationChart.Data : null;
        } else {
          //Convert pie chart data to bar chart data
          $scope.countChart = serverResponse.CountChart ?
            ValueFormatter.convertPieChartDataToDiscreteBarData(serverResponse.CountChart.Data, { sort: true })
            : null;

          $scope.spendChart = serverResponse.SpendChart ?
            ValueFormatter.convertPieChartDataToDiscreteBarData(serverResponse.SpendChart.Data, { sort: true })
            : null;
          $scope.durationChart = serverResponse.DurationChart ?
            ValueFormatter.convertPieChartDataToDiscreteBarData(serverResponse.DurationChart.Data, { sort: true })
            : null;
        }
      }

      function setChartOptions() {
        $scope.chartOptionsCount = $scope.current.showPieCharts ? pieChartOptions : discreteBarChartOptions;
        $scope.chartOptionsSpend = $scope.current.showPieCharts ? pieChartOptions : angular.copy(discreteBarChartOptions);
        $scope.chartOptionsAirTime = $scope.current.showPieCharts ? pieChartOptions : angular.copy(discreteBarChartOptions);

        if($scope.countChart && $scope.spendChart && $scope.durationChart) {
            $scope.chartOptionsCount.chart.forceY = [0, getMaxPercent($scope.countChart[0].values[0].Value)];
            $scope.chartOptionsSpend.chart.forceY = [0, getMaxPercent($scope.spendChart[0].values[0].Value)];
            $scope.chartOptionsAirTime.chart.forceY = [0, getMaxPercent($scope.durationChart[0].values[0].Value)];
        } else {
            $scope.chartOptionsCount.chart.forceY = [0,100];
            $scope.chartOptionsSpend.chart.forceY = [0,100];
            $scope.chartOptionsAirTime.chart.forceY = [0,100];
        }
      }

      function getMaxPercent(val) {
        var max = val + 10;
        max = max - (max % 10)
        return max > 100 ? 100 : max;
      }

    }]);
