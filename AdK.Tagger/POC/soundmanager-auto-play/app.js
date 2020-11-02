
angular.module('poc-app', [])
  .factory('soundmanager', ['$q', function($q) {
    var q = $q.defer();

    soundManager.debugMode = false;
    soundManager.setup({
      url: 'js/soundmanager2/swf/',
      onready: function() {
        q.resolve();
      },
      ontimeout: function() {
        q.notify("Failed to setup SoundManager2");
      },
      useHighPerformance: true,
      useFastPolling: true
    });

    return q.promise;
  }])
  .controller('soundmanagerPocCtrl', ['$scope', '$http', 'soundmanager',
    function($scope, $http, soundmanager) {
      $scope.statuses = [];

      function addStatus(status) {
        $scope.statuses.push({
          name: status
        });
      }

      $scope.player = {
        stop: stop,
        reset: reset,
        playingInfo: {
          uiIndentifier: null,
          songUrl: null,
          songDuration: 0,
          songPosition: 0,
          getCompletePercentage: function() {
            // console.log((this.songPosition / this.songDuration) * 100);
            return (this.songPosition / this.songDuration) * 100;
          },
          isPlaying: false
        }
      };

      ///We will preload sound that's why we use 2 playera
      var currentMp3 = 1;
      var previousSound;
      var currentSound = {};
      var nextSound = {};

      soundmanager.then(function() {
        currentSound = createSound(currentMp3 + '.mp3');
        playCurrentSound();
        createNextSound();
      });

      function createNextSound() {
        currentMp3++;
        if (currentMp3 <= 10) {
          nextSound = createSound(currentMp3 + '.mp3');
        } else {
          currentMp3 = 10;
          nextSound = null;
        }
      }

      function createPreviousSound() {
        currentMp3--;
        if (currentMp3 > 0) {
          previousSound = createSound(currentMp3 + '.mp3');
        } else {
          previousSound = null;
          currentMp3 = 0;
        }
      }


      function createSound(mp3) {
        addStatus('creating sound for ' + mp3);
        return soundManager.createSound({
          url: '/POC/soundmanager-auto-play/mp3/' + mp3,
          onload: function() {
          },
          onfinish: function() {
            console.log('song finish status');
            playNextSound();
          }
        }).load();

      }

      function playNextSound(position) {
        if (nextSound) {
          addStatus('playing next sound');
          currentSound.stop();
          previousSound = currentSound;
          currentSound = nextSound;
          playCurrentSound(position);
          createNextSound();
        } {
          addStatus('no next sound to play');
        }
      }

      function playPreviousSound(position) {
        if (previousSound) {
          addStatus('playing previous sound');
          currentSound.stop();
          nextSound = currentSound;
          currentSound = previousSound;
          setTimeout(function() {
            playCurrentSound(position);
          });
          createPreviousSound();
        } else {
          addStatus('no previous sound to play');
        }
      }

      function playCurrentSound(position) {
        if (position < 0) {
          position = currentSound.duration - Math.abs(position);
        }

        if (position) {
          setTimeout(playProxy.bind(null, position), 50);
        } else {
          playProxy(0);
        }

      }

      function playProxy(position) {
        currentSound.play({
          from: position,
          whileplaying: function() {
            $scope.player.playingInfo.songDuration = currentSound.duration;
            positionChanged(currentSound.position);
          }
        });
      }

      $scope.goToEnd = function() {
        var position = currentSound.duration - 10000;
        currentSound.setPosition(position);
      };

      $scope.addSeconds = function() {
        var position = currentSound.position + 5000;
        if (position > currentSound.duration) {
          currentSound.stop();
          var playerPosition = position - currentSound.duration;
          playNextSound(playerPosition);
        } else {
          currentSound.setPosition(position);
        }
      };

      $scope.removeSeconds = function() {
        var position = currentSound.position - 5000;
        if (position < 0) {
          currentSound.stop();
          playPreviousSound(position);
        } else {
          currentSound.setPosition(position);
        }
        currentSound.setPosition(position);
      };


      function stop() {
        $scope.player.playingInfo.songPosition = 0;
        $scope.player.playingInfo.isPlaying = false;
        if (currentSound && currentSound.playState == 1) { currentSound.stop(); }
      }

      function reset() {
        stop();
        $scope.player.playingInfo.uiIndentifier = null;
        $scope.player.playingInfo.songDuration = 0;
        $scope.player.playingInfo.songUrl = null;

      }

      function positionChanged(position) {
        $scope.$apply(function() {
          $scope.player.playingInfo.songPosition = position;
        });
      }

      $scope.togglePause = function() {
        currentSound.togglePause();
        $scope.player.playingInfo.isPlaying = !currentSound.paused;
      };


    }]);
