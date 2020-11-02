angular.module('app')
.controller('feedPlaybackCtrl', ['$scope', '$timeout', '$routeParams', '$location', 'Service','cookieService','LocalStorage', function ($scope, $timeout, $routeParams, $location, Service,cookieService, LocalStorage) {

    $scope.songInfo = {};

    $scope.showBackButton = !!$routeParams.feedFilterId;

    $scope.showFirstAiringsInfo = !!$routeParams.reportId;

    $scope.goBack = function () {
        if ($routeParams.feedFilterName && (!$routeParams.reportId || $routeParams.reportId == '0'))
        {
            if ($routeParams.returnTo == 'adFeed')
            {
                $location.path('/add-feed');                
            } 
			else
                $location.path('/client-feeds/view/' + $routeParams.feedFilterId + '/' + $routeParams.feedFilterName);
        }            
        else
            $location.path('add-feed/' + ($routeParams.reportId != null ? $routeParams.reportId : ''));
    }
	$scope.showTitle = true;

	$scope.logo = null;

    if ($scope.$parent.user == null)
    {
		//logo only on public site
        $scope.logo = cookieService.get('playback_logo');
        if ($scope.logo == null)
            Service('GetSetting', {
                module: 'playback', key: 'logo'
            }).then(function (data) {
                $scope.logo = '/img/' + data;
                cookieService.put('playback_logo',$scope.logo);
                });
        $scope.showTitle = false;
    }
    

    if ($routeParams.songId) {
        Service('GetFeedResultSongDetails', {
            songId: $routeParams.songId,
            reportId: $routeParams.reportId || '0',
            feedFilterId: $routeParams.feedFilterId || ''
        }).then(function (data) {
            $scope.songInfo = data;
            $scope.songInfo.Created = moment($scope.songInfo.Created).toDate();

            if ($routeParams.reportId == '0' && $routeParams.feedFilterId)
            {
                var feedPlaybackExtraInfo = LocalStorage.getJson('FeedPlaybackExtraInfo');
                $scope.songInfo.Region = feedPlaybackExtraInfo.Region;
                $scope.songInfo.Market = feedPlaybackExtraInfo.Market;
                $scope.songInfo.Channels = feedPlaybackExtraInfo.Channels;
            }

            if ($scope.songInfo.Mp3Url && $scope.songInfo.Duration) {
                $timeout(function () {
                    var uiIndentifier = $routeParams.songId;
                    var songUrl = $scope.songInfo.VideoUrl ? $scope.songInfo.VideoUrl : $scope.songInfo.Mp3Url;
                    var songDuration = $scope.songInfo.Duration;

                    if (/*$routeParams.autoPlay != 'false' &&*/ !$scope.songInfo.VideoUrl)
                    {
                        $scope.player.playPauseSong(songUrl, songDuration, uiIndentifier,1);
                        
                    }
						
                    //$scope.player.playPauseSong(songUrl, songDuration, uiIndentifier);

                    if ($scope.songInfo.VideoUrl) {
         //               $scope.media = {
         //                   sources: [
         //                       {
         //                           src: $scope.songInfo.VideoUrl,
									//type: 'video/' + getExtension($scope.songInfo.VideoUrl)
         //                       }
         //                   ]
         //               }
						var player = videojs('playbackVideo', {
							overrideNative: true,
							responsive: true
						});
						player.src({
							src: $scope.songInfo.VideoUrl,
							type: 'video/' + getExtension($scope.songInfo.VideoUrl)
						});
                        //registerVideoWatch($scope.songInfo.VideoUrl);
                    }
                }, 1000);
            } else {
                $location.path('/client-feeds/view');
            }

        });


    } else {
        $location.path('/client-feeds/view');
    }

    $scope.$on('vjsVideoReady', function (e, data) {
        if($routeParams.autoPlay != 'false' && $scope.songInfo.VideoUrl)
			data.player.play();
	});

	

    $scope.endsWith = function (videoUrl, extension) {
        if (videoUrl) {
            return videoUrl.endsWith(extension);
        }
        else return false;
    }

    function getExtension(url)
    {
        var regEx = /(?:\.([^.]+))?$/;
        return regEx.exec(url)[1];
    }

    var registerVideoWatch = function (videoUrl) {
        //$scope.videoUrl = videoUrl;
        var video = document.getElementById('playbackVideo');
        video.setAttribute("src", videoUrl);
        /*$scope.$watch('player.playingInfo', function (playingInfo) {
            if (playingInfo) {
                if (playingInfo.isPlaying)
                    video.play()
                else
                    video.pause();
            }
        });*/
        if ($routeParams.autoPlay != 'false')
            video.play();
    }    

}]);
