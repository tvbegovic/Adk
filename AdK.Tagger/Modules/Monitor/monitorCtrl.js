angular.module('app')
.controller('monitorCtrl', ['$scope', 'Service', 'BaseChartConfig', function ($scope, Service, BaseChartConfig) {

    Service('MonitorRciLookupLimit').then(function (res) {
        $scope.rciLookupLimit = res;
    });
    Service('MonitorCaptureSummary').then(function (res) {
        $scope.captureSummary = JSON.parse(res);
    });
    Service('MonitorHashSummary').then(function (res) {
        $scope.hashSummary = JSON.parse(res);
    });
    Service('MonitorDuplicateSummaryFinished').then(function (res) {
        $scope.duplicatesFinished = JSON.parse(res).rows[0];
    });
    Service('MonitorDuplicateSummaryLate').then(function (res) {
        $scope.duplicatesLate = JSON.parse(res).rows[0];
    });
    Service('MonitorHarvestingSummary').then(function (res) {
        $scope.harvestingSummary = JSON.parse(res);
    });
    Service('MonitorPromotionSummary').then(function (res) {
        $scope.promotionSummary = JSON.parse(res);
    });

    $scope.chartCoverage = [];

    Service('MonitorWhatClient').then(function (res) {
        $scope.whatClient = res;
        if(res=="PK"){
            Service('MonitorCoverageForChannels', { channel_tag: 'ipsos' }).then(function (res) {
                $scope.coverageIpsos = JSON.parse(res);
                $scope.coverageIpsos.colTypes[1] = "Decimal";
                $scope.coverageIpsos.colTypes[2] = "Decimal";
                var lineChartValues = [];
                for (var i in $scope.coverageIpsos.rows) {
                    $scope.coverageIpsos.rows[i].h_clickpath = "/monitor/coverage/" + $scope.coverageIpsos.rows[i].h_week + "/ipsos";
                    lineChartValues.push({
                        x: $scope.coverageIpsos.rows.length - 1 - i,
                        y: -$scope.coverageIpsos.rows[i].Missing,
                        label: $scope.coverageIpsos.rows[$scope.coverageIpsos.rows.length - 1 - i].Week
                    });
                }
                $scope.chartCoverage.push({ values: lineChartValues, key: 'IPSOS' });
            });
        }
    });

    Service('MonitorCoverageForChannels', { channel_tag: '' }).then(function (res) {
        $scope.coverageAllChannels = JSON.parse(res);
        $scope.coverageAllChannels.colTypes[1] = "Decimal";
        $scope.coverageAllChannels.colTypes[2] = "Decimal";
        var lineChartValues = [];
        for (var i in $scope.coverageAllChannels.rows) {
            $scope.coverageAllChannels.rows[i].h_clickpath = "/monitor/coverage/" + $scope.coverageAllChannels.rows[i].h_week;
            lineChartValues.push({
                x: $scope.coverageAllChannels.rows.length - 1 - i,
                y: - $scope.coverageAllChannels.rows[i].Missing,
                label: $scope.coverageAllChannels.rows[$scope.coverageAllChannels.rows.length - 1 - i].Week
            });
        }
        $scope.chartCoverage.push({ values: lineChartValues, key: 'All' });
    });

    $scope.lineChartOptions = {
        chart: {
            type: 'lineChart',
            width: 450,
            height: 350,
            reduceXTicks: false,
            clipEdge: false,
            margin: { "bottom": 100 },
            xAxis: {
                tickFormat: function (d) {
                    return $scope.chartCoverage[0].values[d].label;
                },
                rotateLabels: -75,
                showMaxMin: false,
                ticks: 12,
            },
            yAxis: {
                axisLabel: 'Missing %'
            }
        }
    };

    Service('MonitorYesterdayGaps').then(function (res) {
        $scope.yesterdayGaps = JSON.parse(res);
        $scope.yesterdayGaps.colTypes[4] = "TimeSpan";

        for (var i in $scope.yesterdayGaps.rows) {
            $scope.yesterdayGaps.rows[i].h_clickpath = "/monitor/capture/" + $scope.yesterdayGaps.rows[i].h_channel_id;
        }
    });


}])
/********
 * Headings: column names with underscores removed 
 * Hidden columns: column names that begin with character 'h'
 * Row coloring: in controller add and populate attribute 'h_class', alowed values: 'danger', 'warning'
 * Alignment: Int16,32,64, DateTime, Decimal, TimeSpan are right-aligned
 * Link: row-click navigates to path, in controller add and populate attribute 'h_clickpath'
 ********/

.directive('genericTable', ['$location', function ($location) {
    return {
        restrict: 'E',
        scope: {
            tabledata: '=',
        },
        templateUrl: '/Modules/Monitor/genericTable.html',
        link: function ($scope, element, attrs) {
            $scope.align = function (colType) {
                var res = "text-left";
                switch (colType) {
                    case "Int16":
                    case "Int32":
                    case "Int64":
                    case "Decimal":
                    case "DateTime":
                    case "TimeSpan":
                        res = "text-right";
                    default:
                } 
                return res;
            }
            $scope.notSorted = function (obj) {
                if (!obj) {
                    return [];
                }
                return Object.keys(obj);
            }
            $scope.go = function (path) {
                if (path !== undefined) {
                    $location.path(path);
                }
            };

        }
    };
}]);