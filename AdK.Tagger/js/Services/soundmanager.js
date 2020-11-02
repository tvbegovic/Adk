angular.module('app')
  .factory('soundmanager', ['$q', function($q) {
    var q = $q.defer();

    soundManager.debugMode = false;
    soundManager.setup({
      url: 'js/soundmanager2/swf/',
      onready: function() {
        q.resolve();
      },
      ontimeout: function() {
        q.notify('Failed to setup SoundManager2');
      },
      useHighPerformance: true,
      useFastPolling: true
    });

    return q.promise;
  }])
