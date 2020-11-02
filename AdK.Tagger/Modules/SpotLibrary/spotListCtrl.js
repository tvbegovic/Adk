angular.module('app')
	.controller('spotListCtrl', ['$scope', '$document', 'Service', 'Pager', 'confirmPopup', 'focus', function ($scope, $document, Service, Pager, confirmPopup, focus) {
	    $scope.pager = new Pager();

	    $scope.current = { spot: null, playing: null };
	    $scope.isPaused = true;

	    ///Player
	    $scope.playPauseSong = function (spot, rowIndex) {
	        $scope.player.playPauseSong(spot.FileUrl, spot.Duration, rowIndex);
	    };

	    $scope.setFocus = function (inputName) {
	        focus(inputName);
	    };

	    $scope.getSpots = function (keepPagerIndex) {
	        if (!keepPagerIndex) {
	            $scope.pager.reset();
	        }

	        $scope.loadSpots({
	            pageSize: $scope.pager.size,
	            pageNum: $scope.pager.index - 1
	        }).then(function (res) {
	            if (res && res.spots) {
	                res.spots.forEach(function (spot) {
	                    spot.Created = spot.Created ? moment(spot.Created) : null;
	                });
	                $scope.spots = res.spots;
	                $scope.pager.setItemCount(res.totalCount);
	            }
	        });
	    };

	    $scope.setSort = function (column) {
	        if ($scope.filter.sort.column == column) {
	            $scope.filter.sort.ascending = !$scope.filter.sort.ascending;
	        } else {
	            $scope.filter.sort.column = column;
	            $scope.filter.sort.ascending = true;
	        }
	        $scope.getSpots();
	    };

	    $scope.getItems = function (setName, search) {
	        if (search.length < 2) { return []; }
	        return Service('SuggestMySampleDescription', { setName: setName, search: search }, { backgroundLoad: true });
	    };

	    $scope.$watchGroup(['pager.index', 'pager.size'], function () { $scope.getSpots(true); });

	    if ($scope.instanceName) {
	        $scope.$on('SpotList.Reload.' + $scope.instanceName, $scope.getSpots);
	    }

	    $scope.selectSpot = function (spot, rowIndex) {
	        if ($scope.player && $scope.player.playingInfo.uiIndentifier !== rowIndex) {
	            $scope.player.reset();
	        }

	        spot._originalName = spot.Name;
	        $scope.current.spot = spot.Deleted ? null : spot;
	        $scope.position = 0;
	    };

	    $scope.isEdited = function (spot) {
	        return $scope.canEdit && spot === $scope.current.spot;
	    };

	    $scope.saveChanges = function (spot) {

	        //Don't allow empty names.
	        if (!spot.Name || !spot.Name.trim()) {
	            //no need to update as we update one field at the time
	            spot.Name = spot._originalName;
	            return;
	        }

	        if ($scope.current.spot) {
	            Service('UpdateMySample', {
	                sampleId: spot.Guid,
	                name: spot.Name,
	                brand: spot.Brand,
	                category: spot.Category,
	                advertiser: spot.Advertiser
	            }, { backgroundLoad: true }).then(function () {
	                spot._originalName = spot.Name;
	            });
	        }
	    };

	    $scope.deleteSpot = function (spot) {
	        if (!spot.Deleted) {
	            confirmPopup.open("Delete a spot", null, "You are about to delete spot '" + (spot.Name || spot.Filename) + "'. Continue?").then(function () {
	                spot.Deleted = true;
	                $scope.selectSpot(spot);
	                Service('DeleteMySample', { sampleId: spot.Guid });
	            });
	        }
	    };

	    $scope.pickSpot = function (spot) {
	        $scope.onPick({ spot: spot });
	    };

	    $scope.$on('refreshSpotList', function () {
	        $scope.pager.index = 1;
	        $scope.getSpots();
	    });

	}]).directive('spotList', [function () {
	    return {
	        restrict: 'E',
	        scope: {
	            filter: '=',
	            loadSpots: '&',
	            canEdit: '=',
	            canDelete: '=',
	            canPick: '=',
	            onPick: '&',
	            instanceName: '@',
	            showScannedColumn: '@'
	        },
	        templateUrl: '/Modules/SpotLibrary/spotList.html',
	        controller: 'spotListCtrl'
	    };
	}]);
