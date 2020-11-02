angular.module('app')
    .controller('feedCheckNewMatchesCtrl', ['$scope', 'Service', '$routeParams',
        function ($scope, Service, $routeParams) {

            Service('GetFeedsMatchCount', {cutOffDate: null}).then(function (res) {
                //res -array of objects feedFilterId, lastReportCount, currentCount
                res.forEach(function (r) {
                    r.cutOffDate = r.cutOffDate ? moment(r.cutOffDate).toDate() : null;
                });
                $scope.results = res;
            });


        }]);
