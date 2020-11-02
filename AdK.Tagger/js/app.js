angular.module('app', ['ngRoute', 'app.reports', 'ui.bootstrap', 'ngSanitize', 'textAngular', 'ngFileUpload', 'nvd3', 'vs-repeat',
	'leaflet-directive', 'ngTagsInput', 'vjs.video', 'ngCookies', 'colorpicker.module'])
	.config(['$routeProvider','$locationProvider', function ($routeProvider, $locationProvider) {

		$locationProvider.hashPrefix('');

		//IMPORTANT: path need to be same as to NavigationCtrl items path.
		$routeProvider
			.when('/dashboard', {
				templateUrl: '/view/dashboard.html',
				controller: 'dashboardCtrl'
			})
			.when('/spot-upload', {
				templateUrl: '/Modules/SpotUpload/spotUpload.html',
				controller: 'spotUploadCtrl'
			})
			.when('/spot-library', {
				templateUrl: '/Modules/SpotLibrary/spotLibrary.html',
				controller: 'spotLibraryCtrl',
				controllerAs: 'ctrl'
			})
			.when('/tagger', {
				templateUrl: '/view/tagger.html',
				controller: 'mainCtrl'
			})
			.when('/transcript-training', {
				templateUrl: '/view/transcript-training.html'
			})
			.when('/transcript', {
				templateUrl: '/view/transcript-ad.html'
			})
			.when('/transcript-review', {
				templateUrl: '/view/tagger.html',
				controller: 'mainCtrl'
			})
			.when('/transcript-stats', {
				templateUrl: '/view/transcript-stats.html',
				controller: 'transcriberStatisticsCtrl'
			})
			.when('/transcript-manager', {
				templateUrl: '/view/transcript-manager.html',
				controller: 'transcriptManagerCtrl'
			})
			.when('/tag-manager', {
				templateUrl: '/view/tag-manager.html',
				controller: 'tagManagerCtrl'
			})
			.when('/tagger-lab', {
				templateUrl: '/view/tagger-lab.html',
				controller: 'taggerLabCtrl'
			})
			.when('/word-cut', {
				templateUrl: '/view/word-cut.html',
				controller: 'wordCutCtrl'
			})
			.when('/matcher', {
				templateUrl: '/view/matcher.html',
				controller: 'matcherCtrl'
			})
			.when('/playout-map', {
				templateUrl: '/view/playout-map.html',
				controller: 'playoutMapCtrl'
			})
			.when('/mailing', {
				templateUrl: '/view/mailing.html',
				controller: 'mailingCtrl'
			})
			.when('/price-designer', {
				templateUrl: '/Modules/PriceDesigner/priceDesigner.html',
				controller: 'priceDesignerCtrl'
			})
			.when('/media-house', {
				templateUrl: '/Modules/MediaHouse/mediaHouse.html',
				controller: 'mediaHouseCtrl'
			})
			.when('/channels', {
				templateUrl: '/Modules/Channels/channels.html',
				controller: 'channelsCtrl'
			})
			.when('/report-viewer/:reportModule/:report?', {
				templateUrl: '/Reports/report-viewer.html',
				controller: 'reportViewerCtrl'
			})
			.when('/markets', {
				templateUrl: '/Modules/Markets/markets.html',
				controller: 'marketsCtrl'
			})
			.when('/reporting', {
				templateUrl: '/view/reporting.html',
				controller: 'reportingCtrl'
			})
			.when('/client-feeds/view/:id?/:name?/:returnTo?/:reportId?', {
				templateUrl: '/Modules/Feeds/clientFeeds.html',
				controller: 'clientFeedsCtrl',
				params: {
					id: null,
					name: null
				}
			})
			.when('/add-feed/:reportId?/:name?/:returnTo?', {
				templateUrl: '/Modules/Feeds/addFeed.html',
				controller: 'addFeedCtrl',
				params: {
					reportId: null
				}
			})
			.when('/client-feeds/new/:returnTo?', {
				templateUrl: '/Modules/Feeds/feedForm.html',
				controller: 'createFeedCtrl'
			})
			.when('/client-feeds/edit/:id/:returnTo?', {
				templateUrl: '/Modules/Feeds/feedForm.html',
				controller: 'editFeedCtrl',
				params: {
					id: null
				}
			})
			.when('/feed-playback/:songId/:reportId?/:feedFilterId?/:feedFilterName?/:returnTo?', {
				templateUrl: '/Modules/Feeds/playback.html',
				controller: 'feedPlaybackCtrl',
				params: {
					songId: null,
					reportId: null
				}
			})
			.when('/account', {
				templateUrl: '/Modules/Account/account.html',
				controller: 'accountCtrl'
			})
			.when('/rights', {
				templateUrl: '/Modules/Rights/rights.html',
				controller: 'rightsCtrl'
			})
			.when('/settings', {
				templateUrl: '/Modules/Settings/settings.html',
				controller: 'settingsCtrl'
			})

			.when('/price-designer1.1', {
				templateUrl: '/Modules/PriceDesigner1.1/priceDesigner11.html',
				controller: 'priceDesigner11Ctrl'
			})
			.when('/price-designer1.1/channel/:id/:tab?', {
				templateUrl: '/Modules/PriceDesigner1.1/priceDesigner11Channel.html',
				controller: 'priceDesigner11ChannelCtrl'
			})
			.when('/dpoc', {
				templateUrl: '/Modules/dpoc/dpoc.html',
				controller: 'dpocCtrl'
			})
			.when('/dpoc/notpromoted', {
				templateUrl: '/Modules/dpoc/dpocNotPromoted.html',
				controller: 'dpocNotPromotedCtrl'
			})
			.when('/dpoc/hashing', {
				templateUrl: '/Modules/dpoc/dpocHashing.html',
				controller: 'dpocHashingCtrl'
			})
			.when('/dpoc/duplicates', {
				templateUrl: '/Modules/dpoc/dpocDuplicates.html',
				controller: 'dpocDuplicatesCtrl'
			})
			.when('/dpoc/harvesting', {
				templateUrl: '/Modules/dpoc/dpocHarvesting.html',
				controller: 'dpocHarvestingCtrl'
			})
			.when('/dpoc/capture', {
				templateUrl: '/Modules/dpoc/dpocCapture.html',
				controller: 'dpocCaptureCtrl'
			})
			.when('/dpoc/capture', {
				templateUrl: '/Modules/dpoc/dpocCapture.html',
				controller: 'dpocCaptureCtrl'
			})
			.when('/dpoc/capture/:whichWeb/:channel', {
				templateUrl: '/Modules/dpoc/dpocCaptureChannel.html',
				controller: 'dpocCaptureChannelCtrl',
				params: {
					channel: null
				}
			})
			.when('/monitor', {
				templateUrl: '/Modules/Monitor/monitor.html',
				controller: 'monitorCtrl'
			})
			.when('/monitor/capture', {
				templateUrl: '/Modules/Monitor/capture.html',
				controller: 'captureCtrl'
			})
			.when('/monitor/capture/:channel', {
				templateUrl: '/Modules/Monitor/captureChannel.html',
				controller: 'captureChannelCtrl',
				params: {
					channel: null
				}
			})
			.when('/monitor/hashes', {
				templateUrl: '/Modules/Monitor/hashes.html',
				controller: 'hashesCtrl'
			})
			.when('/monitor/duplicates', {
				templateUrl: '/Modules/Monitor/duplicates.html',
				controller: 'duplicatesCtrl'
			})
			.when('/monitor/duplicates/report', {
				templateUrl: '/Modules/Monitor/duplicatesReport.html',
				controller: 'duplicatesReportCtrl'
			})
			.when('/monitor/harvesting', {
				templateUrl: '/Modules/Monitor/harvesting.html',
				controller: 'harvestingCtrl'
			})
			.when('/monitor/promotion', {
				templateUrl: '/Modules/Monitor/promotion.html',
				controller: 'promotionCtrl'
			})
			.when('/monitor/coverage/:week/:tag?', {
				templateUrl: '/Modules/Monitor/coverage.html',
				controller: 'coverageCtrl'
			})
			.when('/add-feed/reports/:id?/:client?/:cm?', {
				templateUrl: '/Modules/Feeds/userFeedReports.html',
				controller: 'userFeedReportsCtrl',
				params: {
					id: null,
					name: null
				}
			})
			.when('/clients', {
				templateUrl: '/Modules/Feeds/clientsContacts.html',
				controller: 'clientsContactsCtrl'
			})
			.when('/user-feed/:id/:name?/:returnTo?',
				{
					templateUrl: '/Modules/Feeds/userFeed.html',
					controller: 'userFeedCtrl'
				})
			.when('/transcript-countries-admin',
				{
					templateUrl: '/Modules/Transcript/countries.html',
					controller: 'transcriptCountriesCtrl'
				})
			.when('/webplayer',
				{
					templateUrl: '/Modules/WebPlayer/channelSelect.html',
					controller: 'webPlayerChannelSelectCtrl'
				})
			.when('/webplayer/channel/:id/:from?/:to?',
				{
					templateUrl: '/Modules/WebPlayer/webPlayer2.html',
					controller: 'webPlayerCtrl'
				})
			.when('/matchanalyzer/:channelId?/:from?/:to?/:visual?',
				{
					templateUrl: '/Modules/MatchAnalyzer/MatchAnalyzer.html',
					controller: 'matchAnalyzerCtrl'
				})
			.when('/price-designer2', {
				templateUrl: '/Modules/PriceDesigner1.1/priceDesigner11.html',
				controller: 'priceDesigner11Ctrl'
			})
			.when('/price-designer2/channel/:id/:tab?', {
				templateUrl: '/Modules/PriceDesigner 2/priceDesigner2Channel.html',
				controller: 'priceDesigner2ChannelCtrl'
			})
			.otherwise({
				redirectTo: '/dashboard'
			});

	}]);
