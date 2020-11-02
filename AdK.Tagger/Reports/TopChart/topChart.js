angular.module('app.reports')
	.controller('topChartCtrl', ['$scope', 'Service', 'Pager', 'ValueFormatter', function($scope, Service, Pager, ValueFormatter) {
		var initPageLoad = _.once(load);
		$scope.loading = true;
		$scope.pager = new Pager();
		$scope.load = load;

		$scope.onDirectivesInit = function() {
			if ($scope.haveId($scope.current.channel) && $scope.haveId($scope.current.include) && $scope.haveId($scope.current.value)
				&& $scope.current.periodInfo.PeriodKind) {
				initPageLoad();
			}
		};

		var sortColumns = {
			advertiser: 'AdvertiserName',
			currentRank: 'CurrentRank',
			previousRank: 'PreviousRank',
			currentTotal: 'CurrentTotal',
			previousTotal: 'PreviousTotal',
			change: 'ChangeInRank'
		};

		$scope.sort = {
			ascending: true,
			current: sortColumns.currentRank,
			channelIndex: null, //In case of sorting by channel
			column: sortColumns
		};

		$scope.sortBy = function(val, channelIndex) {
			if ($scope.sort.current === val) {
				$scope.sort.ascending = !$scope.sort.ascending;
			} else {
				$scope.sort.ascending = true;
				$scope.sort.current = val;
			}

			$scope.sort.channelIndex = channelIndex;

			sortTopChartRows();
		};

		function load() {
			$scope.loading = true;
			$scope.hideMessage();
			delete $scope.topChart;
			Service('MediaHouseTopChart', {
				channelId: $scope.current.channel.Id,
				include: $scope.current.include.Id,
				period: $scope.current.periodInfo,
				value: $scope.current.value.Id
			}).then(function(topChart) {

				$scope.topChart = topChart;

				if (topChart && topChart.Rows.length) {
					//calculateChange
					_.each(topChart.Rows, function(row) {
						if (row.PreviousRank && row.CurrentRank) {
							row.ChangeInRank = row.PreviousRank - row.CurrentRank;
						}
					});

					sortTopChartRows();
					$scope.pager.reset();
					$scope.pager.setItemCount(topChart ? topChart.Rows.length : 0);
				} else {
					$scope.showMessage('NoData');
				}

			}).catch(function() {
				$scope.showMessage('Error');
			}).finally(function() {
				$scope.loading = false;
			});
		}


		function sortTopChartRows() {

			if ($scope.topChart && $scope.topChart.Rows) {
				$scope.topChart.Rows.sort(function(a, b) {
					var colA = a[$scope.sort.current];
					var colB = b[$scope.sort.current];

					//sort by channels
					if ($scope.sort.channelIndex || $scope.sort.channelIndex === 0) {
						colA = a.ChannelValues[$scope.sort.channelIndex].Total;
						colB = b.ChannelValues[$scope.sort.channelIndex].Total;
					}

					return ValueFormatter.columnValueToSortIndicator(colA, colB, $scope.sort.ascending);

				});
			}
		}

		$scope.getValue = function(val) {
			if (!val) {
				return '';
			}

			if ($scope.current.value.Id === 'Duration') {
				return ValueFormatter.convertSecondsToHourFormat(val);
			} else {
				return ValueFormatter.toLocalString(val, true);
			}

		};

		$scope.filterAdvertisers = function(row) {
			if ($scope.selectedAdvertiser && $scope.selectedAdvertiser.Name) {
				return (angular.lowercase(row.AdvertiserName).indexOf(angular.lowercase($scope.selectedAdvertiser.Name)) !== -1);
			}
			return true;
		};

		$scope.advertiserFilterChange = function(selectedAdvertiser) {
			$scope.selectedAdvertiser = selectedAdvertiser;
		};

		$scope.$on('channels-loaded', $scope.onDirectivesInit);

	}]);
