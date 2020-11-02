angular.module('app')
    .factory('cookieService', ['$cookies', function ($cookies) {
        var _cookies = {
            get: function (key) {
                if (angular.version.major >= 1 && angular.version.minor >= 4)
                    return $cookies.get(key);
                return $cookies[key]; 
            },
            put: function (key,value) {
                if (angular.version.major >= 1 && angular.version.minor >= 4)
                    return $cookies.put(key,value);
                return $cookies[key] = value; 
            }
        };

        return _cookies;
    }]);