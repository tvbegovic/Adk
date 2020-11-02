const { series, parallel, src, dest} = require('gulp');
const terser = require('gulp-terser');
const concat = require('gulp-concat');
var inject = require('gulp-inject');

function scriptMinifier(sourceFiles, destination, name) {
  return src(sourceFiles)
    // .pipe(sourcemaps.init())
	.pipe(concat(name))
	.pipe(terser())
    .pipe(dest(destination));
}

function buildHtml(html, sources, destination) {
  //create index-debug and inject full files in it
  return src(html)
    .pipe(inject(sources, {
      addRootSlash: false,
      //ignorePath : 'src/main/webapp',
      transform: function(filePath) {
        if(destination.indexOf('Dokaznice') !== -1) {
          var newPath = filePath.replace('Dokaznice/dist/', '');
        } else {
          var newPath = filePath.replace('dist/', '');
        }

        if (filePath.indexOf('.js') !== -1) {
          return '<script src="' + newPath + '"></script>';
        } else {
          return '<link rel="stylesheet" href="' + newPath + '">';
        }
      }
    }))
    .pipe(dest(destination));
}


exports.scriptMinifier = scriptMinifier;
exports.buildHtml = buildHtml;
