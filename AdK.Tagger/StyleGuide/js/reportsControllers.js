var pieChartData = [{ Name: 'One', Value: 10 }, { Name: 'Two', Value: 20 }, { Name: 'Three', Value: 20 }, { Name: 'Four', Value: 10 }, { Name: 'Five', Value: 20 }, { Name: 'Six', Value: 15 }, { Name: 'Seven', Value: 5 }];
//PIE CHART
angular.module('app').controller('pieChartCtrl', function($scope, BaseChartConfig, ValueFormatter) {
  $scope.chartOptions = _.merge(BaseChartConfig.getPieChartOptions({ percentGraph: true }),
    {
      chart: {
        x: function(d) { return d.Name + ' ' + ValueFormatter.toPercentageString(d.Value); },
        y: function(d) { return ValueFormatter.roundWithDecimalPlaces(d.Value, 1); }
      }
    });

  $scope.data = pieChartData;
});

//DONUT CHART
angular.module('app').controller('donutChartCtrl', function($scope, BaseChartConfig, ValueFormatter) {
  $scope.chartOptions = _.merge(BaseChartConfig.getDonutChartConfig({ percentGraph: true }),
    {
      chart: {
        x: function(d) { return d.Name + ' ' + ValueFormatter.toPercentageString(d.Value); },
        y: function(d) { return ValueFormatter.roundWithDecimalPlaces(d.Value, 1); }
      }
    });

  $scope.data = pieChartData;
});

//BAR CHART
angular.module('app').controller('barChartCtrl', function($scope, BaseChartConfig, ValueFormatter) {
  $scope.chartOptions = _.merge(BaseChartConfig.getDiscreteBarChartOptions({ percentGraph: true }),
    {
      chart: {
        x: function(d) { return d.Name; },
        y: function(d) { return ValueFormatter.roundWithDecimalPlaces(d.Value, 1); }
      }
    });

  $scope.data = ValueFormatter.convertPieChartDataToDiscreteBarData(pieChartData, { sort: true });
});
