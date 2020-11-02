angular.module('app')
    .controller('userFeedReportsCtrl', ['$scope', 'Service', 'Pager', '$routeParams', '$location', 'cookieService','LocalStorage',
        function ($scope, Service, Pager, $routeParams, $location, cookieService,LocalStorage) {

            $scope.clientName = decodeURIComponent($routeParams.client);
            $scope.countryMarket = $routeParams.cm;

            $scope.openUserReport = function (report, client) {
                $location.path('add-feed/' + report.reportId + '/' + encodeURIComponent(client) + '/reports');
            };

            var id = $routeParams.id;
            if (id == null || $routeParams.client == null)
            {
                var cached = JSON.parse(cookieService.get('userFeedReportsLastParams'));
                $scope.clientName = cached.clientName;
                id = cached.id;
                $scope.countryMarket = cached.countryMarket;
            }

            function init() {
                cookieService.put('userFeedReportsLastParams', JSON.stringify({ clientName: $scope.clientName, id: id, countryMarket: $scope.countryMarket }));
                Service('GetFeedReports', {feedFilterId: id}).then(function (res) {
                    res.forEach(function (item) {
						item.TimeInserted = item.TimeInserted ? moment(item.TimeInserted).toDate() : null;                        
                    });
                    $scope.reportList = res;                    
                });
                
            }

            $scope.backButton = function () {
                $location.path('add-feed');
            };

            $scope.pager = new Pager();

            $scope.rowComparator = function (row) {
                switch ($scope.sort.Current) {
                    case $scope.sort.DateSent:
						val = row.TimeInserted;
                        break;
                    case $scope.sort.ItemCount:
                        val = row.ItemCount;
                        break;
                    
                    default:
                        val = row.DateSent;
                        break;
                }
                return val; 
            };

            $scope.sortBy = function (sorting) {
                if ($scope.sort.Current == sorting) {
                    $scope.sort.reverse = !$scope.sort.reverse;
                    $scope.sort.Current = sorting;
                } else {
                    $scope.sort.Current = sorting;
                }
            };

            $scope.sort = {
                DateSent: 0,
                ItemCount: 1,                
                Current: 0,
                reverse: true
            }

            //Init
            init();


        }]);
