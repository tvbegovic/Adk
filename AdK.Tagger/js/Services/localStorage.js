angular.module('app')
  .factory('LocalStorage', ['$window', '$q', function($window, $q) {
    function buildKey(prefix, data) {
      var key = prefix;
      if (data) {
        key = key + '-' + angular.toJson(data);
      }

      return key;
    }
    var _storage = {
      setJson: function(key, value) {
        $window.localStorage.setItem(key, angular.toJson(value));
      },
      getJson: function(key) {
        var value = $window.localStorage.getItem(key);
        if (value === 'undefined') {
          value = 'null';
        }

        return angular.fromJson(value);
      },
      cacheOrGenerate: function(key, data, fnGenerate) {
        var q = $q.defer();
        var value = _storage.getJson(buildKey(key, data));
        if (value != null) {
          q.resolve(value);
        } else {
          fnGenerate().then(function success(res) {
            _storage.setJson(buildKey(key, data), res);
            q.resolve(res);
          })
        }
        return q.promise;
      }
    };
    return _storage;

  }]);
