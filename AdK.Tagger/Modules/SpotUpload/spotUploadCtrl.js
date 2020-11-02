angular.module('app')
	.controller('spotUploadCtrl', ['$scope', '$rootScope', 'Service', 'Upload', '$timeout', 'SpotLibraryService',
		function ($scope, $rootScope, Service, Upload, $timeout, SpotLibraryService) {
			$scope.uploads = [];
			var uploadStatus = {
				uploading: 'UPLOADING',
				queued: 'QUEUED',
				ok: 'OK',
				error: 'ERROR'
			};

			$scope.uploadFiles = function (files) {

				_.forEach(files, function (file) {
					file.status = uploadStatus.uploading;
					$scope.uploads.push(file);

					Upload.upload({ url: '/SpotUpload.ashx', file: file })
						.then(function success(response) {
							angular.extend(file, response.data); // adds {originalName, filename, sampleId} properties
						}, function onError() {
							file.status = uploadStatus.error;
						}, function status(evt) {
							file.total = parseInt(evt.total);
							file.loaded = Math.min(parseInt(evt.loaded), file.total);
							file.progress = parseInt(100.0 * file.loaded / file.total);
						});
				});

				runStatusUpdater();
			};

			function runStatusUpdater() {

				var queued = $scope.uploads.filter(function (sample) {
					return (sample.status === uploadStatus.queued || sample.status == uploadStatus.uploading)
				});
				var queuedWithSampleId = queued.filter(function (s) { return s.sampleId; });

				if (queuedWithSampleId.length) {

					var sampleIds = queuedWithSampleId.map(function (sample) { return sample.sampleId; });
					Service('UpdateSampleStatuses', { sampleIds: sampleIds }, { backgroundLoad: true })
						.then(function (statuses) {
							statuses.forEach(function (sampleStatus, index) {
								var sample = queued[index];
								sample.status = sampleStatus.StatusString;
								sample.duration = sampleStatus.Duration; // 0 if no duration
								sample.error = sampleStatus.Error; // null if there is no error
								sample.queuePosition = sampleStatus.QueuePosition;
							});

							if (statuses.some(function (s) { return s.Status === uploadStatus.ok })) {
								refreshSpotLibrary();
							}

							return statuses;
						}).finally(function () {
							$timeout(runStatusUpdater, 1000);
						});
				} else if( queued.length ) {
						$timeout(runStatusUpdater, 1000);
				}
			}


			//SPOT LIBRARY REGION
			$scope.spotLibrary = {};
			$scope.spotLibrary.filter = SpotLibraryService.getDefaultFilter();
			$scope.spotLibrary.filter.songStatuses = [
				SpotLibraryService.songStatus.new,
				SpotLibraryService.songStatus.processed,
				SpotLibraryService.songStatus.mailed
			];
			$scope.spotLibrary.canEdit = true;
			$scope.spotLibrary.canDelete = true;

			$scope.spotLibrary.loadSpots = function (pageSize, pageNum) {
				$scope.spotLibrary.lastPageSize;
				$scope.spotLibrary.lastPageNum;
				return SpotLibraryService.loadSpots(pageSize, pageNum, $scope.spotLibrary.filter).then(function (response) {
					$scope.spotLibrary.totalCount = response.totalCount;
					return response;
				});
			};

			$scope.moveScannedToSpotLibrary = function () {
				Service('MoveScannedSpotsToSpotLibrary', null, { backgroundLoad: true })
					.then(refreshSpotLibrary);
			};

			function refreshSpotLibrary() {
				$rootScope.$broadcast('refreshSpotList');
			}

		}]);
