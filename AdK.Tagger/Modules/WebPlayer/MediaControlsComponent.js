function MediaControlsController(element, $timeout) {
	var ctrl = this;
	var moveHandler;

	ctrl.getPlayButtonCss = function () {
		return ctrl.playing ? 'glyphicon glyphicon-pause' : 'glyphicon glyphicon-play'
	};

	ctrl.togglePlayPause = function () {
		ctrl.onPlayPause();
	};

	ctrl.progressValue = function () {
		return Math.floor(ctrl.currentTime / ctrl.getTotalTime()  * 100);
	};

	ctrl.getTotalTime = function () {
		return moment(ctrl.to).diff(ctrl.from, 'seconds', true);
	};

	ctrl.onProgressBarClick = function (event) {
		ctrl.onSeek({
			time: event.offsetX / event.target.clientWidth * ctrl.getTotalTime()
		});
	};

	ctrl.formattedPlayTime = function (time) {
		return moment(ctrl.from).add(time, 'seconds').format('DD/MM/YYYY HH:mm:ss');
	};

	ctrl.onProgressBarMouseMove = function (event) {
		if (!moveHandler) {
			moveHandler = $timeout(function () {
				event.target.title = ctrl.formattedPlayTime(event.offsetX / event.target.clientWidth * ctrl.getTotalTime());
				moveHandler = null;
			}, 10);
		}		
	};

}

angular.module('app')
	.component('wpMediaControls',
		{
			templateUrl: '/Modules/WebPlayer/mediaControlsComponent.html',
			bindings: {
				from: '<',
				to: '<',
				currentTime: '<',
				playing: '<',
				onPlayPause: '&',
				onSeek: '&'

			},
			controller: ['$element', '$timeout', MediaControlsController]
		});
