var { series, parallel, src, dest} = require('gulp');
var concat = require('gulp-concat');
var copy = require('gulp-copy');
var cleanCSS = require('gulp-clean-css');
var clean = require('gulp-clean');
var config = require('./gulp.config');
var common = require('./gulp.common');

var dokazniceConfig = {
  dest: './Dokaznice/dist/',
  get jsDest() {
    return this.dest + 'js/';
  },
  get cssDest() {
    return this.dest + 'css/';
  },
  get scriptNameMin() {
    return 'dokaznice-bundle-' + Date.now() + '.min.js';
  },
  get loginScriptNameMin() {
    return 'dokaznice-login-bundle-' + Date.now() + '.min.js';
  },
  get cssNameMin() {
    return 'dokaznice-bundle-' + Date.now() + '.min.css';
  }
}

/******************************
 * GULP BUILD FOR PRODUCTION
 ******************************/

function cleanDestFolder() {
  return src(dokazniceConfig.dest, { read: false, allowEmpty: true })
    .pipe(clean());
}

function minifyScripts() {
  return common.scriptMinifier(config.dokazniceScripts, dokazniceConfig.jsDest, dokazniceConfig.scriptNameMin);
}

function minifyLoginScripts() {
  return common.scriptMinifier(config.loginSrc, dokazniceConfig.jsDest, dokazniceConfig.loginScriptNameMin);
}

function minifyVendorBaseScripts() {
  return common.scriptMinifier(config.vendorBase, dokazniceConfig.jsDest, config.vendorScriptBaseNameMin);
}

function minifyCss() {
  return src(config.dokazniceStyles)
    .pipe(concat(dokazniceConfig.cssNameMin))
    .pipe(cleanCSS())
    .pipe(dest(dokazniceConfig.cssDest));
}

function copyHtml() {
  return src(config.htmlSrc)
    .pipe(copy(dokazniceConfig.dest));
}

function copyHtmlNew() {
  return src('Dokaznice/view/**.html')
    .pipe(copy(dokazniceConfig.dest, { prefix: 1 }));
}

function copyResources() {
  return src(config.resources)
    .pipe(copy(dokazniceConfig.dest));
}

function buildLogin() {
  // It's not necessary to read the files (will speed up things), we're only after their paths:
  var sources = src([
    dokazniceConfig.jsDest + config.vendorScriptBaseNameMin,
    dokazniceConfig.jsDest + 'dokaznice-login-bundle*.js',
    dokazniceConfig.cssDest + 'dokaznice-bundle*.css'
  ], { read: false });

  return common.buildHtml(config.dokazniceLoginHtml, sources, dokazniceConfig.dest);

}

function buildIndex() {
  // It's not necessary to read the files (will speed up things), we're only after their paths:
  var sources = src([
    dokazniceConfig.jsDest + config.vendorScriptBaseNameMin,
    dokazniceConfig.jsDest + 'dokaznice-bundle*.js',
    dokazniceConfig.cssDest + 'dokaznice-bundle*.css'
  ], { read: false });

  return common.buildHtml(config.dokazniceIndexHtml, sources, dokazniceConfig.dest);
}

exports.default = series(cleanDestFolder,
	parallel(minifyVendorBaseScripts, minifyCss, minifyLoginScripts, minifyScripts, copyResources, copyHtml, copyHtmlNew),
	parallel(buildLogin, buildIndex));
