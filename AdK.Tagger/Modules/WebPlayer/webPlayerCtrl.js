angular.module('app')
	.controller('webPlayerCtrl', ['$scope', '$routeParams', '$modal', '$window', '$http', '$interval', '$timeout', '$sce', '$location', '$rootScope',
		'$route', '$q','Service', 'confirmPopup','webPlayerFactory',
		function ($scope, $routeParams, $modal, $window, $http, $interval, $timeout, $sce, $location, $rootScope, $route, $q, Service, confirmPopup, webPlayerFactory) {
			$scope.songs = [];
			$scope.chunks = [];
		
			var currentSecondForLoad = 0;
			var playerLength = 3600;
			var waveFormSeconds = 60;
			$scope.dt = moment().startOf('hour').toDate();
			var rowLabelWidth = 75;	//If these are changed in css (wp_rowlabel), it should be changed here too
			var rowEndColumnWidth = 30; //wp_rowend
			$scope.chunkLengthPx = 0;
			$scope.chunkHeightPx = 42;
			var pixelPerSecond = 100;
			var pixelPerMs = pixelPerSecond / 1000;
			var currentHour = null;
			var activeChunkIndex = null;
			//var currentAudio = null;
			var currentTime = 0;
			$scope.soundFiles = [];
			var soundDuration = 300;

			$scope.playing = false;
			var regionStarted = false;
			var region = null;
			var regionTimer = null;
			var currentDuration;
			var regionStepMs = 100;
			var songDuration = 10;
			var activeChunk;
			var currentTime = 0;
			$scope.channelData = {};
		
			var currentMonth;
			var currentYear;
			var tickInterval = 50;
		
			let currentIndex = 0;

		
			var currentSegment = null;
			var segmentFill = { 1: 'full', 2: 'start', 3: 'end' };
			var segments = [];
			var firstTimeResize = false;
		
			var keys = ['i', 'o', 'j', 'k', '1', '2', '3', '4',' ','m', 'b'];

			$scope.layers = [1, 2, 3, 4];
			$scope.activeLayer = '1';

			var dictSegments = {}	//dictionary, key is layer
			var dictShouldRenderSegments = {} //dictionary, key is layer
			var segmentOrderNo = 1;
			$scope.segmentResizing = null;		//sequence of the active resizing segment
			var renderSegmentTolerance = 0.05;	//in seconds

			var resizeInfo = null;
			var lastResizeData = {
				start: null,
				width: null,
				seq: null			
			}
			$scope.resizingEdge = null;
			var waveResizeElement = null;
			var debug = false;
			var fillDirectionRight = 1, fillDirectionLeft = 2;
			var resizingEdgeRight = 1, resizingEdgeLeft = 2;

			$scope.waveData = {
				segmentResizing: null,
				resizingEdge: null,
				lastChunkIndex: null,
				source: null,
				getSegment: function (layer, sequence) {
					return layer in dictSegments ? dictSegments[layer].find(s => s.sequence == sequence) : null;				
				}
			}
			var currentSegment = null;

			var clipValueTypeEnum = {
				client: 1,
				programType: 2,
				programName: 3,
				event: 4,
				language: 5
			}

			var userData = {
				savedVisits: {}
			}

			$scope.datepickers = {
				format: 'dd/MM/yyyy',
				options: {},
				from: {
					opened: false
				},
				to: {
					opened: false
				},
				open: function (picker, event) {
					if (picker == 1) {
						$scope.datepickers.from.opened = true;
					} else {
						$scope.datepickers.to.opened = true;
					}
				}
			}

			$scope.dateTo = getNextHour($scope.dt);
			var currentSoundFile = null;
			var maxClipLength = 3600; //seconds
			var clipStatuses = { created: 'created', new: 'new' };
			var isoFormat = 'YYYY-MM-DD HH:mm:ss.SSS';
			var isoDateOnlyFormat = 'YYYY-MM-DD';
			var routeDateFormat = 'YYYY-MM-DD HH:mm';
			var segmentSaving = false;
			var minimumSegmentDuration = 5; //seconds

			$scope.clipStart = $scope.dt;
			$scope.clipEnd = $scope.dateTo;

			$scope.dateTimeBoxOffsets = [-5, -1, 1, 5];
			$scope.dateTimeBoxDurations = [5, 10, 15, 30];

			var tags = [];

			var modalOpened = false;

			var throttlingIntervals = [
				{ maxClipDuration: 20, waitDuration: 5 },
				{ maxClipDuration: 60, waitDuration: 9}
			]

			var clipLoadStartTime = null;

			$scope.showWaveforms = false;
			$scope.channels = webPlayerFactory.channels;
			$scope.selectedChannel = webPlayerFactory.selectedChannel;

			$scope.currentDay = moment($scope.dt);	//used to prevent flicker when changing dates
			//$scope.pickerDisabled = true;
			var autoScrollRows = 2;
			$scope.channelType = null;

			var thumbOriginalWidth = 640;
			var thumbOriginalHeight = 360;

			var thumbWidth = 100;
			var thumbHeight = 56; 
			var thumbBlockStep = 300;	//300 sec = 5 minutes
			var thumbBlockRows = 4;
			var thumbMarginSize = 1;
			var thumbStep = 15;	//every 15 seconds
			$scope.thumbRowHeaderWidth = 50;
			
			var thumbImages = [];
			$scope.thumbBlocks = [];	//5 minute blocks
			$scope.showVideo = false;
			var videoPlayerTimer = null;
			var vp_currentTime = null;
			$scope.formattedPlayTime = null;

			var vp_FrameStep = 0.04;

			$scope.timelineOptions = {
				thumbWidth: thumbWidth,
				thumbHeight: thumbHeight,
				margin: thumbMarginSize,
				thumbStep: thumbStep,
				blockTime: 300,
				isoFormat: isoFormat,
				thumbUrl: ''
			};

			$scope.videoStartTime = null;

			$scope.media = {
				sources: []
			};

			var vp_offset = 0;

			function init() {

				var from = $routeParams.from != null ? moment($routeParams.from) : null;
				var to = $routeParams.to != null ? moment($routeParams.to) : null;
				if (to != null && (to.diff(from, 'seconds') > playerLength || to.isBefore(from))) {
					to = moment(from).add(1, 'hour');
				}

				getChannels($routeParams.id).then(function (channels) {
					$scope.channels = channels;
					$scope.selectedChannel = webPlayerFactory.selectedChannel;
					if ($scope.selectedChannel) {
						$scope.channelType = !$scope.selectedChannel.MediaType ? 'Radio' :  $scope.selectedChannel.MediaType;
					}				
					//Check subscription limit
					var outOfSubscriptionLimit = from != null && !checkIsInSubsriptionLimit(from);
					if (from != null && to != null && !outOfSubscriptionLimit) {
						$scope.dt = from.toDate();
						$scope.dateTo = to.toDate();
					}
					getUserData();
					if ((from == null || to == null) && !outOfSubscriptionLimit) {
						var date = new Date();
						if ($scope.selectedChannel != null) {
							if ($scope.selectedChannel.Id in userData.savedVisits) {
								date = moment(userData.savedVisits[$scope.selectedChannel.Id].lastDateFrom).startOf('minute').toDate();
								$scope.dt = date;
								$scope.dateTo = moment(userData.savedVisits[$scope.selectedChannel.Id].lastDateTo).startOf('minute').toDate();
							}
						}
					}
				
					//$scope.loadMonth($scope.selectedChannel, $scope.dt.getFullYear(), $scope.dt.getMonth() + 1);
					loadMonths($scope.dt);

					loadDay();
					currentHour = moment($scope.dt).hours();
					if (from != null && to != null && !outOfSubscriptionLimit) {
						$scope.clipStart = $scope.dt;
						$scope.clipEnd = $scope.dateTo;
						currentHour = from.hours();
						loadClip();
					}
					else {
						$scope.loadForHour(currentHour);
					}

				});

				if (!webPlayerFactory.tags) {
					Service('GetClipTags').then(function (data) {
						tags = data;
					});
				}
			
				//currentMonth = $scope.dt.getMonth() + 1;
				//currentYear = $scope.dt.getFullYear();

				angular.element($window).on('resize', function () {
					//_.debounce($scope.$apply(refreshWaveForms), 1000);
					onResize();
				});

				$rootScope.$watch('menuExpanded', function (newValue, oldValue, scope) {
					if (oldValue != newValue) {
						$timeout(onResize, 500);
					}				
				});

				function onResize() {
					if (clipLoadStartTime != null) {
						if (!resizeHandler) {
							resizeHandler = $timeout(function () {
								resizeHandler = null;
								setWaveFormsDivHeight();								
								refreshChunks();
								firstTimeResize = true;
							}, 100);
						}
					}
					if (isTV()) {
						if (!resizeHandler) {
							resizeHandler = $timeout(function () {
								resizeHandler = null;
								calculateThumbsWidth();
							}, 100);
						}						
					}
				}



				angular.element($window).on('keyup', function (event) {
					if (event.ctrlKey || (event.key == ' ' && !modalOpened)) {
						if (keys.indexOf(event.key) >= 0) {
							event.preventDefault();
						}
						$scope.keyPress(event);
					}
				});

				angular.element($window).on('keydown', function (event) {
					/*if (event.ctrlKey) {
						if (event.key == '1') {
							event.preventDefault();					
						}
					}*/
					if (event.ctrlKey || (event.key == ' ' && !modalOpened)) {
						if (keys.indexOf(event.key) >= 0) {
							event.preventDefault();
						}
					}
					//$scope.keyPress(event);
				});
			}

			function getChannels(selectedId) {
				if (webPlayerFactory.channels) {
					return $q(function (resolve, reject) {
						if (webPlayerFactory.selectedChannel && !webPlayerFactory.selectedChannel.channelData) {
							initChannelData(webPlayerFactory.selectedChannel);
						}
						$scope.timelineOptions.thumbUrl = webPlayerFactory.selectedChannel.ArchivePath;
						resolve(webPlayerFactory.channels);
					});
				} else {
					return Service('GetSubscribedChannels').then(function (channels) {
						webPlayerFactory.channels = channels;
						webPlayerFactory.selectedChannel = channels.find(c => c.Id == selectedId);
						initChannelData(webPlayerFactory.selectedChannel);
						$scope.timelineOptions.thumbUrl = webPlayerFactory.selectedChannel.ArchivePath;
						return channels;
					});
				}
			}

			function initChannelData(channel) {
				if (!channel.channelData) {
					channel.channelData = {};
				}
				channel.channelData[channel.Id] = {
					monthData: {}, dayData: {}, files: {}
				};
			}

			$scope.loadMonth = function (channel, year, month) {
				//$scope.pickerDisabled = true;
				if(channel.channelData && channel.Id in channel.channelData) {
					var channelData = channel.channelData[channel.Id];
					if (!(getMonthKey(year, month) in channelData.monthData)) {
						var channelUrl = channel.ArchivePath;
						return $http.get(`${channelUrl}/GetInfoForMonth.ashx?year=${year}&month=${month}`).then(
							(response) => {
								channelData.monthData[getMonthKey(year, month)] = response.data;
								//$scope.pickerDisabled = false;
								//$scope.$broadcast('refreshDatepickers');
								return response.data;
							}
						);
					} else {
						return $q(function (resolve, reject) {
							resolve(channelData.monthData[getMonthKey(year, month)]);
						});
					}
				}
				return null;
			}
				

			$scope.loadForHour = function (hour) {
				currentHour = hour;			
				currentTime = 0;
				activeChunkIndex = null;
				$scope.dt = moment($scope.dt).startOf('day').add(hour, 'hours').toDate();
				$scope.dateTo = getNextHour($scope.dt);
				saveFromToTime();
				dictSegments = {};
				if (isTV()) {
					loadThumbs();
				}
			}

			function loadSoundsAndWaveforms() {
				$scope.clipStart = $scope.dt;
				$scope.clipEnd = $scope.dateTo;
				$scope.soundFiles = [];
				loadSounds($scope.clipStart, $scope.clipEnd);
				loadWaveForms($scope.clipStart, $scope.clipEnd);
				if (!firstTimeResize) {
					firstTimeResize = true;
					$timeout(()=>setWaveFormsDivHeight(),0);	//timeout wrapper to assure elements are in scope before accessing dom properties
				}
			}

			$scope.hourDblClick = function (hour) {
				$scope.loadClip();
			}

			function loadWaveForms(from, to) {
				var channelData = $scope.selectedChannel.channelData[$scope.selectedChannel.Id];
				var start = moment(from).startOf('hour');
				var end = moment(to).startOf('hour');
				while (start.isSameOrBefore(end)) {
					var dayKey = getDayKey(start.toDate());
					if (!(dayKey in channelData.dayData)) {
						channelData.dayData[dayKey] = {};
					}					
					if (channelData.dayData[dayKey].waveforms == null)
						channelData.dayData[dayKey].waveforms = {};
					var waveforms = channelData.dayData[dayKey].waveforms;
					var hour = start.hour();
					if (!(hour in waveforms)) {
						var result = [];
						currentSecondForLoad = hour * 3600;
						//currentDateIso = moment($scope.dt).format('YYYY-MM-DD');
						var url = 'http://media.adamantzg.com/getchannelwave.ashx';
						for (var i = 0; i < playerLength / waveFormSeconds; i++) {
							var image = new Image();
							var datetime = moment(from).startOf('day').add(currentSecondForLoad, 'seconds').format('YYYY-MM-DD HH:mm:ss');
							image.onload = onImageLoaded.bind($scope, moment(start), i);
							image.src = `${url}?h=${$scope.chunkHeightPx}&d=${waveFormSeconds}&cid=${$scope.selectedChannel.Id}&s=${datetime}`;
							result.push({
								loaded: false,
								image: image
							})
							currentSecondForLoad += waveFormSeconds;
						}
						waveforms[hour] = result;
					}
					start.add(1, 'hour');
				}			
				$timeout(()=>refreshChunks(), 0);				
			}
		

			function onImageLoaded(start, i) {
				var waveform = $scope.selectedChannel.channelData[$scope.selectedChannel.Id].dayData[getDayKey(start.toDate())].waveforms[start.hour()][i];
				waveform.loaded = true;
				//waveform.altImage = createImageWithSegmentColor(waveform.image);
				$scope.$apply();
			}

			function loadSounds(from, to) {
				var start = moment(from);
				var end = moment(to);
				if (start.isSameOrBefore(end, 'day')) {
					getSoundsForDay(start.toDate()).then(function (data) {
						start.add(1, 'day');					
						loadSounds(start, to);
					});
				}
				else {
					//Final setup - all sound files for given days loaded
					start = moment($scope.clipStart);
					var offsetStart = (getFullSeconds(start)) % soundDuration;
					start.add(-1 * offsetStart, 'second');
					end = moment($scope.clipEnd);
					var offSetEnd = (getFullSeconds(end)) % soundDuration;
					end.add(-1 * offSetEnd, 'second');
					//var index = Math.floor((start.hour()*3600 + start.minute() * 60 + start.second()) / soundDuration);
					$scope.soundFiles = [];
					while (start.isSameOrBefore(end)) {					
						var soundFile = getSoundFileFromTime(start);
						if (soundFile != null) {
							$scope.soundFiles.push(soundFile);
						}
					
						start.add(soundDuration, 'second');
					}
					var time = moment($scope.dt);
					currentSoundFile = getSoundFileFromTime(time);
					if (currentSoundFile != null) {
						//Account for anomaly when sound file doesn't start at 0 e.g. 20190724095501 and start of clip is 10:00
						offsetStart = moment($scope.clipStart)
							.diff(moment($scope.clipStart)
								.startOf('day')
								.add(currentSoundFile.hour, 'hours')
								.add(currentSoundFile.second, 'seconds'), 'seconds', true);
						currentSoundFile.shouldLoad = true;
						currentTime = time;	//currentTime is moment of time
						$timeout(() => {
							var currentAudio = getCurrentAudio();
							if (currentAudio != null) {
								currentAudio.currentTime = offsetStart;
							}
						}, 0);						

					}
				}			
			}

			function getSoundsForDay(date) {
				return new Promise(function (resolve, reject) {
					var dayKey = getDayKey(date);
					var mDate = moment(date);
					if (dayKey in $scope.selectedChannel.channelData[$scope.selectedChannel.Id].files)
						resolve($scope.selectedChannel.channelData[$scope.selectedChannel.Id].files[dayKey]);
					else {

						$http.get(`${$scope.selectedChannel.ArchivePath}/filesfordate.ashx?date=${mDate.year()}-${mDate.month()+1}-${mDate.date()}`).then(
							(response) => {
								var finalData = readFromString(response.data);
								$scope.selectedChannel.channelData[$scope.selectedChannel.Id].files[dayKey] = finalData;
								resolve(finalData);	
							}
						);
					}

				});
			}

			function loadAudio(index) {
				var element = document.getElementById('audio-' + index);
				element.src = $scope.soundFiles[index].src;
				return element;
			}

			$scope.getAudioSrc = function(index) {
				if ($scope.soundFiles[index].shouldLoad) {
					return $scope.soundFiles[index].src;				
				}
				return '';
			}

			function getCurrentAudio() {
				if (currentSoundFile != null) {
					var audio = document.getElementById('audio-' + currentSoundFile.hour.toString() + '-' + currentSoundFile.second.toString() );
					return audio;
				}
				return null;
			}

			function checkAudio(time) {
				var soundFile = getSoundFileFromTime(time);
				if (soundFile != null)
					currentSoundFile = soundFile;
			}

		

			function refreshChunks() {
			
				var rowDuration = getRowDurationSec();
				$scope.chunkLengthPx = rowDuration * pixelPerSecond;
			
				var chunks = [];
				var time = moment($scope.clipStart);
				var endTime = moment($scope.clipEnd);
				var length = endTime.diff(time, 'second')-1;
				var chunkCount = Math.ceil(length / rowDuration);
			
				//var time = moment().startOf('day').add(startHour, 'hours');
				//var positionSec = 0;
				var posPx = 0;
				var lengthPx = pixelPerSecond;			
			
				/*if (!time.isSame(endTime, 'day')) {
					waveforms = waveforms.concat($scope.selectedChannel.channelData[$scope.selectedChannel.Id].dayData[getDayKey($scope.dateTo)].waveforms);
				}*/
			
				var i = 0;
				while (i < chunkCount) {
					var waveforms = $scope.selectedChannel.channelData[$scope.selectedChannel.Id].dayData[getDayKey(time.toDate())].waveforms;
					var hour = time.hour();
					var minute = time.minute();
					var waveFormIndex = minute;//  Math.floor(i * rowDuration / waveFormSeconds);
					var waveFormOffset = time.second();//i * rowDuration % waveFormSeconds;
					var chunkWaveForms = [];
					var widthPx = 0;
					var length = rowDuration < (waveFormSeconds - waveFormOffset)
						? rowDuration
						: (waveFormSeconds - waveFormOffset);
					if (waveFormIndex < waveforms[hour].length)
						chunkWaveForms.push({
							waveForm: waveforms[hour][waveFormIndex],
							offset: waveFormOffset * pixelPerSecond,
							width: length * pixelPerSecond
						});
					if (length < rowDuration) {
						waveFormIndex++;
						if (waveFormIndex < waveforms[hour].length)
							chunkWaveForms.push({
								waveForm: waveforms[hour][waveFormIndex],
								offset: 0,
								width: (rowDuration - length) * pixelPerSecond
							});
					}

					var chunk = {
						Time: time.format('HH:mm:ss'),
						parts: chunkWaveForms,
						position: null,
						time: moment(time),
						segments: {},	//segments should be dictionary keyed by layer number,
						positionSec: time.second()
					};
					chunk.widthPx = getWaveFormsTotalLength(chunk);
					chunks.push(chunk);
					time.add(rowDuration, 'seconds');					
					//positionSec += rowDuration;
					i++;
				}
				$scope.chunks = chunks;
				if (Object.keys(dictSegments).length > 0) {
					for (var layer in dictSegments) {
						if (layer == $scope.activeLayer) {
							renderSegments(layer);
						} else {
							dictShouldRenderSegments[layer] = true;
						}
					}
				}
			
			}

			$scope.onChannelChange = function () {
			
				var urlParts = $location.path().split('/');
				var channelPartIx = urlParts.findIndex(p => p == 'channel');
				if (channelPartIx >= 0 && channelPartIx + 1 < urlParts.length)
					urlParts = urlParts.slice(0, channelPartIx + 1).concat([$scope.selectedChannel.Id]
						/*.concat(channelPartIx + 2 < urlParts.length ? urlParts.slice(channelPartIx + 2) : [])*/);
				angular.element($window).off();
				webPlayerFactory.selectedChannel = $scope.selectedChannel;
				$scope.timelineOptions.thumbUrl = webPlayerFactory.selectedChannel.ArchivePath;
				$scope.channelType = $scope.selectedChannel.MediaType;
				initChannelData(webPlayerFactory.selectedChannel);
				$location.path(urlParts.join('/'));

			}

			function getMonthKey(year, month) {
				return year * 100 + month;
			}

			function getDayKey(date) {
				if(date != null)
					return `${date.getFullYear()}${date.getMonth() + 1}${date.getDate()}`;
				return '';
			}

			$scope.onDateChange = function (date) {

				$scope.dt = date;
				$scope.dateTo = moment($scope.dt).add(1, 'hour').toDate();
				loadDay();
			}

			function loadDay() {
				if ($scope.selectedChannel && $scope.selectedChannel.channelData && $scope.selectedChannel.Id in $scope.selectedChannel.channelData) {

					saveUserData();
					var channelData = $scope.selectedChannel.channelData[$scope.selectedChannel.Id];
					var dayKey = getDayKey($scope.dt);
					if (!(dayKey in channelData.dayData)) {
						var year = $scope.dt.getFullYear();
						var month = $scope.dt.getMonth() + 1;
						var day = $scope.dt.getDate();

						$http.get(`${$scope.selectedChannel.ArchivePath}/GetInfoForDate.ashx?year=${year}&month=${month}&day=${day}`).then(
							(response) => {
								if (!(dayKey in channelData.dayData))
									channelData.dayData[dayKey] = response.data;
								else {
									Object.assign(channelData.dayData[dayKey], response.data);
								}
								$scope.currentDay = moment($scope.dt);
							}
						);
						if (isRadio()) {
							getSoundsForDay($scope.dt);
						}
					
					} else {
						$scope.currentDay = moment($scope.dt);
					}
				}

			}

			function readFromString(s) {
				var result = [];
				if (s.length > 0) {
					var lines = s.split('\n');
					for (var i = 0; i < lines.length; i++) {
						var l = lines[i];
						if (l.length > 0) {
							var main = l.split('-')[1].split('.')[0];
							var date = moment(main.substr(0, 8));
							var hour = parseInt(main.substr(8, 2));
							var second = parseInt(main.substr(10, 2)) * 60 + parseInt(main.substr(12, 2));
							date.add(hour, 'hour').add(second, 'second');
							result.push({src: `${$scope.selectedChannel.ArchivePath}/${l}`, date: date, hour: hour, second : second });
						}					
					}
				}
				return result;
			}

			$scope.$on('dt_MonthChanged', function (event, data) {
				loadMonths(data);
			});

			//Load month data for current and previous and next month
			function loadMonths(date) {
				var mDate = moment(date);
				var prevMonth = moment(mDate).add(-1, 'month');
				var nextMonth = moment(mDate).add(1, 'month');
				$q.all([$scope.loadMonth($scope.selectedChannel, mDate.year(), mDate.month() + 1),
				$scope.loadMonth($scope.selectedChannel, prevMonth.year(), prevMonth.month() + 1),
				$scope.loadMonth($scope.selectedChannel, nextMonth.year(), nextMonth.month() + 1)]).then(function () {
					$scope.$broadcast('refreshDatepickers');
				});
			}

			var resizeHandler;

		

			function setWaveFormsDivHeight() {
				var chunks = document.getElementsByClassName('wp_songRow');
				var chunkHeight = $scope.chunkHeightPx;
				if (chunks != null && chunks.length > 0 && chunks[0].clientHeight > 0) {
					chunkHeight = chunks[0].clientHeight;
				}
				var mainHeader = document.getElementsByTagName('header')[0];
				var wavePlayerHeader = document.getElementsByClassName('wp_header')[0];
				var height = window.innerHeight - mainHeader.clientHeight - wavePlayerHeader.clientHeight;
				var result = Math.floor(height / chunkHeight) * chunkHeight;
				document.getElementById('wp_songs').style.height = result + 'px';
				//Adjust radio buttons position
				document.getElementById('wp_layers').style.top = (result - 24) + 'px';
			}

			$scope.waveActiveClass = function (index) {
				return index == activeChunkIndex ? 'wp_waveactive' : '';
			}

			$scope.isActive = function (index) {
				return index == activeChunkIndex;
			}

			$scope.selectChunk = function (c) {
				activeChunk = c;
			}

		
			$scope.keyPress = function (event) {
			
				/*if (event.ctrlKey) {
					if (event.code == 'Space')
						$scope.togglePlay();
				}*/
				if (keys.indexOf(event.key) >= 0) {
					event.preventDefault();
				}
				if (event.key == ' ' && !modalOpened)
					$scope.togglePlay();
				if (event.ctrlKey) {
				
					if (event.key == 'i' /*&& event.ctrlKey*/)
						seek(-15);
					if (event.key == 'o' /*&& event.ctrlKey*/)
						seek(15);
					if (event.key == 'j' /*&& event.ctrlKey*/)
						seek(-1);
					if (event.key == 'k' /*&& event.ctrlKey*/)
						seek(1);
					if (event.key >= '1' && event.key <= '9') {
						$scope.activeLayer = event.key;
						startEndSegment(event.key);
					}
					if (event.key == 'm')
						$scope.rewindRight();
					if (event.key == 'b')
						$scope.rewindLeft();
					

				}
			}

			$scope.keyDown = function (event) {
				if (event.code == 'Space')
					event.preventDefault();
			}

			$scope.togglePlay = function() {

				if (isRadio()) {
					var currentAudio = getCurrentAudio();
					if (!$scope.playing) {
						if (activeChunkIndex == null && $scope.chunks.length > 0) {
							activeChunkIndex = 0;
							$scope.chunks[activeChunkIndex].position = 0;
						}
						if (currentAudio != null)
							currentAudio.play();
						counter = $interval(tick, tickInterval);
					} else {
						if (currentAudio != null)
							currentAudio.pause();
						$interval.cancel(counter);
						counter = null;
					}
					$scope.playing = !$scope.playing;

					disableCalendar($scope.playing);

					$scope.$broadcast('refreshDatepickers');
				} else {
					var player = getVideoPlayer();
					if (player != null) {
						if ($scope.isVideoPlaying()) {
							player.pause();
						}
						else {
							player.play();
						}
					}
				}
				
				

			}

			function tick() {
				//currentTime += tickInterval;
				/*if (currentTime % (2 * tickInterval) == 0)
					$scope.seconds = (currentTime / 1000).toFixed(2);*/
				if ($scope.playing) {
					var currentAudio = getCurrentAudio();
					if (currentAudio != null) {
						var soundFile = currentSoundFile;
						var audioTime = moment(soundFile.date).add(currentAudio.currentTime,'s');
					
						if (currentAudio.ended)
							currentTime.add(tickInterval,'ms');
						if (!currentAudio.ended && currentAudio.paused)
							currentAudio.play();
						if (audioTime.isAfter($scope.clipEnd)) {
							//Stop if reached end of the clip
							$interval.cancel(counter);
							currentAudio.pause();
							return;
						}
						if (audioTime.isAfter(currentTime) || Math.abs(audioTime.diff(currentTime,'ms')) <= 10)  //account for small precision difference >=
							currentTime = audioTime;
						else {
							//if (currentSoundIndex < $scope.soundFiles.length - 1) {
						
							var newFile = getNextSoundFile(currentSoundFile.date);
							if (newFile != null) {
								//Switch to new audio
								currentSoundFile = newFile;
								currentSoundFile.shouldLoad = true;
								currentAudio = getCurrentAudio();
								currentAudio.currentTime = currentTime.diff(newFile.date,'ms')/1000;
								$timeout(() => currentAudio.play(), 0);
							} else {
								currentTime.add(tickInterval, 'ms');
							}
							//}
						}
					} else {
						currentTime.add(tickInterval, 'ms');
						checkAudio(currentTime);
					}				
				}
			
				setPosition(currentTime);
			}

			function setPosition(mTime) {
				var time = mTime.diff(moment($scope.clipStart), 'ms')/1000;
				var position = secToPx(time);
				var index = Math.floor(position / $scope.chunkLengthPx);
				//var absTime = currentHour * 3600 + time;
				var pxPos = secToPx(mTime.diff($scope.chunks[index].time,'ms')/1000);
				if (index != activeChunkIndex)
					checkVisible(index);
				setPosition2(pxPos, index);
			}

			function setPosition2(pxPos, index, processSegment) {
				if (index != activeChunkIndex) {
					if (activeChunkIndex != null) {
						var previousChunkIndex = activeChunkIndex;
						$timeout(() => $scope.chunks[previousChunkIndex].position = $scope.chunkLengthPx + 1, 0);	//remove highlight in old chunk
					}
					if (index < $scope.chunks.length) {
						activeChunkIndex = index;
						$timeout(() => {
							$scope.chunks[activeChunkIndex].position = pxPos;
							if (processSegment == true) {
								startEndSegment();
							}
						}, 0);
					}
					else
						$interval.cancel(counter);
				}
				else {
					$timeout(() => {
						$scope.chunks[activeChunkIndex].position = pxPos;
						if (processSegment == true) {
							startEndSegment();
						}
					}, 0);
				}
				
			}

			function msToPx(ms) {
				return Math.ceil(ms / 1000 * pixelPerSecond);
			}

			function secToPx(sec) {
				return Math.ceil(sec * pixelPerSecond);
			}

			function pxToMs(px) {
				return px / pixelPerSecond * 1000;
			}

			function getFullSecondsFromMoment(moment) {
				return moment.hour() * 3600 + moment.minute() * 60 + moment.second() + moment.milliseconds()/1000;
			}

			function getFullMsFromMoment(moment) {
				return getFullSecondsFromMoment(moment) * 1000 + moment.milliseconds();
			}

			$scope.segmentFillClass = function (hour) {
				var result = 'btn btn-default btn-sm btn-block wp_segmentfill';
				var dayKey = getDayKey($scope.currentDay.toDate());
				if ($scope.selectedChannel && $scope.selectedChannel.channelData && $scope.selectedChannel.Id in $scope.selectedChannel.channelData) {
					var channelData = $scope.selectedChannel.channelData[$scope.selectedChannel.Id];
					if (dayKey in channelData.dayData) {
						var dayData = channelData.dayData[dayKey];
						if ((hour) in dayData) {
							var fillId = dayData[hour];
							if (fillId in segmentFill)
								result += ' wp_segmentfill_' + segmentFill[fillId];
						}
					}
				}
				if (hour == currentHour)
					result += ' wp_hour_selected';

				return result;
			}

			$scope.getDayClass = function (date, mode) {
				if (mode === 'day') {
					var day = date.getDate();
					var month = date.getMonth() + 1;
					var year = date.getFullYear();
					var ym = getMonthKey(year, month);
					if ($scope.selectedChannel && $scope.selectedChannel.channelData && $scope.selectedChannel.Id in $scope.selectedChannel.channelData) {
						var channelData = $scope.selectedChannel.channelData[$scope.selectedChannel.Id];
						if (ym in channelData.monthData && day in channelData.monthData[ym])
							return 'wp_day_hasData';
					}

				}
				return '';
			};		

			$scope.trustSrc = function (src) {
				return $sce.trustAsResourceUrl(src);
			}

			$scope.waveClicked = function (e, chunkIndex) {
				if (!segmentSaving) {
					var x = e.x;
					var time = moment($scope.chunks[chunkIndex].time);
					time.add(pxToMs(x), 'ms');

					//var timeSeconds = getFullSecondsFromMoment(time) - currentHour * 3600;
					currentTime = time;// timeSeconds;
					setSoundTime(time);
					setPosition2(x, chunkIndex, e.ctrlKey);			
				}		
			
			}

			//time is moment of current date/time
			function setSoundTime(time) {
				var soundFile = getSoundFileFromTime(time);
				if (soundFile != null) {
					soundFile.shouldLoad = true;
				}			
				var currentAudio = getCurrentAudio();
				if (soundFile != null && (currentSoundFile == null || !soundFile.date.isSame(currentSoundFile.date))) {
					if (currentAudio != null) {
						if ($scope.playing)
							currentAudio.pause();
						currentSoundFile = soundFile;
						currentAudio = getCurrentAudio();
						if (currentAudio != null) {
							currentAudio.currentTime = time.diff(currentSoundFile.date, 'ms') / 1000;
							if ($scope.playing)
								currentAudio.play();
						}					
					}
				} else {
					if (currentAudio) {
						currentAudio.currentTime = time.diff(currentSoundFile.date, 'ms') / 1000;					
					}				
				}
			}

			function seek(seconds) {
				
				var newTime = moment(currentTime).add(seconds, 's');
				if (newTime.isBefore($scope.dt))
					newTime = moment($scope.dt);
				if (newTime.isAfter($scope.dateTo))
					newTime = moment($scope.dateTo);
				
				if (newTime.isBefore(moment($scope.dateTo))) {
					currentTime = newTime;
					if (isRadio()) {
						setSoundTime(currentTime);
						setPosition(currentTime);
					}
				
				} else {
					currentTime = moment($scope.dateTo).add(-1, 'ms');
					if (isRadio()) {
						setPosition(currentTime);
						var currentAudio = getCurrentAudio();
						if (currentAudio != null && $scope.playing)
							currentAudio.pause();
					}					
				}
				if (isTV()) {
					setVideoTime(getVideoOffsetTime(currentTime));
				}

			
			}

			function checkVisible(index) {
				var divWaves = document.getElementById('wp_songs');
				var chunkHeight = document.getElementsByClassName('wp_songRow')[0].clientHeight;
				var topPosChunk = index * chunkHeight;
				var numScrollRows = autoScrollRows;
				if (index + numScrollRows > $scope.chunks.length - 1) {
					numScrollRows = $scope.chunks.length - 1 - index;
				}
				if (topPosChunk + chunkHeight > divWaves.scrollTop + divWaves.clientHeight)
					divWaves.scrollTop = topPosChunk + (1 + numScrollRows)*chunkHeight - divWaves.clientHeight;
				if (topPosChunk < divWaves.scrollTop)
					divWaves.scrollTop = topPosChunk;
			
			}

			var tempCanvas;

			function createImageWithSegmentColor(img) {
				/*var arrOrigColor = [0, 0, 0];
				var arrNewColor = segmentBackColor;*/
				if(tempCanvas == null)
					tempCanvas = document.createElement('canvas');
				var ctx = tempCanvas.getContext("2d");
				var w = img.width;
				var h = img.height;

				tempCanvas.width = w;
				tempCanvas.height = h;

				var color = segmentBackColor;			
				ctx.fillStyle = color;
				ctx.fillRect(0, 0, w,h);

				// draw the image on the temporary canvas
				ctx.drawImage(img, 0, 0, w, h);			

				// pull the entire image into an array of pixel data
			
				// put the altered data back on the canvas  
				ctx.putImageData(imageData, 0, 0);
				var imgResult = new Image(w,h);
				imgResult.src = tempCanvas.toDataURL('image/png');
				return imgResult;
			}

			function startEndSegment() {
				var currentPosition = isRadio() ? $scope.chunks[activeChunkIndex].position : vp_currentTime;
				if (currentPosition != null) {
					$timeout(() => createUpdateSegment($scope.chunks, activeChunkIndex, currentTime, $scope.activeLayer), 0);
				}
			}

			function createUpdateSegment(chunks, activeIndex, time, layer) {
				//scenarios:
			
				//1.segments doesn't exist on current position, create from position to the next segment or to the end of the whole hour
				var found = false;
				var segment = null;
				if (layer in dictSegments) {
					segment = dictSegments[layer].find(s => time >= s.start && time < s.end);
					if (segment != null) {
						var endTime = moment(time);
						if (endTime.isSame(segment.start)) {
							endTime.add(minimumSegmentDuration, 's');
							if (endTime.isAfter(moment($scope.clipEnd))) {
								endTime = moment($scope.clipEnd);
							}
						
						}
						segment.end = endTime;
						segment.metaData.segment_end = segment.end.format(isoFormat);
					}
					
				}
				if (segment == null) {
					var clip_start = moment($scope.clipStart);				
					var clip_end = moment($scope.clipEnd);				
					var name = 'Segment ' + segmentOrderNo.toString();
					segment = {
						start: time,
						end: getSegmentEndTime(layer, time),
						sequence: segmentOrderNo++,
						name: name,
						resizable: function () {
							return isSegmentMininumDuration(this);
						},
						metaData: {
							clip_start: clip_start.format(isoFormat),
							clip_end: clip_end.format(isoFormat),						
							clip_status: clipStatuses.created,
							clip_name: '',
							playlist_name: '',
							channel_name: $scope.selectedChannel.Name,
							channel_id: $scope.selectedChannel.Id,
							channel_pkid: $scope.selectedChannel.ExternalId,
							segment_track_number: parseInt($scope.activeLayer) - 1,
							segment_index: segmentOrderNo - 1,
							repeat: false,
							Tags: []
						}
					};
					segment.metaData.segment_start = segment.start.format(isoFormat);
					segment.metaData.segment_end = segment.end.format(isoFormat);
					segment.metaData.segment_name = segment.name;
					if (!(layer in dictSegments))
						dictSegments[layer] = [];
					dictSegments[layer].push(segment);
				}
				if (isRadio())
					renderSegment(chunks, segment, layer);
				else
					renderSegmentVideo(segment, layer);
				segmentSaving = true;
				$scope.saveSegment(segment.metaData).then(function () {
					segmentSaving = false;				
				}).catch(function () {
					segmentSaving = false;
				});

			}

			function renderSegment(chunks, segment, layer) {
				var rowDuration = getRowDurationSec();
				//first try with segment start inside clip
				var index = chunks.findIndex(c =>
					(segment.start.isSameOrAfter(c.time) && (segment.start.isBefore(moment(c.time).add(rowDuration - 1, 's'))))
				);
				if (index < 0) {
					//maybe segment starts before clip start but overlaps
					index = chunks.findIndex(c => (c.time.isSameOrAfter(segment.start) && c.time.isBefore(segment.end)));
				}
				if (index >= 0) {
					var chunk = chunks[index];
					var first = true;
					while (chunk.time.isBefore(segment.end)) {
						if (!(layer in chunk.segments))
							chunk.segments[layer] = [];
						var pxStart = segment.start.isAfter(chunk.time) ? (segment.start.diff(chunk.time,'ms')) * pixelPerMs : 0;
						var widthPx = segment.end.isBefore(moment(chunk.time).add(rowDuration - 1,'s')) ? (segment.end.diff(chunk.time,'ms')) * pixelPerMs - pxStart :
							chunk.widthPx - pxStart;
						var chunkSegment = chunk.segments[layer].find(s => pxStart >= s.start && pxStart < s.start + s.width);
						if (chunkSegment == null) {
							chunkSegment = { start: pxStart, width: widthPx, sequence: segment.sequence, first: false, last: false, source: segment };
							chunk.segments[layer].push(chunkSegment);
						} else {
							chunkSegment.start = pxStart;
							chunkSegment.width = widthPx;
						}
						if (first) {
							chunkSegment.first = true;
							first = false;
						}

						index++;
						if (index >= chunks.length) {
							chunkSegment.last = true;
							break;
						}	
						chunk = chunks[index];
						if (chunk.time.isAfter(segment.end))
							chunkSegment.last = true;
					}
					if (index < chunks.length) {
						//Remove trailing segments (after shrinking existing one)
						chunk = chunks[index];
						//var nextSegment = getNextSegment(layer, segment.end);
						var endTime = getSegmentEndTime(layer, segment.end);
						while (chunk.time.isBefore(endTime)) {
							var pxStart = 0;
							if (!(layer in chunk.segments))
								chunk.segments[layer] = [];
							var widthPx = endTime.isBefore(moment(chunk.time).add(rowDuration - 1, 's')) ? (endTime.diff(chunk.time,'ms')) * pixelPerMs - pxStart :
								$scope.chunkLengthPx - pxStart;
							var chunkSegmentIndex = chunk.segments[layer].findIndex(s => pxStart >= s.start && pxStart < s.start + s.width);
							if (chunkSegmentIndex >= 0) {
								chunk.segments[layer].splice(chunkSegmentIndex, 1);
							}
							index++;
							if (index >= chunks.length)
								break;
							chunk = chunks[index];
						}

					}
				}
						
			}

			function renderSegmentVideo(segment, layer) {

			}

			function getSegmentEndTime(layer, time) {
				if (!(layer in dictSegments))
					return moment($scope.clipEnd);
				var nextSegment = getNextSegment(layer, time);
				var end = isRadio() ? $scope.clipEnd : moment($scope.videoStartTime).add($scope.videoDuration, 'seconds');
				return moment((nextSegment != null ? nextSegment.start  : end)).add(-1,'ms');			
			}

			function getNextSegment(layer, fromTime) {
				return dictSegments[layer].find(s => s.start.isAfter(fromTime));
			}

			function getRowDurationSec() {
				var songContainer = document.getElementById('wp_songs');
				return Math.floor((songContainer.clientWidth - rowLabelWidth - rowEndColumnWidth) / pixelPerSecond);
			}					

			function renderSegments(layer) {
				if (layer in dictSegments) {
					for (var i = 0; i < dictSegments[layer].length; i++) {
						renderSegment($scope.chunks, dictSegments[layer][i], layer);
					}
				}			
			}

			$scope.$watch('activeLayer',
				(newValue, oldValue, scope) => {
					if (newValue in dictShouldRenderSegments && dictShouldRenderSegments[newValue]) {
						renderSegments(newValue);
						dictShouldRenderSegments[newValue] = false;
					}
					if ($scope.isRadio) {
						var songsElement = document.getElementById('wp_songs');
						if (songsElement)
							songsElement.focus();
					}
					
				}
			)

			$scope.segmentResized = function (data, chunkIndex) {
				var resizingEdgeRight = 1, resizingEdgeLeft = 2;
				var layer = chunkIndex != null ? $scope.activeLayer : data.layer;
				var segment = dictSegments[layer].find(s => s.sequence == data.seq);
				if (chunkIndex != null) {
					//RAdio 					
					debuglog('webplayer.segmentResized ... start: ' + data.start + ' width: ' + data.width + ' seq: ' + data.seq + ' chunkIndex: ' + chunkIndex);

					if (data.start != lastResizeData.start || data.width != lastResizeData.width || data.seq != lastResizeData.seq) {
						lastResizeData.start = data.start;
						lastResizeData.width = data.width;
						lastResizeData.seq = data.seq;

						var startX = resizeInfo.startChunkIndex * $scope.chunkLengthPx + resizeInfo.startPosX;
						var endX = chunkIndex * $scope.chunkLengthPx + data.start + (data.edge == resizingEdgeLeft ? 0 : data.width);						

						if (segment != null) {
							if (data.edge == resizingEdgeLeft)
								segment.start.add((endX - startX) / pixelPerMs, 'ms');
							else
								segment.end.add((endX - startX) / pixelPerMs, 'ms');
						}
						if (segment.start.isBefore($scope.dt)) {
							segment.start = moment($scope.dt);
						}
						if (segment.end.isAfter($scope.dateTo)) {
							segment.end = moment($scope.dateTo);
						}
						if (endX % $scope.chunkLengthPx == 0)
							segment.end = segment.end.add(-1 * segment.end.milliseconds() - 0.01, 'ms');
						if (startX % $scope.chunkLengthPx == 0)
							segment.start = segment.start.add(-1 * segment.start.milliseconds(), 'ms');
						
					}
				} else {
					var videoStartOffset = moment($scope.dt).diff($scope.videoStartTime, 'seconds', true);
					segment.start = moment($scope.videoStartTime).add(data.startTime, 'seconds');
					segment.end = moment(segment.start).add(data.duration, 'seconds');
				}
				segment.metaData.segment_start = segment.start.format(isoFormat);
				segment.metaData.segment_end = segment.end.format(isoFormat);
				segmentSaving = true;
				$scope.saveSegment(segment.metaData).then(() => {
					segmentSaving = false;
				});
				if(chunkIndex != null)
					renderSegment($scope.chunks, segment, layer);
				$scope.waveData.segmentResizing = null;
						
			}

			$scope.mouseup = function (event, chunkIndex) {
				if ($scope.waveData.segmentResizing) {
					//if outside canvas area
					if (event.target.classList.contains('wp_rowlabel') || event.target.classList.contains('wp_label')) {
						$scope.segmentResized(
							{
								start: 0,
								width: waveResizeElement.width,
								seq: $scope.waveData.segmentResizing,
								edge: $scope.waveData.resizingEdge
							}, chunkIndex);
					}
					if (event.target.classList.contains('wp_rowend')) {
						$scope.segmentResized(
							{
								start: waveResizeElement.start,
								width: waveResizeElement.width,
								seq: $scope.waveData.segmentResizing,
								edge: $scope.waveData.resizingEdge
							}, chunkIndex);
					}
			
				}
				
			}
					

			$scope.onSegmentStartResizing = function (data, chunkIndex) {
				if (data.sequence != null) {
					debuglog('onSegmentStartResizing seq: ' + data.sequence + ' posX: ' + data.posX + ' elementX: ' + data.elementX +
						' elementWidth: ' + data.elementWidth + ' resizingEdge: ' + data.resizingEdge + (chunkIndex ? ' Time: ' + $scope.chunks[chunkIndex].Time : ''));
					resizeInfo = {
						sequence: data.sequence,
						startChunkIndex : chunkIndex,
						startPosX: data.posX,
						elementX: data.elementX,
						elementWidth: data.elementWidth
					}
				
					$scope.waveData.resizingEdge = data.resizingEdge;
					$scope.waveData.segmentResizing = data.sequence;
				}			

			}

			$scope.onResizeElementChanged = function (data, chunkIndex) {
				waveResizeElement = { start: data.start, width: data.width };
				//TODO: limit resize to 5 sec
			}

			function debuglog(message) {
				if (debug)
					console.log(message);
			}

			function getWaveFormsTotalLength(chunk) {
				var result = 0;
				for (var i = 0; i < chunk.parts.length; i++) {
					result += chunk.parts[i].width;
				}
				return result;
			}

			$scope.requestChunksRender = function (data, chunkIndex) {
				for (var i = data.from; i <= data.to; i++) {
					if ($scope.chunks[i].fillDiv)
						$scope.chunks[i].fillDiv(data.sequence, data.resizingEdge == resizingEdgeLeft ? fillDirectionLeft : fillDirectionRight);
				}
			}

			$scope.waveDblClicked = function (e, chunkIndex) {
				var x = e.x;
				var time = moment($scope.chunks[chunkIndex].time);
				time.add(pxToMs(x),'ms');
				var layer = $scope.activeLayer;
				if (layer in dictSegments) {
					currentSegment = findSegment(layer, time);
					if (currentSegment != null) {
						editSegment(currentSegment);
					}					
				}
			}

			function editSegment(segment, layer) {
				modalOpened = true;
				openMetaDataModal(segment).then(
					function (returnValue) {
						modalOpened = false;
						if (returnValue) {
							if (returnValue.op == 'delete') {
								removeSegment(segment, layer);
							}
							if (returnValue.op == 'save') {
								segment.metaData = returnValue.data;
								segment.name = segment.metaData.summaryText;
								if(isRadio())
									renderSegment($scope.chunks, currentSegment, layer);
							}
						}
					},
					function () {
						modalOpened = false;
					}
				);
			}

			function removeSegment(segment, layer) {
				if(!layer)
					layer = $scope.activeLayer;
				if (layer in dictSegments) {

					if (isRadio()) {
						var rowDuration = getRowDurationSec();
						var index = $scope.chunks.findIndex(c => segment.start.isSameOrAfter(c.time) && segment.start.isBefore(moment(c.time).add(rowDuration - 1, 's')));
						if (index >= 0) {
							var chunk = $scope.chunks[index];
							while (chunk.time.isBefore(segment.end)) {
								var chunkSegmentIndex = chunk.segments[layer].findIndex(s => s.sequence == segment.sequence);
								if (chunkSegmentIndex >= 0) {
									chunk.segments[layer].splice(chunkSegmentIndex, 1);
								}
								index++;
								if (index >= $scope.chunks.length)
									break;
								else
									chunk = $scope.chunks[index];
							}
						}
					}
					
					var index = dictSegments[layer].findIndex(s => s.start.isSame(segment.start));
					if (index >= 0) {
						dictSegments[layer].splice(index, 1);
					}
				}
			}

			function openMetaDataModal(segment) {
			
				var modalInstance = $modal.open({
					animation: false,
					size: 'lg',
					templateUrl: 'SegmentModal.html',
					controller: ['$scope', '$modalInstance', '$timeout', '$rootScope', 'segment', 'saveSegment', 'tags','detagify','channel',
						function ($scope, $modalInstance, $timeout, $rootScope, segment, saveSegment, tags, detagify, channel) {

							//Make a copy in case of cancel
							var data = JSON.parse(JSON.stringify(segment.metaData));
							data.segment_start = segment.start;
							data.segment_end = segment.end;
							var segment_duration = data.segment_end.diff(data.segment_start, 'ms');
							data.segment_duration = moment(data.clip_start).startOf('hour').add(segment_duration, 'ms').format('mm:ss.S');
							data.export_date_time = new Date();
							data.clip_status = 'new';
							data.summaryText = generateSummaryText(data.segment_name);						
							$scope.data = data;
							if ($scope.data.Tags == null) {
								$scope.data.Tags = [];
							}
							updateDetectedTags(data.segment_name);

							$scope.typeaheadSettings = {
								minValue: 3,
								minValueClient: 1,
								minValueCompetitor: 1,
								minValueEvent: 1,
								minValueLanguage: 1
							}
							$scope.form = {};					
							$scope.tags = {
								tag: '',
								newId: -1,
								currentTag: null,
								tags: tags,
								selectedTags: []
							}

							var tagFilterMinChars = 3;

							$scope.errorMessage = '';
							$scope.successMessage = '';

							$scope.clipValueTypes = Object.assign({}, clipValueTypeEnum);

							$scope.lookupSearch = function (list_id, text) {
								return Service('SearchClipValues', { list_id: list_id, text: text }, { backgroundLoad: true }).then(
									function (data) {
										return data.map(d => d.text_value);
									}
								)
							}

					

							$scope.tagKeyPress = function (event) {
								if (event.key == 'Enter') {
									onTagSelected();
								}
							}

							function onTagSelected() {
								var tagText = ' #' + $scope.tags.currentTag.name.replace(new RegExp(' ', 'g'), '_');
								$scope.tags.selectedTags.push($scope.tags.currentTag);
								var segmentNameField = document.getElementById('data.segment_name');
								var pos = segmentNameField.selectionStart;
								if (pos < 0) {
									pos = $scope.data.segment_name.length - 1;
								}
								$scope.data.segment_name = $scope.data.segment_name != null ?
									$scope.data.segment_name.substring(0, pos) + tagText + $scope.data.segment_name.substring(pos)
									: tagText;
								$timeout(function () {
									$scope.onSegmentNameChange();
									segmentNameField.focus();
									$timeout(function () {
										var newPos = pos + tagText.length;
										segmentNameField.setSelectionRange(newPos, newPos);
									}, 20);
								});
							}

							$scope.formatDate = function (date) {
								return moment(date).format(isoFormat);
							}
					
							$scope.cancel = function () {
								$modalInstance.close();
							}

							$scope.saveSegment = function () {												
								if ($scope.form.main.$valid) {
									updateSegmentTags($scope.data.segment_name);
										saveSegment($scope.data).then(function () {
										$modalInstance.close({ op: 'save', data: $scope.data });
										$scope.errorMessage = '';
										//segment.metaData = data;
									}).catch(err => {
										$scope.errorMessage = err.data.Message;
										$scope.successMessage = '';
									});
								} else {
									$scope.errorMessage = 'Please fill all required fields';
								}
							}

							$scope.exportSegment = function () {
								var date = moment($scope.data.segment_start).format(isoDateOnlyFormat);
								var timeFormat = 'HH:mm:ss';
								var start = moment($scope.data.segment_start).format(timeFormat);
								var end = moment($scope.data.segment_end).format(timeFormat);
								var url = `${channel.ArchivePath}/GetAudio.ashx?date=${date}&start=${start}&end=${end}&format=mp3`;
								$rootScope.loading += 1;
								$http.get(url, { responseType: 'blob' }).then(function (response) {
									$rootScope.loading -= 1;
									var blob = response.data;
									var link = document.createElement('a');
									link.href = (window.URL || window.webkitURL).createObjectURL(blob);
									//window.open(link.href, '_blank');
									link.download = `${channel.Name}-${date}-${start}-${end}.mp3`;
									document.body.appendChild(link);
									link.click();
									document.body.removeChild(link);
								});
							

							}
							$scope.deleteSegment = function () {						
								confirmPopup.open('Delete segment', null, 'Are you sure you want to delete this segment?').then(
									() => {
										if (segment.metaData.id > 0)
											Service('DeleteClip', { id: segment.metaData.id }).then(() => {
												$modalInstance.close({ op: 'delete' });
											});
										else {
											$modalInstance.close({ op: 'delete' });
										}
									}
								);
							}

							$scope.pullAveFromDb = function () {
								Service('GetPricePerSecond', { channelId: $scope.data.channel_id, segmentStart: moment($scope.data.segment_start).format(isoFormat), type: 1 }).
									then(function (price) {
										if (price != null) {
											var totalSec = moment($scope.data.segment_end).diff(moment($scope.data.segment_start)) / 1000;
											$scope.data.ave = (price * Math.floor(totalSec)).toFixed(2);
											$scope.data.ave_per_30_sec = (price * 30).toFixed(2);
										}
									});
							}

							$scope.pullRatingsFromDb = function () {
								Service('GetPricePerSecond', { channelId: $scope.data.channel_id, segmentStart: moment($scope.data.segment_start).format(isoFormat), type: 2 }).
									then(function (price) {
										if (price != null) {
											$scope.data.tams = price;
										}
									});
							}

							$scope.onSegmentNameFocus = function (event) {
								event.target.select();
							}

							$scope.onTagFilterChange = function (event) {
								if ($scope.tags.tag.length >= tagFilterMinChars) {
									var taglist = document.getElementById('taglist');
									//if (taglist.selectedIndex < 0) {
									//	taglist.selectedIndex = 0;
									//}
									$timeout(function () {
										if (taglist.options.length > 0) {
											//if ($scope.tags.currentTag == null) {
												$scope.tags.currentTag = $scope.tags.tags.find(t => t.name == taglist.options[0].innerText);
											//}										
										}
										//taglist.focus();
									});
								}
							}

							$scope.filterTags = function (item) {
								return item.name.toLowerCase().indexOf($scope.tags.tag.toLowerCase()) >= 0;
							}

							$scope.onSegmentNameChange = function () {
								var segment_name = $scope.data.segment_name;
								updateDetectedTags(segment_name);
								$scope.data.summaryText = generateSummaryText(segment_name);							

							}

							function generateSummaryText(segment_name) {
								return detagify(segment_name);
							}

							function updateDetectedTags(segment_name) {
								if (segment_name != null) {
									var tagWords = segment_name.split(' ').filter(w => w.substring(0, 1) == '#');
									$scope.data.detectedTags = tagWords.map(w => w.replace(new RegExp('_', 'g'), ' ').substring(1)).join(', ');
								} else {
									$scope.data.detectedTags = '';
								}
							
							}

							function updateSegmentTags(segment_name) {
								//Update tags to be ready for database update
								var tags = [];
								var tagWords = segment_name.split(' ').filter(w => w.substring(0, 1) == '#');
								for (var i = 0; i < tagWords.length; i++) {
									var addTag = true;
									var tag = tagWords[i].substring(1).replace(new RegExp('_', 'g'), ' ');
									if (tag.length > 0) {
										tags.push(tag.toLowerCase());
										var clipTag = $scope.data.Tags.find(t => t.name.toLowerCase() == tag.toLowerCase());
										if (clipTag == null) {
											clipTag = $scope.tags.tags.find(t => t.name.toLowerCase() == tag.toLowerCase());
										} else {
											addTag = false;
										}
										if (addTag) {
											if (clipTag == null) {
												clipTag = { name: tag };
											}
											$scope.data.Tags.push(clipTag);
										}
									}								
								}
								$scope.data.Tags = $scope.data.Tags.filter(t => tags.indexOf(t.name.toLowerCase()) >= 0);
							}

							$scope.onTagFilterKeyUp = function (event) {
								if ($scope.tags.currentTag != null && (event.code == 'ArrowDown' || event.code == 'ArrowUp' || event.code == 'Enter')) {
									var filteredTags = $scope.tags.tags.filter($scope.filterTags);
									var index = filteredTags.findIndex(t => t.name.toLowerCase() == $scope.tags.currentTag.name.toLowerCase());
									if (event.code == 'ArrowDown' && index < filteredTags.length - 1) {
										$scope.tags.currentTag = filteredTags[index + 1];
									}
									if (event.code == 'ArrowUp' && index > 0) {
										$scope.tags.currentTag = filteredTags[index - 1];
									}
									if (event.code == 'Enter') {
										onTagSelected();
									}
								}
							}
					}],
					resolve: {
						segment: function () {
							return segment;
						},
						saveSegment: function () {
							return $scope.saveSegment;
						},
						tags: function () {
							return tags;
						},
						detagify: function () {
							return detagify;
						},
						channel: function () {
							return $scope.selectedChannel;
						}
					}
				});
				return modalInstance.result;
			}

			$scope.dateDisabled = function (date, mode) {
				var disabled = $scope.playing;
				if (!disabled) {
					return !checkIsInSubsriptionLimit(date);
				}
				return disabled;
			}

			function saveFromToTime() {
				if (!($scope.selectedChannel.Id in userData.savedVisits)) {
					userData.savedVisits[$scope.selectedChannel.Id] = {};
				}
				userData.savedVisits[$scope.selectedChannel.Id].lastDateFrom = $scope.dt;
				userData.savedVisits[$scope.selectedChannel.Id].lastDateTo = $scope.dateTo;
				saveUserData();
			}

			function getUserData() {
				var stor = localStorage.getItem('webplayer_userdata');
				if (stor != null) {
					userData = JSON.parse(stor);
				}			
			}

			function saveUserData() {			
				localStorage.setItem('webplayer_userdata', JSON.stringify(userData));
			}

			function getNextHour(date) {
				return moment(date).add(3600, 'seconds').toDate();
			}

			$scope.loadClip = function () {
				angular.element($window).off();
				if (isTV()) {
					var player = getVideoPlayer();
					if (player != null)
						player.dispose();
				}
				$route.updateParams({ from: moment($scope.dt).format(routeDateFormat), to: moment($scope.dateTo).format(routeDateFormat) });
			
			}

			function loadClip() {
				if (isRadio()) {
					$scope.showWaveforms = false;
					clipLoadStartTime = moment();
					$timeout(() => loadSoundsAndWaveforms(), 0);
					saveFromToTime();
					loadSegments().then(() => {
						var endTime = moment();
						var clipDuration = moment($scope.dateTo).diff(moment($scope.dt), 'm');
						var throttlingData = throttlingIntervals.find(ti => clipDuration <= ti.maxClipDuration);
						if (throttlingData != null) {
							var waitTime = throttlingData.waitDuration * 1000;
							var diff = endTime.diff(clipLoadStartTime, 'ms');
							if (diff < waitTime) {
								$rootScope.loading += 1;
								var extraWait = waitTime - diff;
								$timeout(() => {
									$rootScope.loading -= 1;
									$scope.showWaveforms = true;
								}, extraWait);
							} else {
								$scope.showWaveforms = true;
							}
						} else {
							$scope.showWaveforms = true;
						}
					});
					$timeout(() => document.getElementById('wp_songs').focus(), 0);
				} else {
					$scope.clipStart = $scope.dt;
					$scope.clipEnd = $scope.dateTo;
					loadThumbs();
					$scope.showVideo = true;
					saveFromToTime();
					$timeout(() => loadVideo(), 0);
					segmentsLoaded = false;
				}
				
			}

			function getFullSeconds(mDate) {
				return mDate.minute() * 60 + mDate.second();
			}

			function getSoundFileFromTime(time) {
				dayKey = getDayKey(time.toDate());
				return $scope.selectedChannel.channelData[$scope.selectedChannel.Id].files[dayKey]
					.find(f => time.isSameOrAfter(f.date) && time.isBefore(moment(f.date).add(soundDuration, 's')));
			}

			function getNextSoundFile(time) {
				dayKey = getDayKey(time.toDate());
				return $scope.selectedChannel.channelData[$scope.selectedChannel.Id].files[dayKey]
					.find(f => f.date.isAfter(time));
			}

			$scope.changeTime = function (minutes, what) {
				if (what == 's') {
					$scope.dt = moment($scope.dt).add(minutes, 'm').toDate();
					$scope.checkClipLength('s');
				} else {
					$scope.dateTo = moment($scope.dateTo).add(minutes, 'm').toDate();
					$scope.checkClipLength('e');
				}
				saveFromToTime();
			}

			$scope.quickDuration = function (duration) {
				$scope.dateTo = moment($scope.dt).add(duration, 'm').toDate();
				saveFromToTime();
			}

			$scope.checkClipLength = function(what) {
				if (what == 's') {
					if (Math.abs(moment($scope.dateTo).diff(moment($scope.dt), 's')) > maxClipLength) {
						$scope.dateTo = moment($scope.dt).add(maxClipLength, 's').toDate();
					}
				} else {
					if (Math.abs(moment($scope.dateTo).diff(moment($scope.dt), 's')) > maxClipLength) {
						$scope.dt = moment($scope.dateTo).add(-1*maxClipLength, 's').toDate();
					}
				}
			}

			$scope.saveSegment = function (metaData) {
			
				//convert dates to string to avoid timezone offset
				var data = JSON.parse(JSON.stringify(metaData));
				data.segment_start = moment(data.segment_start).format(isoFormat);
				data.segment_end = moment(data.segment_end).format(isoFormat);
				data.clip_start = moment(data.clip_start).format(isoFormat);
				data.clip_end = moment(data.clip_end).format(isoFormat);
				data.export_date_time = moment(data.export_date_time).format(isoFormat);
				if (data.__type) {
					delete data.__type;	//Remove type so ClipDto is not confused with Clip on server side (Update method)
				}			
				var method = metaData.id ? 'UpdateClip' : 'CreateClip';			
				return Service(method, { clip: data }).then(function (clip) {
				
					Object.assign(metaData, clip);
				
					metaData.segment_start = moment(metaData.segment_start).format(isoFormat);
					metaData.segment_end = moment(metaData.segment_end).format(isoFormat);
					metaData.clip_start = moment(metaData.clip_start).format(isoFormat);
					metaData.clip_end = moment(metaData.clip_end).format(isoFormat);
					metaData.export_date_time = moment(metaData.export_date_time).format(isoFormat);
			
				});			
			}

			function findSegment(layer, time) {
				if(layer in dictSegments)
					return dictSegments[layer].find(s => time.isSameOrAfter(s.start) && time.isBefore(s.end));
				return null;
			}

			function loadSegments() {
				var from = isRadio() ? moment($scope.clipStart) : moment($scope.videoStartTime);
				var to = isRadio() ? moment($scope.clipEnd) : moment($scope.videoStartTime).add($scope.videoDuration, 'seconds');
				return Service('GetSegments', {
					channelId: $scope.selectedChannel.Id, clipStart: from.format(isoFormat),
					clipEnd: to.format(isoFormat)
				})
				.then(data => {
					for (var i = 0; i < data.length; i++) {
						var s = data[i];
						s.summaryText = detagify(s.segment_name);
						var segment = {
							start: moment(s.segment_start),
							end: moment(s.segment_end),
							sequence: s.id,
							name: s.summaryText,
							metaData: s,
							resizable: function () {
								return isSegmentMininumDuration(this);
							}
						};
						
						var layer = s.segment_track_number + 1;
						if (!(layer in dictSegments)) {
							dictSegments[layer] = [];
						}
						dictSegments[layer].push(segment);
						if(isRadio())
							renderSegment($scope.chunks, segment, layer);
					}
					segmentOrderNo = 1;				
				})
			}

			function isSegmentMininumDuration(segment) {
				return segment.end.diff(segment.start, 'ms') > minimumSegmentDuration * 1000;
			}

			$scope.formatDate = function (date, format) {
				return moment(date).format(format);
			}

			function detagify(segment_name) {
				if (segment_name != null) {
					var words = segment_name.split(' ');
					if (words.length > 0) {
						//find last word that is not tag
						var index = words.slice().reverse().findIndex(w => w.substring(0, 1) != '#');
						if (index >= 0) {
							index = words.length - 1 - index;
							return words.slice(0, index + 1)
								.map(w => w.substring(0, 1) == '#' ? w.substring(1).replace(new RegExp('_', 'g'), ' ') : w).join(' ');
						} else {
							return '';
						}
					}
				}
				return segment_name;
			}

			function disableCalendar(on) {
				var buttons = document.getElementsByClassName('dtpMonthSelector');
				for (var i = 0; i < buttons.length; i++) {
					buttons[i].disabled = on;
				}
			}

			function checkIsInSubsriptionLimit(date) {
				var days_back = $scope.selectedChannel != null ?  $scope.selectedChannel.SubscribedDaysBack : null;
				if (days_back != null) {
					var mDate = moment(date);
					var targetDate = moment().add(-1 * days_back, 'd');
					return mDate.isAfter(targetDate);
				}
				return true;
			}

			function isRadio() {
				return $scope.channelType == 'Radio';
			}

			function isTV() {
				return $scope.channelType == 'TV';
			}

			//$scope.loadHourForVideo = function () {				
				
			//}

			function loadThumbs() {
				var blocks = [];
				//$scope.dt = moment($scope.dt).startOf('day').add(currentHour, 'hours').toDate();
				for (var i = 0; i < 3600; i += thumbBlockStep) {
					var block = {
						time: moment($scope.dt).add(i, 'seconds')
					};
					block.rowHeaders = generateThumbBlockRowHeaders(block.time, thumbBlockRows);
					block.thumbs = [];
					block.imgSrc = ''
					for (var j = 0; j < thumbBlockStep; j += thumbStep) {
						var thumb = {
							time: moment(block.time).add(j, 'seconds'),
							width: thumbWidth,
							height: thumbHeight,
							margin: thumbMarginSize
						};
						block.thumbs.push(thumb);
					}
					blocks.push(block);
				}
				$scope.thumbBlocks = blocks;
			}

			function calculateThumbsWidth() {
				var thumbsElem = document.getElementById('thumbsElem');
				thumbsInRow = Math.floor((thumbsElem.clientWidth - $scope.thumbRowHeaderWidth) / (thumbWidth + thumbMarginSize));
				thumbBlockRows = Math.ceil(thumbBlockStep / thumbStep / thumbsInRow);
				for (var i = 0; i < $scope.thumbBlocks.length; i++) {
					var block = $scope.thumbBlocks[i];
					block.rowHeaders = generateThumbBlockRowHeaders(block.time, thumbBlockRows);					
				}

			}

			function generateThumbBlockRowHeaders(time, rowCount) {
				var result = [];
				for (var i = 0; i < rowCount; i++) {
					var header = { title: i == 0 ? time.format('HH:mm') : '', height: thumbHeight + 2*thumbMarginSize };
					result.push(header);
				}
				return result;
			}

			//$scope.onHourSelected = function (hour) {
			//	currentHour = hour;
			//}

			$scope.getThumbStyle = function (t, block, thumbIndex) {
				return {
					width: t.width + 'px',
					height: t.height + 'px',
					'margin-left': t.margin + 'px',
					'margin-top': t.margin + 'px',
					'margin-bottom': t.margin + 'px',
					//'background-image': 'url("/Modules/Webplayer/fakeThumbs.ashx?from=' + block.time.format(isoFormat) + '")',
					'background-image': `url("${$scope.selectedChannel.ArchivePath}/GetThumbnails.ashx?date=${block.time.format('YYYY-MM-DD')}&start=${block.time.format('HH:mm:ss')}&end=${moment(block.time).add(thumbBlockStep,'seconds').format('HH:mm:ss')}&w=${thumbWidth}&h=${thumbHeight}")`,
					'background-position': (-1 * thumbIndex * thumbWidth).toString() + 'px 0px'
				};
			}

			function loadVideo() {
				//TODO: load real m3u8 link
				var player = getVideoPlayer();
				var mClipFrom = moment($scope.dt);
				var mClipTo = moment($scope.dateTo);
				//$scope.media = {
				//	sources: [
				//		{
				//			src: `${$scope.selectedChannel.ArchivePath}/GetVODClip.m3u8?date=${mClipFrom.format('YYYY-MM-DD')}&start=${mClipFrom.format('HH:mm:ss')}&end=${mClipTo.format('HH:mm:ss')}`,
				//			type: 'application/x-mpegURL'
				//		}
				//	]
				//};
				
				player.ready(() => {
					player.src({
						src: `${$scope.selectedChannel.ArchivePath}/Getm3u8.ashx?date=${mClipFrom.format('YYYY-MM-DD')}&start=${mClipFrom.format('HH:mm:ss')}&end=${mClipTo.format('HH:mm:ss')}`,
						//src: '/testvideo/GetVODClip.m3u8',
						type: 'application/x-mpegURL'
					});
					if (videoPlayerTimer != null)
						$interval.cancel(videoPlayerTimer);
					videoPlayerTimer = $interval(checkVideoPlaying, 500);
				});

				
			}

			function checkVideoPlaying() {
				var player = getVideoPlayer();
				if (player != null) {
					var time = player.currentTime();
					if (vp_currentTime != time) {												
						if (player.vhs.playlists.media_) {
							vp_currentTime = time;
							$scope.videoStartTime = player.vhs.playlists.media_.dateTimeObject;
							vp_offset = moment($scope.dt).diff($scope.videoStartTime, 'seconds', true);
							if (vp_currentTime < vp_offset) {
								//Before clip start
								vp_currentTime = vp_offset;
								time = vp_offset;
								player.currentTime(vp_currentTime);
							}
							$scope.videoDuration = player.duration();
							var clipDuration = moment($scope.dateTo).diff($scope.dt, 'seconds', true);
							if (time - vp_offset > clipDuration) {
								//After clip end
								time = vp_offset + clipDuration;
								vp_currentTime = time;
								if (!player.paused()) {
									player.pause();
								}
							}
							currentTime = moment(player.vhs.playlists.media_.dateTimeObject).add(time, 'seconds');
							$scope.formattedPlayTime = currentTime.format('DD/MM/YYYY HH:mm:ss');
							if (!segmentsLoaded) {
								loadSegments();
								segmentsLoaded = true;
							}
						}						
					}

				}
			}

			$scope.isVideoPlaying = function () {
				var player = getVideoPlayer();
				return player != null && !player.paused();
			}

			function getVideoPlayer() {
				var elem = document.getElementById('wp_video1');
				if(elem != null)
					return videojs('wp_video1');
				return null;
			}

			$scope.rewindLeft = function () {
				var player = getVideoPlayer();
				if (player != null && player.paused()) {
					if (player.currentTime() > 0) {
						player.currentTime(player.currentTime() - vp_FrameStep);
					}
				}
			}

			$scope.rewindRight = function () {
				var player = getVideoPlayer();
				if (player != null && player.paused()) {
					if (player.currentTime() < player.duration()) {
						player.currentTime(player.currentTime() + vp_FrameStep);
					}
				}
			}

			$scope.getVideoTime = function () {
				var player = getVideoPlayer();
				if (player != null) {
					return player.currentTime();
				}
				return 0;
			}

			$scope.getVideoTimeOffset = function () {
				return $scope.getVideoTime() - vp_offset;
			}

			$scope.onTimeLineClick = function (data) {
				var player = getVideoPlayer();
				if (player != null) {
					player.currentTime(data.time + vp_offset);
				}
			}

			function setVideoTime(time) {
				var player = getVideoPlayer();
				if (player != null) {
					player.currentTime(time);
				}

			}

			$scope.onThumbClick = function (t) {
				var player = getVideoPlayer();
				if (player != null) {
					player.currentTime(getVideoOffsetTime(t.time, player));
				}
			}

			function getVideoOffsetTime(time, player) {
				if (player == null)
					player = getVideoPlayer();
				return time.diff(player.vhs.playlists.media_.dateTimeObject, 'seconds', true);
			}

			$scope.getDictSegments = function () {
				return dictSegments;
			}

			$scope.onVideoSegmentDblclick = function (layer, time) {
				var segment = findSegment(layer, moment($scope.dt).add(time, 'seconds'));
				if (segment != null) {
					editSegment(segment, layer);
				}
			}

			$scope.onVideoLayerClick = function (data) {
				var player = getVideoPlayer();
				if (player != null) {
					player.currentTime(data.time);
					vp_currentTime = data.time;
					currentTime = moment($scope.videoStartTime).add(data.time, 'seconds');
					if (data.ctrlKey) {
						$scope.activeLayer = data.layer;
						startEndSegment();
					}

				}
			}

			$scope.onMediaControlsPlayPause = function () {
				$scope.togglePlay();
			}

			$scope.onMediaControlsSeek = function (time) {
				var player = getVideoPlayer();
				if (player != null) {
					player.currentTime(time + vp_offset);
				}
			}

			init();
		}
	]
);

angular.module('app').controller('SegmentModalInstanceCtrl', ['$scope', '$modalInstance', function ($scope, $modalInstance) {
	$scope.ok = function () {
		$modalInstance.close($scope.selected.item);
	};

	$scope.cancel = function () {
		$modalInstance.dismiss('cancel');
	};
}]);
