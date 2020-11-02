angular.module('app')
  .service('MyChannels', ['Service', '$q', function(Service, $q) {
    var _channelsCache = null;

    function getChannels() {
      var deferred = $q.defer();

      if (_channelsCache) {
        deferred.resolve(_channelsCache);
      } else {
        Service('GetMyChannels').then(function(channels) {
          _channelsCache = channels;
          deferred.resolve(_channelsCache);
        }).catch(function() {
          deferred.reject();
        });
      }

      return deferred.promise;
    } getChannels();

    return {
      getChannels: getChannels,
      refreshChannels: function() {
        _channelsCache = null;
      }
    };
  }]);
