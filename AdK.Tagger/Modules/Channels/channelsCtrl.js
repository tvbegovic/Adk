angular.module('app')
    .controller('channelsCtrl', ['$scope', '$filter', 'focus', 'Pager', 'Service', function ($scope, $filter, focus, Pager, Service) {

        $scope.pager = new Pager();

        $scope.ChannelsWithCoverage = [];
        $scope.FilterSuggestions = { 'Country': [], 'City': [] };
        $scope.channels = [];
        $scope.channelsFiltered = [];

        $scope.filter = { sort: {} };

        var initPageLoad = _.once(init);

        function init() {

            loadChannelsWithCoverage();

        }

        /*****Channels******/
        function loadChannelsWithCoverage() {
            Service('GetChannelsWithCoverage').then(function (ChannelsWithCoverage) {

                $scope.ChannelsWithCoverage = ChannelsWithCoverage;

                $scope.FilterSuggestions['Country'] = _.uniq(_.map(ChannelsWithCoverage, 'Country'));

                $scope.FilterSuggestions['City'] = _.uniq(_.map(ChannelsWithCoverage, 'City'));

                $scope.getChannelsWithDuration();
            });
        }


        /*************INIT*************/
        initPageLoad();


        $scope.setFocus = function (inputName) {
            focus(inputName);
        };

        $scope.setSort = function (column) {
            if ($scope.filter.sort.column == column) {
                $scope.filter.sort.ascending = !$scope.filter.sort.ascending;
            } else {
                $scope.filter.sort.column = column;
                $scope.filter.sort.ascending = true;
            }
            $scope.getChannelsWithDuration();
        };

        $scope.getChannelsWithDuration = function (keepPagerIndex) {
            if (!keepPagerIndex) {
                $scope.pager.reset();
            }

            var pageSize = $scope.pager.size;
            var pageNum = $scope.pager.index - 1;

            var sliceStart = pageNum * pageSize;
            var sliceStop = sliceStart + pageSize;

            $scope.ChannelsWithCoverage.sort(function (a, b) {
                if ($scope.filter.sort.ascending)
                    if (a[$scope.filter.sort.column] > b[$scope.filter.sort.column])
                        return 1;
                    else if (a[$scope.filter.sort.column] < b[$scope.filter.sort.column])
                        return -1
                    else return 0;
                else
                    if (a[$scope.filter.sort.column] < b[$scope.filter.sort.column])
                        return 1;
                    else if (a[$scope.filter.sort.column] > b[$scope.filter.sort.column])
                        return -1
                    else return 0;
            });

            $scope.channelsFiltered = $scope.ChannelsWithCoverage;

            if ($scope.filter.Country)
                $scope.channelsFiltered = $scope.channelsFiltered.filter(function (c) {
                    return c.Country.includes($scope.filter.Country);
                });

            if ($scope.filter.City)
                $scope.channelsFiltered = $scope.channelsFiltered.filter(function (c) {
                    return c.City.includes($scope.filter.City);
                });

            $scope.channels = $scope.channelsFiltered.slice(sliceStart, sliceStop);

            $scope.pager.setItemCount($scope.channelsFiltered.length);
        };

        $scope.$watchGroup(['pager.index', 'pager.size'], function () { $scope.getChannelsWithDuration(true); });

        $scope.getItems = function (setName, search) {
            var searchLowerCase = search.toLowerCase();
            return $scope.FilterSuggestions[setName].filter(function (fs) {
                return fs.toLowerCase().includes(searchLowerCase);
            });
        };
    }]);