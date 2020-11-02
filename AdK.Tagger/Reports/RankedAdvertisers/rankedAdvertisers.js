
angular.module('app.reports')
    .controller('rankedAdvertisersCtrl', ['$scope', 'Service', 'Pager', 'MediaHouses', 'MyChannels', 'ValueFormatter',
        function($scope, Service, Pager, MediaHouses, MyChannels, ValueFormatter) {
            var initPageLoad = _.once(load);
            $scope.loading = true;
            $scope.pager = new Pager();
            $scope.load = load;

            $scope.onDirectivesInit = function() {
                if ($scope.haveId($scope.current.value) && $scope.haveId($scope.current.market)
                    && $scope.current.periodInfo.PeriodKind) {
                    initPageLoad();
                }
            };

            function load() {
                $scope.loading = true;
                $scope.hideMessage();
                delete $scope.rankedAdvertisers;
                Service('MediaHouseRankedAdvertisers', {
                    period: $scope.current.periodInfo,
                    value: $scope.current.value.Id,
                    marketId: $scope.current.market.Id
                }).then(function(rankedAdvertisers) {
                    $scope.rankedAdvertisers = rankedAdvertisers;
                    $scope.pager.reset();
                    $scope.pager.setItemCount(rankedAdvertisers.RankedAdvertiserRows ? rankedAdvertisers.RankedAdvertiserRows.length : 0);
                    if (!rankedAdvertisers.RankedAdvertiserRows || !rankedAdvertisers.RankedAdvertiserRows.length) {
                        $scope.showMessage('NoData');
                    }
                }).catch(function() {
                    $scope.showMessage('Error');
                }).finally(function() {
                    $scope.loading = false;
                });
            }

            $scope.getPercent = function(row) {
                var percent = row.GrandTotal == 0 ? 0 : arraySum(row.ChannelValuesGroup) * 100 / row.GrandTotal;
                return ValueFormatter.toPercentageString(percent);
            };

            var sortColumns = {
                Rank: 'CurrentRank',
                Percentage: 'Percentage',
                GroupProperties: 'GroupProperties',
                SubtotalMediaHouse: 'SMH',
                Competitors: 'Competitors',
                SubtotalCompetitors: 'SC',
                GrandTotal: 'GrandTotal'
            };

            $scope.sort = {
                current: sortColumns.GrandTotal,
                column: sortColumns,
                ascending: false,
                SortingIndex: null
            };

            //Sorting comparator
            $scope.comparator = function(row) {
                var val = row.GrandTotal;
                switch ($scope.sort.current) {
                    case $scope.sort.column.Rank:
                        val = row.CurrentRank;
                        break;
                    case $scope.sort.column.Percentage:
                        var percent = row.GrandTotal == 0 ? 0 : arraySum(row.ChannelValuesGroup) * 100 / row.GrandTotal;
                        val = percent;
                        break;
                    case $scope.sort.column.GroupProperties:
                        val = row.ChannelValuesGroup[$scope.sort.SortingIndex].Value;
                        break;
                    case $scope.sort.column.SubtotalMediaHouse:
                        val = arraySum(row.ChannelValuesGroup);
                        break;
                    case $scope.sort.column.Competitors:
                        val = row.ChannelValuesCompetitors[$scope.sort.SortingIndex].Value;
                        break;
                    case $scope.sort.column.SubtotalCompetitors:
                        val = arraySum(row.ChannelValuesCompetitors);
                        break;
                }

                //anchor empty values to the bottom of the list
                if (!val) {
                    return Number.MAX_VALUE;
                }

                return $scope.sort.ascending ? val : -val;
            };

            $scope.sortBy = function(val, index) {
                var sameIndex = true;
                if (val == $scope.sort.column.GroupProperties || val == $scope.sort.column.Competitors) {
                    sameIndex = $scope.sort.SortingIndex === index;
                    $scope.sort.SortingIndex = index;

                }

                if (sameIndex && $scope.sort.current === val) {
                    $scope.sort.ascending = !$scope.sort.ascending;
                } else {
                    $scope.sort.ascending = true;
                }

                $scope.sort.current = val;
            };

            $scope.getValue = function(val) {
                if (!val) { return ''; }
                switch ($scope.current.value.Id) {
                    case 'Duration':
                        return ValueFormatter.convertSecondsToHourFormat(val);
                    case 'Spend':
                        return ValueFormatter.toLocalString(ValueFormatter.roundServerNumberString(val), true);
                    default:
                        return ValueFormatter.toLocalString(val, true);
                }
            };

            $scope.getTotal = function(arr) {
                var sum = arraySum(arr);
                return $scope.getValue(sum);
            };

            MediaHouses.ready.then(function() {
                $scope.mediaHouse = _.find(MediaHouses.holdings, function(house) { return house.Mine; });
            });

            MyChannels.getChannels().then(function(channels) {
                $scope.showNoMediaHouseMsg = !(channels && channels.length);
            });

            function arraySum(arr) {
                var total = 0;
                for (var i in arr) {
                    var value = arr[i].Value || 0;
                    total += value;
                }
                return total;
            }

            $scope.filterAdvertisers = function(row) {
                if ($scope.selectedAdvertiser) {
                    return (angular.lowercase(row.AdvertiserName).indexOf(angular.lowercase($scope.selectedAdvertiser)) !== -1);
                }

                return true;
            };

            $scope.advertiserFilterChange = function(selectedAdvertiser) {
                $scope.selectedAdvertiser = selectedAdvertiser.Name;
            };

        }]);
