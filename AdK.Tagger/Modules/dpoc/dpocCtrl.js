angular.module('app')
.controller('dpocCtrl', ['$scope', '$timeout', 'Service', function ($scope, $timeout, Service) {
    Service('DpocCaptureStatus', { whichWeb: "hr", delayMin: "5"}).then(function (res) {
        $scope.CaptureStatusHr = res;
    });
    Service('DpocCaptureStatus', { whichWeb: "hr", delayMin: "20" }).then(function (res) {
        $scope.CaptureStatusDelayHr = res;
    });
    Service('DpocHashingStatus', { whichWeb: "hr" }).then(function (res) {
        $scope.HashingStatusHr = res;
    });
    Service('DpocHashingStatus', { whichWeb: "mis" }).then(function (res) {
        $scope.HashingStatusMis = res;
    });
    Service('DpocDuplicateStatusLate').then(function (res) {
        $scope.DuplicateStatusLate = res;
    });
    Service('DpocDuplicateStatusFinished').then(function (res) {
        $scope.DuplicateStatusFinished = res;
    });
    Service('DpocHarvestingStatus', { whichWeb: "hr" }).then(function (res) {
        $scope.HarvestingStatusHr = res;
    });
    Service('DpocHarvestingStatus', { whichWeb: "mis" }).then(function (res) {
        $scope.HarvestingStatusMis = res;
    });
    Service('DpocPromotionQueue').then(function (res) {
        $scope.PromotionQueue = res;
    });
    Service('DpocCoverageWorst', {numdays: 30}).then(function (res) {
        $scope.CoverageWorst30 = res;
    });
    $scope.daysOptions = [1, 3, 7, 10, 15, 30];
    $scope.data = {}; //tabset makes its own scope and breaks data binding for primitives, so numDays has to be in an object
    $scope.data.numDays = 1;
    $scope.getCoverageWorst = function () {
        Service('DpocCoverageWorst', { numdays: $scope.data.numDays }).then(function (res) {
            $scope.CoverageWorst = res;
        });
    };
    $scope.getCoverageWorst();
    Service('DpocMissingTotal', { numdays: 90 }).then(function (res) {
        $scope.missingCoverage90 = res;
    });
    Service('DpocMissingTotal', { numdays: 30 }).then(function (res) {
        $scope.missingCoverage30 = res;
    });
    Service('DpocMissingTotal', { numdays: 7 }).then(function (res) {
        $scope.missingCoverage7 = res;
    });
    Service('DpocMissingTotal', { numdays: 1 }).then(function (res) {
        $scope.missingCoverage1 = res;
    });
    Service('DpocCountBadChannels', { numdays: 90 }).then(function (res) {
        $scope.badCoverage90 = res;
    });
    Service('DpocCountBadChannels', { numdays: 30 }).then(function (res) {
        $scope.badCoverage30 = res;
    });
    Service('DpocCountBadChannels', { numdays: 7 }).then(function (res) {
        $scope.badCoverage7 = res;
    });
    Service('DpocCountBadChannels', { numdays: 1 }).then(function (res) {
        $scope.badCoverage1 = res;
    });

}]);