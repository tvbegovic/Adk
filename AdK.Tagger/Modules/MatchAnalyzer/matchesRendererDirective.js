angular.module('app')
	.directive('matchRenderer', [ function () {
		return {
			restrict: 'E',
			scope: {
				matches: '=',
				from: '=',
				to: '=',
				width: '=',
				height: '=',
				barHeight: '=?',
				barPadding: '=?'
			},
			link: function ($scope, element, attrs) {
				/*var canvas = document.createElement('canvas');
				canvas.width = $scope.width;
				canvas.height = $scope.height;*/
				/*canvas.style.width = '100%';
				canvas.style.height = $scope.height + 'px';*/
				//element[0].appendChild(canvas);

				var mainDiv = document.createElement('div');
				mainDiv.style.height = $scope.height + 'px';
				mainDiv.style.position = 'relative';
				element[0].appendChild(mainDiv);
				var gridDivs = {};
				var minGridDistancePx = 5;

				var layers = [];

				if (!$scope.barHeight) {
					$scope.barHeight = 50;
				}
				if (!$scope.barPadding) {
					$scope.barPadding = 2;
				}
				var seconds; 
				var pixelPerSecond; 

				$scope.$watch('matches',
				(newValue, oldValue, scope) => {
					if (oldValue != newValue && newValue != null) {
						var mFrom = moment($scope.from);
						var mTo = moment($scope.to);
						seconds = mTo.diff(mFrom, 's', true);
						pixelPerSecond = mainDiv.clientWidth / seconds;
						buildLayersAndBlocks(newValue);
						requestAnimationFrame(drawData.bind(scope));
					}

					});

				function drawData(scope) {
					/*var ctx = canvas.getContext('2d');
					ctx.fillStyle = 'blue';
					ctx.clearRect(0, 0, canvas.clientWidth, canvas.clientHeight);
					for (var i = 0; i < $scope.matches.length; i++) {
						var match = $scope.matches[i];
						ctx.fillRect(match.start * pixelPerSecond, 0, (match.end - match.start) * pixelPerSecond, $scope.barHeight);
					}*/
					mainDiv.innerHTML = '';
					gridDivs = {};
					drawGrid(pixelPerSecond);
					for (var i = 0; i < layers.length; i++) {
						for (var j = 0; j < layers[i].length; j++) {
							var div = document.createElement('div');
							var match = layers[i][j];
							var start = match.songStart >= 0 ? match.songStart : 0;
							var end = match.songEnd <= seconds ? match.songEnd : seconds;
							div.className = 'ma_layer_0';
							div.style.top = (i * $scope.barHeight + i*$scope.barPadding).toString() + 'px';
							div.style.left = (start * pixelPerSecond).toFixed(0) + 'px';
							div.style.height = $scope.barHeight + 'px';
							div.style.position = 'absolute';
							div.style.display = 'flex';
							div.style.width = ((end - start) * pixelPerSecond).toFixed(0) + 'px';

							var hasLead = match.match_start > 0 && match.start > 0;
							var hasTail = match.songEnd > match.end && match.end < seconds;
							var leadWidth = match.start - Math.max(match.songStart, 0);
							var tailWidth = Math.min(seconds, match.songEnd) - match.end;

							var matchDiv = document.createElement('div');
							matchDiv.className = 'ma_matched';
							matchDiv.style.width = !hasLead && !hasTail ? div.style.width : ((end - start - leadWidth - tailWidth) * pixelPerSecond).toFixed(0) + 'px';
							matchDiv.style.height = '100%';
							matchDiv.title = match.Song.Title + ' Start: ' + moment(match.match_occurred).format(checkDateFormat(match.match_occurred)) + ' End: ' +
								moment(match.match_ended).format(checkDateFormat(match.match_ended)) + ' BER: ' + match.min_ber.toFixed(4);
							var leadDiv, tailDiv;
							if (hasLead) {
								leadDiv = document.createElement('div');
								leadDiv.className = 'ma_unmatched';
								leadDiv.style.height = '100%';
								leadDiv.style.width = (leadWidth * pixelPerSecond).toFixed(0) + 'px';
								leadDiv.title = match.Song.Title + ' Start: ' + moment($scope.from).add(match.songStart,'s').format(checkDateFormat(match.songStart)) + ' Duration: ' + leadWidth.toFixed(2) + 's';
								div.appendChild(leadDiv);
							}
							div.appendChild(matchDiv);
							if(hasTail) {
								tailDiv = document.createElement('div');
								tailDiv.className = 'ma_unmatched';
								tailDiv.style.height = '100%';
								tailDiv.style.width = (tailWidth * pixelPerSecond).toFixed(0) + 'px';
								tailDiv.title = match.Song.Title + ' End: ' + moment($scope.from).add(match.songEnd, 's').format(checkDateFormat(match.songEnd)) + ' Duration: ' + tailWidth.toFixed(2) + 's';
								div.appendChild(tailDiv);
							}
							
							mainDiv.appendChild(div);
						}
						
					}
				}

				function drawGrid(pixelPerSecond) {
					var mTime = moment($scope.from);
					for (var s = 0; s < seconds; s++) {
						var showDiv = mTime.seconds() == 0 && 60 * pixelPerSecond >= minGridDistancePx ||
							mTime.seconds() % 5 == 0 && 5 * pixelPerSecond >= minGridDistancePx ||
							pixelPerSecond >= minGridDistancePx;
					
						var div = s in gridDivs ? gridDivs[s] : null;

						if (div == null) {
							if (showDiv) {
								div = document.createElement('div');
								div.style.position = 'absolute';
								div.className = 'ma_tick ma_' + (mTime.seconds() == 0 ? 'major' :
									mTime.seconds() % 5 == 0 ? 'minor' : 'minor_minor') + '_tick';
								mainDiv.appendChild(div);
								gridDivs[s] = div;
								div.title = mTime.format('HH:mm:ss');
							}
						} else {
							div.style.display = showDiv ? 'block' : 'none';							
						}
						if(showDiv)
							div.style.left = (s * pixelPerSecond).toFixed(0) + 'px';
						mTime.add(1, 'seconds');
					}
				}

				function checkDateFormat(date) {
					if (moment(date).isBefore($scope.from, 'day') || moment(date).isAfter($scope.from, 'day')) {
						return 'DD/MM/YYYY HH:mm:ss';
					} else {
						return 'HH:mm:ss';
					}
				}

				function buildLayersAndBlocks(data) {
					layers = [];
					for (var i = 0; i < data.length; i++) {
						var layer = getLayer(data[i].start, data[i].end);
						layer.push(data[i]);
					}
				}

				function getLayer(start, end) {
					var result = null;
					var blocks = null;
					for (var i = 0; i < layers.length; i++) {
						matches = layers[i];
						var match = matches.find(b => (start >= b.songStart && start < b.songEnd) || (end >= b.songStart && end < b.songEnd));
						if (match == null) {
							result = layers[i];							
							break;
						}
					}
					if (result == null) {
						result = [];
						layers.push(result);
					}					
					return result;
				}
			}
		};
	}]);
