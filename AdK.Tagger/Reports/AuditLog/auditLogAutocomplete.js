angular.module('app.reports')
	.controller('auditLogAutocompleteCtrl', ['$scope', 'Service', 'CachedApiCalls',
		function ($scope, Service, CachedApiCalls) {
			var channels = [];
			var spots = [];
			var lastSongAjaxTerm = '';

			CachedApiCalls.getAllChannels().then(function (response) {
				channels = response || [];
			}, { backgroundLoad: true });

			$scope.getChannelsAutocomplete = function (term) {
				return channels.filter(function (channel) {
					return channel.Name.toLowerCase().indexOf(term.toLowerCase()) !== -1;
				});
			};

			//We are filtering songs for 2 letter term in server
			//additional filtering is done on client side
			//This is called only when first 2 letters are different
			$scope.getSpotsAutocomplete = function (term) {
				if (!lastSongAjaxTerm || term.indexOf(lastSongAjaxTerm.substring(0, 2)) !== 0) {
					lastSongAjaxTerm = term.substring(0, 2);
					$scope.loadingSpots = true;

					return Service('GetUserSpots', {
						term: term,
						onlyUploaded: true
					}, { backgroundLoad: true }).then(function (response) {
						spots = response || [];
						return getFilteredSongs(term);
					}).finally(function () {
						$scope.loadingSpots = false;
					});
				} else {
					return getFilteredSongs(term);
				}

			};

			function getFilteredSongs(term) {
				var spotsToReturn = [];
				for (var i = 0; spots.length > i; i++) {
					var spot = spots[i];
					spot.displayName = spot.Name || spot.Filename;
					if (spot.displayName.toLowerCase().indexOf(term.toLowerCase()) !== -1) {
						spotsToReturn.push(spot);
						if (spotsToReturn.length === 10) {
							return spotsToReturn;
						}
					}

				}

				return spotsToReturn;
			}

		}]).directive('auditLogAutocomplete', [function () {
			return {
				restrict: 'E',
				scope: {
					selectedChannels: '=',
					selectedSpots: '='
				},
				templateUrl: '/Reports/AuditLog/auditLogAutocomplete.html',
				controller: 'auditLogAutocompleteCtrl'
			};
		}]);
