angular.module('app')
	.factory('navigationService', ['Service', function (Service) {

		var factory = {};
		factory.navigationItems = [];

		factory.getNavigationItems = function () {
			return Service('GetSetting', { module: 'General', key: 'hierarchicalNavigation' }).then(function (data) {
				if (data != null && data.toLowerCase() == 'true') {
					factory.navigationItems = [
						{ id: 'dashboard', path: 'dashboard', label: 'Home', icon: 'home', alwaysDisplay: true },						
						{
							id: 'pricedesigners', path: '', label: 'Pricing', icon: 'usd',
							children: [
								{ id: 'price-designer', path: 'price-designer', label: 'Price Designer', icon: 'usd' },
								{ id: 'price-designer1.1', path: 'price-designer1.1', label: 'Price Designer 1.1', icon: 'usd' },
								{ id: 'price-designer2', path: 'price-designer2', label: 'Price Designer 2.0', icon: 'usd' }
							]
						},						
						{
							id: 'alerts', path: '', label: 'Ad alerts', icon: 'blackboard',
							children: [
								{ id: 'client-feeds', path: 'client-feeds/view', label: 'Ad Alert Manager', icon: 'blackboard' },
								{ id: 'clients', path: 'clients', label: 'Client Manager', icon: 'user' }
							]

						},
						{
							id: 'add-feed', path: 'add-feed', label: 'My Ad Alerts', icon: 'blackboard'
						},
						
						{
							id: 'security', path: '', label: 'Security', icon: 'lock',
							children: [
								{ id: 'account', path: 'account', label: 'My Account', icon: 'user', alwaysDisplay: true },
								{ id: 'rights', path: 'rights', label: 'Rights', icon: 'lock' },
								{ id: 'settings', path: 'settings', label: 'Settings', icon: 'cog' }
							]
						},
						{ id: 'webplayer', path: 'webplayer', label: 'Web player', icon: 'music' }

					];
				} else {
					//Old flat navigation
					factory.navigationItems = [
						{ id: 'dashboard', path: 'dashboard', label: 'Home', icon: 'home', alwaysDisplay: true },
						{ id: 'spot-upload', path: 'spot-upload', label: 'Spot upload', icon: 'upload' },
						{ id: 'spot-library', path: 'spot-library', label: 'Spot library', icon: 'book' },
						{ id: 'tagger', path: 'tagger', label: 'Ad Tagger', icon: 'tags' },
						{ id: 'transcript-training', path: 'transcript-training', label: 'Transcript Training', icon: 'education' },
						{ id: 'transcript', path: 'transcript', label: 'Ad Transcript', icon: 'headphones' },
						{ id: 'transcript-review', path: 'transcript-review', label: 'Transcript Review', icon: 'search' },
						{ id: 'transcript-stats', path: 'transcript-stats', label: 'Transcript Stats', icon: 'stats' },
						{ id: 'transcript-manager', path: 'transcript-manager', label: 'Transcript Manager', icon: 'volume-up' },
						{ id: 'tag-manager', path: 'tag-manager', label: 'Tag Manager', icon: 'random' },
						{ id: 'tagger-lab', path: 'tagger-lab', label: 'Tagger Lab', icon: 'alert' },
						{ id: 'word-cut', path: 'word-cut', label: 'Word Cut', icon: 'scissors' },
						{ id: 'matcher', path: 'matcher', label: 'Matcher', icon: 'object-align-vertical' },
						{ id: 'playout-map', path: 'playout-map', label: 'Playout Map', icon: 'map-marker' },
						{ id: 'mailing', path: 'mailing', label: 'Mailing', icon: 'envelope' },
						{ id: 'price-designer', path: 'price-designer', label: 'Price Designer', icon: 'usd' },
						{ id: 'price-designer1.1', path: 'price-designer1.1', label: 'Price Designer 1.1', icon: 'usd' },
						{ id: 'price-designer2', path: 'price-designer2', label: 'Price Designer 2', icon: 'usd' },
						{ id: 'media-house', path: 'media-house', label: 'Media House', icon: 'film' },
						{ id: 'markets', path: 'markets', label: 'Markets', icon: 'th' },
						{ id: 'channels', path: 'channels', label: 'Channels', icon: 'th' },
						{ id: 'client-feeds', path: 'client-feeds/view', label: 'Ad Alert Manager', icon: 'blackboard' },
						{
							id: 'add-feed', path: 'add-feed', label: 'My Ad Alerts', icon: 'blackboard'
						},
						{ id: 'advertiser-reports', path: 'report-viewer/advertiser-reports', label: 'Reports: Advertiser', icon: 'blackboard' },
						{ id: 'agency-reports', path: 'report-viewer/agency-reports', label: 'Reports: Agency', icon: 'blackboard' },
						{ id: 'mediahouse-reports', path: 'report-viewer/mediahouse-reports', label: 'Reports: Media', icon: 'blackboard' },
						{ id: 'reporting', path: 'reporting', label: 'Reporting', icon: 'blackboard' },
						{ id: 'account', path: 'account', label: 'My Account', icon: 'user', alwaysDisplay: true },
						{ id: 'rights', path: 'rights', label: 'Rights', icon: 'lock' },
						{ id: 'settings', path: 'settings', label: 'Settings', icon: 'cog' },
						{ id: 'clients', path: 'clients', label: 'Client Manager', icon: 'user' },
						{ id: 'user-countries', path: 'transcript-countries-admin', label: 'Transcript countries', icon: 'user' },
						{ id: 'webplayer', path: 'webplayer', label: 'Web player', icon: 'music' }

					];
				}
			});
		}

		return factory;
	}]);

