angular.module('app')
.controller('duplicatesCtrl', ['$scope', 'Service', function ($scope, Service) {

    Service('MonitorDuplicatesDetails').then(function (res) {
        $scope.duplicateProcessing = JSON.parse(res);
    });

}]);