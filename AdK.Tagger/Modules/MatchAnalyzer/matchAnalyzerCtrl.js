angular.module('app')
	.controller('matchAnalyzerCtrl', ['$scope','$routeParams','$location', 'Service', function ($scope, $routeParams, $location, Service) {
		$scope.channels = [];
		$scope.selectedChannel = null;
		$scope.showFilter = true;
		$scope.matches = [];
		$scope.headers = [];
		$scope.showchart = true;
		$scope.lastUrl = '';
		$scope.selection = {
			from: moment().startOf('hour').toDate(),
			selectedChannel: null,
			to: moment().startOf('hour').add(1, 'hour').toDate()			
		}
		var isoFormat = 'YYYY-MM-DD HH:mm';
		var key;

		function init() {
			key = $routeParams.key;
			if ($routeParams.channelId == null) {				
				getChannels();
			}			
			else {
				$scope.showFilter = false;
				$scope.selection.selectedChannel = { Id: $routeParams.channelId };
				if ($routeParams.from != null) {
					$scope.selection.from = moment($routeParams.from);
				}
				if ($routeParams.to != null) {
					$scope.selection.to = moment($routeParams.to);
				}
				$scope.showchart = !$routeParams.visual || $routeParams.visual == 1;
				$scope.showData();
			}

		}

		function getChannels() {			
			Service('GetChannels').then(function (channels) {
				$scope.channels = channels;				
			});						
		}

		
		$scope.showData = function () {
			$scope.lastUrl = $routeParams.channelId != null ?
				$location.absUrl() :
				`${$location.absUrl()}/${$scope.selection.selectedChannel.Id}/${moment($scope.selection.from).format(isoFormat)}/${moment($scope.selection.to).format(isoFormat)}`;

			Service('GetMatchesByCriteria', {
				channelId: $scope.selection.selectedChannel.Id,
				from: moment($scope.selection.from).format(isoFormat),
				to: moment($scope.selection.to).format(isoFormat),
				key: key
			}).then(function (data) {
				
				var from = $scope.selection.from;
				var to = $scope.selection.to;
				var periodDuration = moment(to).diff(moment(from), 's', true);
				for (var i = 0; i < data.length; i++) {
					var match = data[i];
					var end = moment(match.match_occurred).add(match.duration, 's');
					var songStart = moment(match.match_occurred).subtract(match.match_start, 's');
					var songEnd = moment(songStart).add(match.Song.Duration, 's');
					match.match_ended = end.toDate();
					if (moment(match.match_occurred).isSameOrAfter(from)) {
						match.start = moment(match.match_occurred).diff(from, 's', true);						
						if (end.isAfter(to)) {
							match.end = periodDuration;
						} else {
							match.end = match.start + match.duration;
						}
					} else {
						match.start = 0;
						if (end.isAfter(to)) {
							match.end = periodDuration;
						} else {
							match.end = match.duration - moment(from).diff(match.match_occurred,'s', true);
						}
					}
					match.songStart = moment(match.match_occurred).diff(from, 's', true) - match.match_start;
					match.songEnd = match.songStart + match.Song.Duration;
				}
				if (!$scope.showchart && data.length > 0) {
					var obj = data[0];
					for (var key in obj) {
						if(key.substring(0,1) != '_') 
							$scope.headers.push(key);
					}
				}
				$scope.matches = data;
			});
		}

		init();
	}]);
