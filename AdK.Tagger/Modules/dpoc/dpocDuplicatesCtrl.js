angular.module('app')
.controller('dpocDuplicatesCtrl', ['$scope', '$interval', 'Service', function ($scope, $interval, Service) {

    $scope.getDuplicateDelays = function () {
        Service('DpocDuplicateDelays').then(function (res) {
            $scope.duplicateDelays = res;
            var alternateColor = false;
            for (var i in $scope.duplicateDelays) {
                if (i>0 && $scope.duplicateDelays[i].matchedTime != $scope.duplicateDelays[i-1].matchedTime) {
                    alternateColor = !alternateColor;  
                }
                $scope.duplicateDelays[i].color = alternateColor;
            }
            console.log($scope.duplicateDelays);
        });
    };
    var reloadDuplicates = $interval(function () {
        $scope.getDuplicateDelays();
    }, 60000);

    $scope.$on('$destroy', function () {
        $interval.cancel(reloadDuplicates)
    });

    $scope.getDuplicateDelays();
}]);