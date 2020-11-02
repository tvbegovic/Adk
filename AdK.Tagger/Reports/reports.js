angular.module('app.reports', [])
	.controller('reportViewerCtrl', ['$scope', '$route', '$location', 'CurrentReport', 'AppSettings', function($scope, $route, $location, CurrentReport, AppSettings) {
		var reports = [
			{ id: 'TopChart', heading: 'Top Chart', content: 'TopChart/topChart' },
			{ id: 'SalesLeads', heading: 'Sales Leads', content: 'SalesLeads/salesLeads' },
			{ id: 'Brendvertiser', heading: 'Brand / Advertiser', content: 'Brandvertiser/brandvertiser' },
			{ id: 'MarketOverview', heading: 'Market Overview', content: 'MarketOverview/marketOverview' },
			{ id: 'KeyAccountActivity', heading: 'Key Account Activity', content: 'KeyAccountActivity/keyAccountActivity' },
			{ id: 'AdvertisingActivityTrend', heading: 'Advertising Activity Trend', content: 'AdvertisingActivityTrend/AdvertisingActivityTrend' },
			{ id: 'AuditLog', heading: 'Audit Log', content: 'AuditLog/quickAudit' },
			{ id: 'AsRunLog', heading: 'As-Run Log', content: 'AsRunLog/asRunLog' },
			{ id: 'ShareOfAdvertisingActivity', heading: 'Share of Advertising Activity', content: 'ShareOfAdvertisingActivity/shareOfAdvertisingActivity' },
			{ id: 'ShareOfBusiness', heading: 'Share of Business', content: 'ShareOfBusiness/shareOfBusiness' },
			{ id: 'RankedAdvertisers', heading: 'Ranked Advertisers', content: 'RankedAdvertisers/rankedAdvertisers' },
			{ id: 'ShareByBrandOrAdvertiser', heading: 'Share by Brand / Advertiser', content: 'ShareByBrandOrAdvertiser/shareByBrandOrAdvertiser' },
			{ id: 'AdvertisingSummary', heading: 'Advertising Summary', content: 'AdvertisingSummary/advertisingSummary' },
			{ id: 'AdBlocks', heading: 'Ad Blocks', content: 'AdBlocks/adBlocks' },
			{ id: 'ActivityByDaypart', heading: 'Activity by Daypart', content: 'ActivityByDaypart/activityByDaypart' },
			{ id: 'InvestmentTrend', heading: 'Investment Trend', content: 'InvestmentTrend/investmentTrend' },
			{ id: 'Market12MonthTrend', heading: 'Market 12 Month Trend', content: 'Market12MonthTrend/market12MonthTrend' },
			{ id: 'BrandActivityByWeekday', heading: 'Brand Activity by Weekday', content: 'BrandActivityByWeekday/brandActivityByWeekday' },
			{ id: 'BrandActivityByMediaHouse', heading: 'Brand Activity by Media House', content: 'BrandActivityByMediaHouse/brandActivityByMediaHouse' },
			{ id: 'AdvertisingLogsByBrand', heading: 'Advertising Logs By Brand', content: 'AdvertisingLogsByBrand/advertisingLogsByBrand' },
			{ id: 'Clutter', heading: 'Clutter', content: 'Clutter/clutter' },
			{ id: 'CompetitorProximity', heading: 'Competitor Proximity', content: 'CompetitorProximity/competitorProximity' }

		];
		var reportsToDisplay = [];

		//moduleReportsDefinition name is same as claim name
		var moduleReportsDefinition = {
			'advertiser-reports': ['InvestmentTrend', 'AsRunLog', 'AuditLog', 'ShareByBrandOrAdvertiser', 'BrandActivityByWeekday', 'BrandActivityByMediaHouse', 'Clutter', 'AdvertisingLogsByBrand'],
			'agency-reports': ['MarketOverview', 'InvestmentTrend', 'AsRunLog', 'AuditLog', 'ShareByBrandOrAdvertiser', 'Market12MonthTrend'],
			'mediahouse-reports': ['TopChart', 'SalesLeads', 'MarketOverview', 'AsRunLog', 'AuditLog', 'KeyAccountActivity', 'AdvertisingActivityTrend', 'ShareOfAdvertisingActivity', 'ShareOfBusiness',
				'RankedAdvertisers', 'AdvertisingSummary', 'ActivityByDaypart', 'AdBlocks', 'CompetitorProximity']
		};
		$scope.reportModule = $route.current.params.reportModule;
		var showAllReports = $scope.reportModule === 'all-reports';
		var moduleReports = moduleReportsDefinition[$scope.reportModule];

		//Check if user have rights to see this module
		if (!$scope.user.isAdmin && !$scope.user.granted[$scope.reportModule]) {
			return;
		}

		if (showAllReports) {
			reportsToDisplay = reports;
		} else if (moduleReports) {
			//Order reports in direction as they are defined in moduleReportsDefinition
			moduleReports.forEach(function(moduleReport) {
				var report = _.find(reports, function(report) { return report.id === moduleReport; });
				if (report) { reportsToDisplay.push(report); }
			});
		} else {
			//unrecognized module break execution (don't display anything')
			return;
		}

		AppSettings.getReportsToHide().then(function(reportsToHide) {
			if (reportsToHide && reportsToHide.length) {
				reportsToDisplay = reportsToDisplay.filter(function(report) {
					return !reportsToHide.some(function(reportToHide) {
						return reportToHide === report.id;
					});
				});
			}

			if (!reportsToDisplay.length) { return; }

			$scope.tabs = reportsToDisplay;

			if ($route.current.params.report) {
				var selectedReport = _.find($scope.tabs, function(report) {
					return report.id === $route.current.params.report;
				});

				if (selectedReport) {
					setTabContent(selectedReport);
				}
			}

			if (!$scope.tabContentUrl) {
				setTabContent($scope.tabs[0]);
			}

		});

		function setTabContent(report) {
			report.active = true;
			$scope.tabContentUrl = 'reports/' + report.content + '.html';
		}

		$scope.current = CurrentReport.Filter;
		$scope.message = { show: false };

		$scope.goToReport = function(reportId) {
			$location.path('report-viewer/' + $scope.reportModule + '/' + reportId);
		};

		$scope.isReportActive = function(reportId) {
			return $route.current.params.report && $route.current.params.report === reportId;
		};

		$scope.haveId = function(obj) {
			return obj && obj.Id;
		};

		$scope.resetCategories = function(callback) {
			$scope.current.categories = [];
			if (callback) { callback(); }
		};

		$scope.onChannelsLoad = function(channels) {
			$scope.channels = channels;
			$scope.channelsLoaded = true;
			$scope.haveChannels = channels && channels.length > 0;
			$scope.$broadcast('channels-loaded');
		};

		$scope.showMessage = function(template) {
			$scope.message.template = template;
			$scope.message.show = true;
		};

		$scope.hideMessage = function() {
			$scope.message.show = false;
		};

	}])
	.service('CurrentReport', [function() {
		return {
			Filter: {
				channel: {},
				include: {},
				period: {},
				sortByPreviousPeriod: false,
				value: {},
				industry: {},
				market: {},
				lessthan: false,
				spent: {},
				dateRange: {},
				displayType: {},
				show: {},
				dayPart: {},
				dayOfWeek: {},
				media: {},
				brandOrAdvertiser: {},
				groupBy: {},
				channelOrDate: {},
				adBreakDuration: {},
				limit: {},
				categories: [],
				periodBy: {},
				showOthers: true,
				dateFrom: null,
				dateTo: null,
				timeFrom: null,
				timeTo: null,
				periodInfo: {
					PeriodKind: null,
					Name: null,
					DateFrom: null,
					DateTo: null
				},
				customDate: null
			}
		};
	}]);

