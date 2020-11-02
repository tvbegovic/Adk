angular.module('app')
	.controller('filterGroupsCtrl', ['$scope', 'Service', '$routeParams',
        function ($scope, Service, $routeParams) {

            $scope.editFilterGroupIsVisible = false;

            $scope.createOrUpdateGroup = function (group, exclude) {
                $scope.editGroup(group, exclude);
            }

            $scope.getDisplay = function (array) {
                return array.map(function (item) { return item.DisplayName || '' }).join(', ');
            }

        }]).directive('filterGroups', [function () {
            return {
                restrict: 'E',
                scope: {
                    feed: '=',
                    editGroup: '=',
                    deleteGroup: '='
                },
                templateUrl: '/Modules/Feeds/filterGroups.html',
                controller: 'filterGroupsCtrl'
            };
        }]);
