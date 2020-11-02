angular.module('app', ['ngRoute', 'nvd3'])
  .config(function($routeProvider) {
    $routeProvider
      .when('/', {
        templateUrl: 'markup/gettingstarted.html'
      })
      .when('/fonts', {
        templateUrl: 'markup/reports/fonts.html'
      })
      .when('/formating', {
        templateUrl: 'markup/reports/formating.html'
      })
      .when('/messages', {
        templateUrl: 'markup/reports/messages.html'
      })
      .when('/filters', {
        templateUrl: 'markup/reports/filters.html'
      })
      .when('/tables', {
        templateUrl: 'markup/reports/tables.html'
      })
      .when('/barchart', {
        templateUrl: 'markup/reports/barchart.html',
        controller: 'barChartCtrl'
      })
      .when('/piechart', {
        templateUrl: 'markup/reports/piechart.html',
        controller: 'pieChartCtrl'
      })
      .when('/donutchart', {
        templateUrl: 'markup/reports/donutChart.html',
        pieChartCtrl: 'donutChartController'
      })
      .when('/dates', {
        templateUrl: 'markup/reports/dates.html'
      });
  });

angular.module('app').controller('StyleGuideCtrl', function($scope) {

  $scope.styleGuideSections = [
    {
      heading: 'Report Viewer',
      sectionPath: 'reports/',
      content: 'report',
      subNavigation: [
        {
          heading: 'Fonts',
          content: 'fonts'
        },
        {
          heading: 'Formating/Numbers',
          content: 'formating'
        },
        {
          heading: 'Messages',
          content: 'messages'
        },
        {
          heading: 'Filters',
          content: 'filters'
        },
        {
          heading: 'Tables',
          content: 'tables'
        },
        {
          heading: 'Bar Chart',
          content: 'barchart'
        },
        {
          heading: 'Pie Chart',
          content: 'pieChart'
        },
        {
          heading: 'Donut Chart',
          content: 'donutChart'
        },
        {
          heading: 'Dates',
          content: 'dates'
        }
        // {
        //   heading: 'Horizontal Bar Chart',
        //   content: 'horizontalBarChart'
        // }
      ]
    }];

  $scope.activePage = 'markup/gettingstarted.html';
  // $scope.activePage = 'markup/reports/barchart.html';

  $scope.setActivePage = function(content, sectionPath) {
    sectionPath = sectionPath || '';
    $scope.activePage = 'markup/' + sectionPath + content + '.html';
  };

});
