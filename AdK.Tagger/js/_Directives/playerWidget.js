angular.module('app')
  .directive('playerWidget', ['soundmanager', function(soundmanager) {
	  return {
		  restrict: 'AE',
		  scope: {
			  player: '='
		  },
		  templateUrl: '/js/_Directives/playerWidget.html',
		  link: function (scope) {

			  scope.player = {
				  stop: stop,
				  reset: reset,
				  playPauseSong: playPauseSong,
				  playingInfo: {
					  uiIndentifier: null,
					  songUrl: null,
					  songDuration: 0,
					  songPosition: 0,
					  getCompletePercentage: function () {
						  return (this.songPosition / this.songDuration) * 100;
					  },
					  isPlaying: false
				  }
			  };

			  var firstTime = true;

			  ///Player
			  var player = {};
			  soundmanager.then(function () {
				  player = soundManager.createSound(
					  {
						  id: 'aSound'
					  });
			  });

			  function play(songUrl, loops) {
				  if (loops == null)
					  loops = 1;
				  var options = {
					  url: songUrl,
					  loops: 1,
					  from: 0,
					  volume: 100,
					  onfinish: function () {
						  scope.$apply(function () {
							  stop();
							  firstTime = false;
							  player.stop();
						  });
					  },
					  whileplaying: function () {
						  positionChanged(player.position);
					  }
				  };
				  player.play(options);
			  }

			  function stop() {
				  scope.player.playingInfo.songPosition = 0;
				  scope.player.playingInfo.isPlaying = false;
				  if (player && player.playState == 1) { player.stop(); }
			  }

			  function reset() {
				  stop();
				  scope.player.playingInfo.uiIndentifier = null;
				  scope.player.playingInfo.songDuration = 0;
				  scope.player.playingInfo.songUrl = null;

			  }

			  function positionChanged(position) {
				  scope.$apply(function () {
					  scope.player.playingInfo.songPosition = position;
				  });
			  }

			  scope.playerSeek = function (pos) {
				  var position = scope.player.playingInfo.songDuration * pos;
				  player.setPosition(position);
			  };

			  scope.togglePause = function () {
				  if (scope.player.playingInfo.songPosition == 0 && !firstTime) {
					  scope.player.playingInfo.isPlaying = true;
					  play(scope.player.playingInfo.songUrl, 1);					  
				  }
				  else {
					  player.togglePause();
					  scope.player.playingInfo.isPlaying = !player.paused;
				  }
				  
			  };

			  function playPauseSong(songUrl, songDuration, uiIndentifier, loops) {
				  if (uiIndentifier == scope.player.playingInfo.uiIndentifier) {
					  scope.togglePause();
				  } else {
					  stop();
					  play(songUrl, loops);
				  }

				  scope.player.playingInfo.uiIndentifier = uiIndentifier;
				  scope.player.playingInfo.songUrl = songUrl;
				  scope.player.playingInfo.songDuration = songDuration * 1000;
				  scope.player.playingInfo.isPlaying = !player.paused;
			  }

			  scope.$on('$destroy', function () {
				  stop();
				  player.destruct();
			  });

		  }
	  };
  }]);
