angular.module('app', ['ngRoute', 'ui.bootstrap', 'ngSanitize', 'textAngular',	 'ngFileUpload', 'vs-repeat', 'ngTagsInput', 'app.reports'])
	.config(['$routeProvider', function ($routeProvider) {

		//IMPORTANT: path need to be same as to NavigationCtrl items path.
		$routeProvider
			.when('/audit-log', {
				templateUrl: '/Reports/AuditLog/auditLog.html',
				controller: 'auditLogCtrl'
			})
			.when('/quick-audit', {
				templateUrl: '/Reports/AuditLog/quickAudit.html',
				controller: 'quickAuditLogCtrl'
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
			.when('/channels', {
				templateUrl: '/Modules/Channels/channels.html',
				controller: 'channelsCtrl'
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
			.otherwise({
				redirectTo: '/audit-log'
			});

	}]).factory('navigationItems', function () {
		return [
			{ id: 'audit-log', path: 'audit-log', label: 'Audit Log', icon: 'folder-open' },
			{ id: 'quick-audit', path: 'quick-audit', label: 'Quick Audit', icon: 'list-alt' },
			{ id: 'spot-upload', path: 'spot-upload', label: 'Spot Upload', icon: 'upload' },
			{ id: 'spot-library', path: 'spot-library', label: 'Spot Library', icon: 'book' },
			{ id: 'channels', path: 'channels', label: 'Channels', icon: 'th' },
			{ id: 'account', path: 'account', label: 'My Account', icon: 'user', alwaysDisplay: true },
			{ id: 'rights', path: 'rights', label: 'Rights', icon: 'lock' },
			{ id: 'settings', path: 'settings', label: 'Settings', icon: 'cog' }
		];
	});
