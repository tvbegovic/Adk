angular.module('app')
	.factory('SpotLibraryService', ['Service', function (Service) {

		return {
			getDefaultFilter: function () {
				return {
					name: '',
					brand: '',
					category: '',
					advertiser: '',
					sort: {
						column: 'created',
						ascending: false
					}
				};
			},

			//Song Status Enum on server
			songStatus: {
				new: 0,
				processed: 1,
				uploaded: 2,
				mailed: 3
			},

			loadSpots: function (pageSize, pageNum, filter) {
				return Service('GetMySamples', {
					pageSize: pageSize,
					pageNum: pageNum,
					sortColumn: filter.sort.column,
					ascending: filter.sort.ascending,
					filter: filter
				}, { backgroundLoad: true });
			}
		};

	}]);
