var { series, parallel, src, dest} = require('gulp');
var concat = require('gulp-concat');
var copy = require('gulp-copy');
var cleanCSS = require('gulp-clean-css');
var clean = require('gulp-clean');

var config = require('./gulp.config');
var common = require('./gulp.common');

/******************************
 * GULP BUILD FOR PRODUCTION
 ******************************/

function cleanDestFolder() {
  return src(config.dest, { read: false, allowEmpty: true })
    .pipe(clean());
}

function minifyScripts() {
  return common.scriptMinifier(config.scriptSrc, config.destJs, config.scriptNameMin);
}

function minifyLoginScripts() {
  return common.scriptMinifier(config.loginSrc, config.destJs, config.loginScriptNameMin);
}

function minifyPublicScripts() {
    return common.scriptMinifier(config.publicScriptSrc, config.destJs, config.publicScriptNameMin);
}

function minifyVendorBaseScripts() {
  return common.scriptMinifier(config.vendorBase, config.destJs, config.vendorScriptBaseNameMin);
}

function minifyVendorScripts() {
  return common.scriptMinifier(config.vendorScripts, config.destJs, config.vendorScriptNameMin);
}

function minifyPublicVendorScripts() {
    return common.scriptMinifier(config.publicVendorScripts, config.destJs, config.vendorScriptPublicNameMin);
}

function minifyCss() {
  return src(config.cssSrc)
    .pipe(concat(config.cssNameMin))
    .pipe(cleanCSS())
    .pipe(dest(config.destCss));
}

function copyHtml() {
  return src(config.htmlSrc)
    .pipe(copy(config.dest));
}



function copyResources() {
  return src(config.resources)
    .pipe(copy(config.dest));
}

function buildLogin() {
  // It's not necessary to read the files (will speed up things), we're only after their paths:
  var sources = src([
    config.destJs + config.vendorScriptBaseNameMin,
    config.destJs + 'login-bundle*.js',
    config.destCss + 'tagger-bundle*.css'
  ], { read: false });

  return common.buildHtml(config.loginHtml, sources, config.dest);

}

function buildIndex() {
  // It's not necessary to read the files (will speed up things), we're only after their paths:
  var sources = src([
    config.destJs + config.vendorScriptBaseNameMin,
    config.destJs + 'vendor*.js',
    config.destJs + 'tagger-bundle*.js',
    config.destCss + 'tagger-bundle*.css'
  ], { read: false });

  return common.buildHtml(config.indexHtml, sources, config.dest);
}

function buildPublic() {
    // It's not necessary to read the files (will speed up things), we're only after their paths:
    var sources = src([
        config.destJs + config.vendorScriptPublicNameMin,
        config.destJs + 'public-bundle*.js',
        config.destCss + 'tagger-bundle*.css'
    ], { read: false });

    return common.buildHtml(config.publicHtml, sources, config.dest);

}

exports.default = series( cleanDestFolder,
	parallel(minifyVendorBaseScripts, minifyVendorScripts, minifyPublicVendorScripts, minifyCss, minifyLoginScripts, minifyPublicScripts, minifyScripts,
		copyResources, copyHtml),
		parallel(buildIndex, buildLogin, buildPublic));
