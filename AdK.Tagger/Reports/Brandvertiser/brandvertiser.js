angular.module('app.reports')
	.controller('brandvertiserCtrl', ['$scope', 'Service', 'MyChannels', 'CurrentReport', 'BaseChartConfig', 'ValueFormatter',
		function($scope, Service, MyChannels, CurrentReport, BaseChartConfig, ValueFormatter) {
			var initPageLoadOnce = _.once(initPageLoad);
			$scope.loading = true;

			var discreteBarChartOptions = _.merge(BaseChartConfig.getDiscreteBarChartOptions({ excludeSize: true }), {
				chart: {
					x: function(d) { return d.Media; },
					y: function(d) { return d.Total; },
					height: 300
				}
			});

			var pieChartOptions = _.merge(BaseChartConfig.getPieChartOptions({ excludeSize: true }), {
				chart: {
					x: function(d) { return d.Media; },
					y: function(d) { return d.Total; },
					labelSunbeamLayout: false,
					height: 200,
					noData: '--',
					margin: {
						top: 0,
						right: 5,
						bottom: 10,
						left: 5
					}
				}
			});

			setChartOptions();

			$scope.brandvertiser = {
				Id: null,
				Name: null,
				IsBrand: true
			};

			$scope.load = function() {
				$scope.loading = true;
				$scope.brandvertiser.MediaMix = null;
				Service('MediaHouseBrandvertiser', {
					id: $scope.brandvertiser.Id,
					isBrand: $scope.brandvertiser.IsBrand,
					channelId: $scope.current.channel.Id,
					include: $scope.current.include.Id,
					period: $scope.current.periodInfo,
					value: $scope.current.value.Id
				}).then(function(brandvertiser) {
					$scope.brandvertiser = brandvertiser;
					setChartData();
					$scope.maxShift = 0;
					brandvertiser.Shifts.Percent.forEach(function(shift) {
						$scope.maxShift = Math.max($scope.maxShift, Math.abs(shift));
					});

				}).finally(function() {
					$scope.loading = false;
				});
			};
			$scope.loadAdsInRotation = function() {
				$scope.adsInRotation = ['Loading...'];
				Service('MediaHouseRotatingAds', {
					id: $scope.brandvertiser.Id,
					isBrand: $scope.brandvertiser.IsBrand,
					channelId: $scope.current.channel.Id,
					include: $scope.current.include.Id,
					period: $scope.current.periodInfo,
					value: $scope.current.value.Id
				}, { backgroundLoad: true }).then(function(adsInRotation) {
					$scope.adsInRotation = adsInRotation;
				});
			};

			$scope.onDirectivesInit = function() {
				if ($scope.haveId($scope.current.channel) && $scope.haveId($scope.current.include) && $scope.haveId($scope.current.value)) {
					initPageLoadOnce();
				}
			};

			function initPageLoad() {
				if ($scope.channels.length && $scope.brandvertiser.Id) {
					$scope.load();
					$scope.loadAdsInRotation();
				}
			}

			$scope.$on('channels-loaded', $scope.onDirectivesInit);


			$scope.changePieChartDisplay = function() {
        setChartOptions();
        setChartData();
      };

			function setChartData() {
				var mediaMix = $scope.brandvertiser && $scope.brandvertiser.MediaMix ? $scope.brandvertiser.MediaMix : {
					CurrentQuarter: {}, PreviousQuarter: {}, YearToDate: {}, LastYear: {}
				};
        if ($scope.current.showPieCharts) {
					$scope.currentQuarter = mediaMix.CurrentQuarter;
					$scope.previousQuarter = mediaMix.PreviousQuarter;
					$scope.yearToDate = mediaMix.YearToDate;
					$scope.lastYear = mediaMix.LastYear;
        } else {
					$scope.currentQuarter = {
						ByMedia: ValueFormatter.convertPieChartDataToDiscreteBarData(mediaMix.CurrentQuarter.ByMedia)
					};
					$scope.previousQuarter = {
						ByMedia: ValueFormatter.convertPieChartDataToDiscreteBarData(mediaMix.PreviousQuarter.ByMedia)
					};
					$scope.yearToDate = {
						ByMedia: ValueFormatter.convertPieChartDataToDiscreteBarData(mediaMix.YearToDate.ByMedia)
					};
					$scope.lastYear = {
						ByMedia: ValueFormatter.convertPieChartDataToDiscreteBarData(mediaMix.LastYear.ByMedia)
					};
        }

      }

      function setChartOptions() {
        $scope.chartOptions = $scope.current.showPieCharts ? pieChartOptions : discreteBarChartOptions;
      }

		}]);