angular.module('app')
	.controller('NavigationCtrl', ['$scope', '$rootScope', '$location', '$timeout', 'navigationService',
		function ($scope, $rootScope, $location, $timeout, navigationService) {
			//!IMPORTANT: id need to be same as module name defined in GetAllClaims service. path need to be same as path defined in  $routeProvider

			navigationService.getNavigationItems().then(function () {
				$scope.items = navigationService.navigationItems;
				$scope.items = _.filter($scope.items, function (item) {
					return item.alwaysDisplay || $scope.user.isAdmin || $scope.user.granted[item.id]
						|| (item.children && item.children.find(c => $scope.user.granted[c.id]));
				});
			})

			$rootScope.menuExpanded = true;
			
			$scope.toggleMenu = function () {
				$scope.menuClosed = !$scope.menuClosed;
				$rootScope.menuExpanded = !$scope.menuClosed;
			};

			$scope.activateMenu = function (path) {
				$location.path(path);

				//On navigation show loading overlay for min 750ms
				$rootScope.loading += 1;
				$timeout(function () {
					$rootScope.loading -= 1;
				}, 750);
			};

			$scope.isMenuActive = function (menuItemPath) {
				var pagePath = $location.path();
				if (pagePath.indexOf('report-viewer/') !== -1) {
					return pagePath.indexOf('/' + menuItemPath) !== -1;
				}
				//return pagePath === ('/' + menuItemPath);
				return pagePath.substring(1, menuItemPath.length+1) == menuItemPath;
			};

			$scope.applyFixedClass = function () {
				return $rootScope.headerScrolledOut && $scope.pageIsTallerThenNavigation;
			};

			angular.element(window).bind('resize', observeResizeEvent);
			function observeResizeEvent() {
				var sideNavigation = document.getElementById('side-nav-list');
				var navigationHeight = sideNavigation ? sideNavigation.clientHeight : 0;
				$scope.pageIsTallerThenNavigation = window.innerHeight > navigationHeight;
			}
			$timeout(observeResizeEvent, 500);

			$scope.toggleSubMenu = function (item) {
				item.expanded = !item.expanded;				
			}

		}])
	.directive('navigation', function () {
		return {
			restrict: 'E',
			replace: true,
			templateUrl: '/Modules/Navigation/navigation.html',
			controller: 'NavigationCtrl'
		};
	});
