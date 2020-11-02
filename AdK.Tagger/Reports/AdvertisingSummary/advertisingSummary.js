angular.module('app.reports')
    .controller('advertisingSummaryCtrl', [
        '$scope', 'Service', 'CurrentReport', 'Pager',
        function($scope, Service, CurrentReport, Pager) {
            $scope.selectedBrand = '';
            var initPageLoad = _.once(load);
            $scope.pager = new Pager();
            $scope.loading = true;
            $scope.load = load;

            function load() {
                $scope.hideMessage();
                $scope.loading = true;
                Service('MediaHouseAdvertisingSummary', {
                    channelId: $scope.current.channel.Id,
                    include: $scope.current.include.Id,
                    period: $scope.current.periodInfo,
                    groupBy: $scope.current.groupBy.Id
                }).then(function(data) {
                    $scope.serverData = {
                        PeriodEnd: data.PeriodEnd,
                        PeriodStart: data.PeriodStart
                    };

                    $scope.advertisingSummary = data.AdvertisingSummaryDetailsData;
                    $scope.pager.reset();
                    $scope.pager.setItemCount(data && data.AdvertisingSummaryDetailsData ? data.AdvertisingSummaryDetailsData.length : 0);

                    if (!$scope.advertisingSummary.length) {
                        $scope.showMessage('NoData');
                    }

                }).catch(function() {
                    $scope.showMessage('Error');
                }).finally(function() {
                    $scope.loading = false;
                });
            }
            $scope.onDirectivesInit = function() {
                if ($scope.haveId($scope.current.channel)
                    && $scope.haveId($scope.current.include)
                    && $scope.current.periodInfo.PeriodKind
                    && $scope.haveId($scope.current.groupBy)) {
                    initPageLoad();
                }
            };
            
            //Sorting
            $scope.sortBy = function(sorting) {
                if($scope.sort.Current == sorting) {
                    $scope.sort.Direction = $scope.sort.Direction == 'ASC' ? 'DESC' : 'ASC';
                    $scope.sort.Current = sorting;
                } else {
                    $scope.sort.Current = sorting;
                }
            };
            $scope.sort = {
                Direction: 'DESC',
                Count: 0,
                Duration: 1,
                Spend: 2,
                Current: 2
            }
            $scope.groupComparator = function(group) {
                switch ($scope.sort.Current) {
                    case $scope.sort.Count:
                        val = group.Total.Count;
                        break;
                    case $scope.sort.Spend:
                        val = group.Total.Spend;
                        break;
                    default:
                        val = group.Total.Duration;
                        break;
                }
                return $scope.sort.Direction == 'ASC' ? val : -val;
            };
            $scope.rowComparator = function(row) {
                switch ($scope.sort.Current) {
                    case $scope.sort.Count:
                        val = row.Count;
                        break;
                    case $scope.sort.Spend:
                        val = row.Spend;
                        break;
                    default:
                        val = row.Duration;
                        break;
                }
                return $scope.sort.Direction == 'ASC' ? val : -val;
            };
            $scope.$on('channels-loaded', $scope.onDirectivesInit);
        }
    ]);
