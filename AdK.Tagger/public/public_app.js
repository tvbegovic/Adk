angular.module('app', ['ngRoute', 'ui.bootstrap', 'ngSanitize','vjs.video','ngCookies'])
	.config(['$routeProvider', '$locationProvider', function ($routeProvider, $locationProvider) {

		$locationProvider.hashPrefix('');

        $routeProvider
            .when('/playback/:songId/:autoPlay?', {
                templateUrl: '/Modules/Feeds/playback.html',
                controller: 'feedPlaybackCtrl',
                params: {
                    songId: null
                }
            })
            .when('/check-new-matches', {
                templateUrl: '/Modules/Feeds/feedCheckNewMatches.html',
				controller: 'feedCheckNewMatchesCtrl'
            })
            .when('/add-feed/:reportId?/:name?/:returnTo?', {
                templateUrl: '/Modules/Feeds/addFeed.html',
                controller: 'addFeedCtrl',
                params: {
                    reportId: null
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
			.when('/matchanalyzer/:key/:channelId?/:from?/:to?/:visual?',
				{
					templateUrl: '/Modules/MatchAnalyzer/MatchAnalyzer.html',
					controller: 'matchAnalyzerCtrl'
				})
            .otherwise({
                redirectTo: '/'
            });

    }])
.config(['$sceDelegateProvider',function ($sceDelegateProvider) {
    $sceDelegateProvider.resourceUrlWhitelist([
        // Allow same origin resource loads.
        'self',
        // Allow loading from our assets domain.  Notice the difference between * and **.
        'http://accessa.streamsink.com/**']);
}]);
