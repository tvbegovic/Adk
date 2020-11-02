angular.module('app.reports')
	.controller('salesLeadsCtrl', ['$scope', 'Service', 'Pager', 'ValueFormatter',
		function($scope, Service, Pager, ValueFormatter) {
			var initPageLoad = _.once(load);
			$scope.loading = true;

			$scope.pager = new Pager();
			$scope.load = load;
			function load() {
				$scope.loading = true;
				$scope.hideMessage();
				delete $scope.salesLeads;

				var request = {
					channelId: $scope.current.channel.Id,
					include: $scope.current.include.Id,
					period: $scope.current.periodInfo,
					lessthan: $scope.current.lessthan,
					spent: $scope.current.spent.Id,
					industryId: $scope.current.industry.Id
				};

				Service('MediaHouseSalesLeads', request).then(function(salesLeads) {
					$scope.salesLeads = salesLeads;

					if (salesLeads && salesLeads.Rows.length) {
						$scope.pager.reset();
						$scope.pager.setItemCount(salesLeads.Rows.length);
						sortSalesLeadsRows();
					} else {
						$scope.showMessage('NoData');
					}
				}).catch(function() {
					$scope.showMessage('Error');
				}).finally(function() {
					$scope.loading = false;
				});
			}

			var sortColumns = {
				totalSpent: 'CurrentTotal',
				channelLastDate: 'LastDateTimeStamp',
				channelSpent: 'Total'
			};

			$scope.sort = {
				ascending: false,
				current: sortColumns.totalSpent,
				channelIndex: null, //In case of sorting by channel
				column: sortColumns
			};

			$scope.sortBy = function(val, channelIndex) {
				if ($scope.sort.current === val && (!channelIndex || channelIndex === $scope.sort.channelIndex)) {
					$scope.sort.ascending = !$scope.sort.ascending;
				} else {
					$scope.sort.ascending = true;
				}

				$scope.sort.current = val;
				$scope.sort.channelIndex = channelIndex;

				sortSalesLeadsRows();

			};

			function sortSalesLeadsRows() {
				if ($scope.salesLeads && $scope.salesLeads.Rows.length) {
					$scope.salesLeads.Rows.sort(function(a, b) {
						var currentSortColumn = $scope.sort.current;
						var colA = a[currentSortColumn];
						var colB = b[currentSortColumn];

						if (currentSortColumn === sortColumns.channelLastDate || currentSortColumn === sortColumns.channelSpent) {
							colA = a.ChannelValues[$scope.sort.channelIndex][currentSortColumn];
							colB = b.ChannelValues[$scope.sort.channelIndex][currentSortColumn];
						}
						return ValueFormatter.columnValueToSortIndicator(colA, colB, $scope.sort.ascending);

					});
				}
			}

			$scope.onDirectivesInit = function() {
				if ($scope.haveId($scope.current.channel) && $scope.haveId($scope.current.include)
					&& $scope.haveId($scope.current.spent) && $scope.current.periodInfo.PeriodKind
                    && $scope.haveId($scope.current.industry)) {
					initPageLoad();
				}
			};
			$scope.$on('channels-loaded', $scope.onDirectivesInit);

			$scope.getValue = function(val) {
				if (!val) { return ''; }
				return ValueFormatter.toLocalString(val, true);
			};

		}]);
