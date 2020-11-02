angular.module('app.reports')
  .controller('clutterReportCtrl', ['$scope', 'Service', 'CurrentReport', 'ValueFormatter',
    function($scope, Service, CurrentReport, ValueFormatter) {
      var initPageLoad = _.once(load);
      $scope.loading = true;

      $scope.onDirectivesInit = function() {
        if ($scope.haveId($scope.current.channel) && $scope.haveId($scope.current.dayPart)
          && $scope.haveId($scope.current.adBreakDuration) && $scope.current.customDate) {
          initPageLoad();
        }
      };

      $scope.$on('channels-loaded', $scope.onDirectivesInit);

      var sortColumns = {
        averageBreak: 'AverageBreak',
        percentageAboveAdBreak: 'PercentageAboveAdBreak'
      };

      $scope.sort = {
        ascending: false,
        current: sortColumns.averageBreak,
        dayPartIndex: 0,
        column: sortColumns
      };

      $scope.sortBy = function(sortColumn, dayPartIndex) {
        if (sortColumn === $scope.sort.current && dayPartIndex === $scope.sort.dayPartIndex) {
          $scope.sort.ascending = !$scope.sort.ascending;
        } else {
          $scope.sort.ascending = true;
        }

        $scope.sort.current = sortColumn;
        $scope.sort.dayPartIndex = dayPartIndex;

        sortClutterRows();
      };

      $scope.load = load;
      function load() {
        $scope.hideMessage();
        $scope.loading = true;

        var request = {
          channelId: $scope.current.channel.Id,
          dayPart: $scope.current.dayPart.Id,
          adBreakDurationInSeconds: $scope.current.adBreakDuration.Id,
          date: ValueFormatter.getServerStringDateWithoutTime($scope.current.customDate)
        };

        Service('MediaHouseClutter', request)
          .then(function(response) {
            //Set default sorting to Day Average
            $scope.sort.dayPartIndex = response.DayParts.length - 1;
            $scope.clutter = response;
            if (response.ChannelRows.length) {
              sortClutterRows();
            } else {
              $scope.showMessage('NoData');
            }

          }).catch(function() {
            $scope.clutter = null;
            $scope.showMessage('Error');
          }).finally(function() {
            $scope.loading = false;
          });
      }

      $scope.haveData = function() {
        return $scope.clutter && $scope.clutter.ChannelRows && $scope.clutter.ChannelRows.length;
      };

      function sortClutterRows() {
        if ($scope.clutter && $scope.clutter.ChannelRows) {
          $scope.clutter.ChannelRows.sort(function(a, b) {
            var colA = a.DayPartValues[$scope.sort.dayPartIndex][$scope.sort.current];
            var colB = b.DayPartValues[$scope.sort.dayPartIndex][$scope.sort.current];

            return ValueFormatter.columnValueToSortIndicator(colA, colB, $scope.sort.ascending);

          });
        }
      }

    }]);
