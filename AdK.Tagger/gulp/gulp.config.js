function getName() {
	return 'tagger-bundle-' + Date.now();
}

function getLoginName() {
	return 'login-bundle-' + Date.now();
}

function getPublicName() {
    return 'public-bundle-' + Date.now();
}

function getVendorName() {
	return 'vendor-bundle-' + Date.now();
}

module.exports = {
	src: './src/',
	dest: './dist/',
	destJs: './dist/js/',
	destCss: './dist/css/',
	get scriptNameFull() { return getName() + '.js'; },
	get scriptNameMin() { return getName() + '.min.js'; },
	get loginScriptNameFull() { return getLoginName() + '.js'; },
    get loginScriptNameMin() { return getLoginName() + '.min.js'; },
    get publicScriptNameFull() { return getPublicName() + '.js'; },
    get publicScriptNameMin() { return getPublicName() + '.min.js'; },
	get vendorScriptNameFull() { return getVendorName() + '.js'; },
	get vendorScriptNameMin() { return getVendorName() + '.min.js'; },
	get vendorScriptBaseNameFull() { return 'base-vendor.js'; },
    get vendorScriptBaseNameMin() { return 'base-vendor.min.js'; },
    get vendorScriptPublicNameFull() { return 'public-vendor.js'; },
    get vendorScriptPublicNameMin() { return 'public-vendor.min.js'; },
	get cssNameFull() { return getName() + '.css'; },
	get cssNameMin() { return getName() + '.min.css'; },
	vendorBase: [
		'js/Vendor/angular/angular.js',
		'js/Vendor/ui-bootstrap-tpls-0.13.4.js',
		'js/Vendor/angular/angular-route.min.js',
        'js/Vendor/angular/angular-sanitize.min.js',
        'js/Vendor/angular/angular-cookies.min.js',
		'js/Vendor/textAngular/textAngularSetup.js',
		'js/Vendor/textAngular/textAngular.js',
		'js/Vendor/textAngular/textAngular-sanitize.js',
		'js/Vendor/textAngular/textAngular-rangy.min.js',
		'js/Vendor/angular/angular-vs-repeat.min.js',
		'js/Vendor/ng-file-upload/ng-file-upload-shim.min.js',
		'js/Vendor/ng-file-upload/ng-file-upload.min.js',
		'js/Vendor/lodash.js',
		'js/Vendor/soundmanager2/soundmanager2-jsmin.js',
		'js/Vendor/moment-with-locales.min.js',
		'js/Vendor/ng-tags-input.js',
		'js/Vendor/datePickerDecorator.js'
	],
	vendorScripts: [
		'js/Vendor/d3.min.js',
		'js/Vendor/nv.d3.js',
		'js/Vendor/angular-nvd3.js',
		'js/Vendor/ZeroClipboard.min.js',
		'js/Vendor/leaflet/leaflet.js',
        'js/Vendor/leaflet/angular-leaflet-directive.js',
		'js/Vendor/vjs-video.min.js'
    ],
    publicVendorScripts: [
        'js/Vendor/angular/angular.js',
        'js/Vendor/angular/angular-cookies.min.js',
        'js/Vendor/ui-bootstrap-tpls-0.13.4.min.js',
        'js/Vendor/angular/angular-route.min.js',
        'js/Vendor/angular/angular-sanitize.min.js',
        'js/Vendor/soundmanager2/soundmanager2-jsmin.js',
        'js/Vendor/moment-with-locales.min.js',
        'js/Vendor/lodash.js',
        'js/Vendor/vjs-video.min.js'
    ],
    publicScriptSrc: [
        'public/public_app.js',
        'js/Services/soundmanager.js',
        'js/_Directives/playerWidget.js',
        'js/filters.js',
        'js/Services/*.js',
        'js/_Directives/*.js',
        'js/common.js',
        'Modules/**/*.js',
        '!Modules/Components/**/*.js'
    ],
	scriptSrc: [
		'js/app.js',
		'js/filters.js',
		'js/tagger.js',
		'js/fsGrid.js',
		'js/Services/*.js',
        'js/_Directives/*.js',
		'js/common.js',
		'Modules/**/*.js',
		'Reports/**/*.js',
		'!Modules/Security/loginForm.js',
        '!**/**.ignore.js',
        '!Modules/Components/**/*.js'
    ],
    componentsSrc : [
        'Modules/Components/**/*.js'
    ],
    componentsDest: 'app.bundle.js',
	loginSrc: [
		'js/login.js',
		'js/Services/service.js',
		'js/Services/localStorage.js',
		'Modules/Security/**.js',
		'!Modules/Security/auth.js'

	],
	// styleGuideResources: [
	// 	'StyleGuide/**/*',
	// 	'js/vendor/**/**.js',
	// 	'js/common.js',
	// 	'js/Services/appSettings.js',
	// 	'js/Services/d3ChartLabels.js',
	// 	'js/Services/ValueFormatter.js',
	// 	'js/Services/baseChartConfig.js',
	// 	'css/bootstrap.min.css',
	// 	'css/nv.d3.min.css',
	// 	'css/report.css',
	// 	'css/styleguide.css',
	// 	'css/font-awesome.min.css',
    //     'css/dpoc.css'
	// ],
	indexHtml: 'index.html',
    loginHtml: 'login.html',
	publicHtml: 'public.html',
	dokazniceIndexHtml: 'Dokaznice/index.html',
	dokazniceLoginHtml: 'Dokaznice/login.html',
	htmlSrc: [
		'js/_Directives/**/*.html',
		'view/**/*.html',
		'Reports/**/*.html',
        'Modules/**/*.html'
    ],

	sharedCss: [
		'css/bootstrap.min.css',
		'css/styleguide.css',
		'css/tagger.css',
		'css/font-awesome.min.css',
		'css/ng-tags-input.css',
		'css/report.css',
		'css/audit.css',
		'css/textAngular.css',
		'css/dpoc.css',
		'css/colorpicker.min.css',
		'js/Vendor/video/video-js.css'
	],
	get cssSrc() {
		return this.sharedCss.concat([
			'css/nv.d3.min.css',
			'js/vendor/leaflet/leaflet.css',
			'css/markets.css'
		]);
	},
	resources: [
		'img/**/*',
		'fonts/**/*',
		'Service.asmx',
		'SpotUpload.ashx',
		'NLog.config'
	],
	get dokazniceStyles() {
		return this.sharedCss.concat(['Dokaznice/css/dokaznice.css']);
	},
	dokazniceScripts: [
		'Dokaznice/js/app.js',
		'js/filters.js',
		'js/common.js',
		'js/fsGrid.js',
		'js/Services/*.js',
		'js/_Directives/playerWidget.js',
		'js/_Directives/alertMessage.js',
		'js/_Directives/ajaxInlineIndicator.js',
		'Reports/_Directives/reportDateRangeFilter.js',
		'Reports/_Directives/reportMessage.js',
		'Reports/_Directives/channelThresholdSlideIn.js',
		'Reports/AuditLog/**.js',
		'Modules/Account/accountCtrl.js',
		'Modules/Channels/channelsCtrl.js',
		'Modules/SpotLibrary/spotLibraryCtrl.js',
		'Modules/SpotLibrary/spotListCtrl.js',
		'Modules/SpotUpload/spotUploadCtrl.js',
		'Modules/EmailComposer/emailComposer.js',
		'Modules/Security/security.js',
		'Modules/Settings/**.js',
		'Modules/Rights/rightsCtrl.js',
		'Modules/Navigation/navigation.js'
	]
};
