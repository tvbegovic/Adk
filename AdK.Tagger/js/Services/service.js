angular.module('app')
  .factory('Service', ['$http', '$rootScope','$q', function($http, $rootScope,$q) {
    $rootScope.loading = 0;
    return function(method, data, options) {
      options = options || {};
      if (!options.backgroundLoad) {
        $rootScope.loading += 1;
      }

      return $http.post('/service.asmx/' + method, data || {}).then(function(res) {
        return res.data.d;
      }, function (response) {
          console.log(response.statusText);
          return $q.reject(response);
      }).finally(function () {
        if (!options.backgroundLoad) {
          $rootScope.loading -= 1;
        }
      });
    };

  }]);
