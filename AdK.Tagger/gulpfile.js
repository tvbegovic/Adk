
const serve = require('./gulp/gulp.serve');
const buildTagger = require('./gulp/gulp.build-tagger');
const buildDokaznice = require('./gulp/gulp.build-dokaznice');



exports.serve = serve.default;
exports.build = buildTagger.default;
exports.buildDokaznice = buildDokaznice.default;
exports.default = buildTagger.default;

