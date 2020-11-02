angular.module('app')
.controller('rightsCtrl', ['$scope', 'Service', function ($scope, Service) {
    Service('GetRights').then(function (rights) {
        $scope.claims = rights.Claims;
        _.forEach(rights.Users, function (user) {
            user.granted = user.granted.reduce(function (map, g) {
                map[g] = true;
                return map;
            }, {});
        });
        $scope.users = rights.Users;
    });
    $scope.toggleRight = function (user, claim) {
        var granted = !user.granted[claim.Value];
        user.granted[claim.Value] = granted;
        Service('SetRight', { userId: user.id, claim: claim.Value, granted: granted });
    };
}]);
