angular.module('app')
	.controller('webPlayerChannelSelectCtrl', ['$scope', '$filter', '$modal', '$location', 'confirmPopup', 'Service', 'webPlayerFactory',
		function ($scope, $filter, $modal, $location, confirmPopup, Service, webPlayerFactory) {
		$scope.channelSelected = function () {
			return $scope.selectedChannel != null;
		};

		$scope.channels = [];
		$scope.selectedChannel = null;
		$scope.gridDefinition = {
			columns: [
				{ field: 'Name', name: 'Channel name', filter: '', hasFilter: true, filterTypeahead: false },
				{ field: 'Country', name: 'Country', filter: '', hasFilter: true },
				{ field: 'City', name: 'City', filter: '', hasFilter: true }
			],
			idField: 'Id',
			pager: false,
			fixedHeader: true,
			scrollY: '80vh'
		};

		function loadChannels() {
			Service('GetSubscribedChannels').then(function (channels) {
				$scope.channels = channels;
			});
		};

		$scope.editChannel = function () {
			if ($scope.selectedChannel != null)
				$location.path('/webplayer/channel/' + $scope.selectedChannel.Id);
		};

		$scope.onChannelSelected = function (c) {
			$scope.selectedChannel = c;
			webPlayerFactory.selectedChannel = c;
			$scope.editChannel();
		}

		var initPageLoad = _.once(init);

		function init() {
			loadChannels();
		}

		initPageLoad();
	}]);
