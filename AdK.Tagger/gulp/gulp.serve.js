var { series, parallel, src, dest} = require('gulp');
var inject = require('gulp-inject');
var browserSync = require('browser-sync').create();
var config = require('./gulp.config');
//var babel = require('gulp-babel');

function serveHtml(html, filesToServe, destination) {
  destination = destination || '.'
  //inject full unchanged files in html
  return src(html)
    .pipe(inject(filesToServe))
    .pipe(dest(destination));
}

function serveTaggerIndex() {
    const sources = src(config.vendorBase.concat(config.vendorScripts, config.scriptSrc, config.cssSrc), { read: false });
  return serveHtml(config.indexHtml, sources);
}

function serveTaggerLogin() {
  var sources = src(config.vendorBase.concat(config.loginSrc, config.cssSrc), { read: false });
  return serveHtml(config.loginHtml, sources);
}

function serveTaggerPublic () {
    var sources = src(config.publicVendorScripts.concat(config.publicScriptSrc, config.cssSrc), { read: false });
    return serveHtml(config.publicHtml, sources);
}

function serveDokazniceIndex() {
  var sources = src(config.vendorBase.concat(config.dokazniceScripts, config.dokazniceStyles), { read: false });
  return serveHtml(config.dokazniceIndexHtml, sources, 'Dokaznice/');
}

function serveDokazniceLogin() {
  var sources = src(config.vendorBase.concat(config.loginSrc, config.dokazniceStyles), { read: false });
  return serveHtml(config.dokazniceLoginHtml, sources, 'Dokaznice/');
}

function browserSync() {
  browserSync.init({
    proxy: 'http://localhost:52494/'
  });
}

exports.default = parallel(serveDokazniceIndex, serveDokazniceLogin, serveTaggerIndex, serveTaggerLogin, serveTaggerPublic);


