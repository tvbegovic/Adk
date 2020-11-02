angular.module('app')
	.controller('spotLibraryCtrl', ['Service', 'SpotLibraryService', function (Service, SpotLibraryService) {
		var vm = this;
		vm.filter = SpotLibraryService.getDefaultFilter();
		vm.filter.songStatuses = [SpotLibraryService.songStatus.uploaded];

		vm.canEdit = true;
		vm.canDelete = true;

		vm.loadSpots = function (pageSize, pageNum) {
			return SpotLibraryService.loadSpots(pageSize, pageNum, vm.filter);
		};

	}]);
