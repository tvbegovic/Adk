angular.module('app')
    .controller('priceDesigner11Ctrl', ['$scope', '$filter', '$modal', '$location','confirmPopup','Service', function ($scope, $filter, $modal,$location, confirmPopup,Service) {
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
			scrollY: '80vh',
			fixedClass: 'channelsGridFixed'
		};		

        function loadChannels () {
            Service('GetChannels').then(function (channels) {
                $scope.channels = channels;
            });
        };

		$scope.editChannel = function () {
			if ($scope.selectedChannel != null) {
				var sufix = '1.1';
				if ($location.path().indexOf('price-designer2') >= 0) {
					sufix = '2';
				}

				$location.path('/price-designer' + sufix + '/channel/' + $scope.selectedChannel.Id);
			}
        };

        $scope.onChannelSelected = function (c)
        {
            $scope.selectedChannel = c;
            $scope.editChannel();
        }

        var initPageLoad = _.once(init);

        function init() {
            loadChannels();
        }

        initPageLoad();
    }]);
