angular.module('app.reports')
  .controller('competitorProximityReportCtrl', ['$scope', 'Service', 'CurrentReport', 'ValueFormatter',
    function ($scope, Service, CurrentReport, ValueFormatter) {
        var initPageLoad = _.once(load);
        $scope.loading = true;

        $scope.selectedBrand;
        //$scope.current.brandOrAdvertiser.Id = 'Brand';

        $scope.onDirectivesInit = function () {
            if ($scope.haveId($scope.current.channel) && $scope.current.customDate) {
                initPageLoad();
            }
        };

        $scope.$on('channels-loaded', $scope.onDirectivesInit);


        $scope.load = load;
        function load() {
            if (!$scope.selectedBrand)
                return;

            $scope.loading = true;

            $scope.hideMessage();

            var request = {
                channelId: $scope.current.channel.Id,
                brandId: $scope.selectedBrand.Id,
                date: ValueFormatter.getServerStringDateWithoutTime($scope.current.customDate)
            };

            Service('MediaHouseCompetitorProximity', request)
              .then(function (response) {
                  $scope.AdBlocks = response.AdBlocks;
                  if (!$scope.AdBlocks.length) {
                      $scope.showMessage('NoData');
                  }

              }).catch(function () {
                  $scope.AdBlocks = null;
                  $scope.showMessage('Error');
              }).finally(function () {
                  $scope.loading = false;
              });
        }

        $scope.brandFilterChange = function (selectedBrand) {
            $scope.selectedBrand = selectedBrand;
            load();
        };

        $scope.haveData = function () {
            return $scope.AdBlocks && $scope.AdBlocks.length;
        };

    }]);
