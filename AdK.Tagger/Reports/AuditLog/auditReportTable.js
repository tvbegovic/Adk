angular.module('app.reports')
	.controller('auditReportTableCtrl', ['$scope', '$q', '$timeout', 'Service', 'ValueFormatter', 'channelThresholdService', 'AuditService', 'UserSettings',
		function ($scope, $q, $timeout, Service, ValueFormatter, channelThresholdService, AuditService, UserSettings) {
			$scope.message = { show: false };
			$scope.showDaySubTotal = false;
			$scope.initialized = false;
			$scope.savedAudit = false;
			var filter = undefined;
			var hours = getHours();
			var partialHoursGroup;
			var normalHoursGroup;

			UserSettings.getSettings('Defaults', 'groupEmptyAuditHours').then(function (groupEmptyAuditHours) {
				$scope.groupEmptyHours = Boolean(groupEmptyAuditHours && groupEmptyAuditHours.Value === 'True');
			});

			$scope.showMessage = function (template) {
				$scope.message.template = template;
				$scope.message.show = true;
			};

			$scope.hideMessage = function () {
				$scope.message.show = false;
			};

			function getHours() {
				return [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23];
			}

			//AUDIT REPORT
			$scope.$on('load-audit-report-table', function (event, args) {

				$scope.showPartials = AuditService.filter.showPartials;
				filter = args;
				if (filter.auditId) {
					$scope.savedAudit = true;
					channelThresholdService.getForAudit(args.auditId, args.channels).then(function (auditChannels) {
						$scope.thresholdChannels = auditChannels;
					});
				} else {
					$scope.savedAudit = false;
					$scope.thresholdChannels = args.channels;
				}

				runAuditLogReport();
			});

			$scope.$on('clear-audit-report-table', function () {
				$scope.auditReport = null;
			});

			$scope.$on('audit-view-change', function () {
				$scope.showPartials = AuditService.filter.showPartials;
			});

			$scope.getHours = function () {
				if ($scope.groupEmptyHours) {
					return $scope.showPartials ? partialHoursGroup : normalHoursGroup;
				}

				return hours;
			};

			$scope.isGroupedHour = function (hour) {
				return typeof hour === 'string';
			};

			function runAuditLogReport(silentLoading) {
				$scope.initialized = true;
				$scope.hideMessage();

				if (!silentLoading) {
					$scope.loading = true;
					$scope.auditReport = null;
				}

				$scope.dateRange = [];
				for (var d = moment(filter.dateFrom); !d.isAfter(filter.dateTo); d.add(1, 'day')) {
					$scope.dateRange.push(d.clone());
				}

				return Service('MediaHouseAuditLog', {
					channelIds: filter.channels.map(function (channel) { return channel.Id; }),
					songIds: filter.spots.map(function (spot) { return spot.Guid; }),
					dateFrom: ValueFormatter.getServerStringDateWithoutTime(filter.dateFrom),
					dateTo: ValueFormatter.getServerStringDateWithoutTime(filter.dateTo),
					auditId: filter.auditId || 0 //optional
				}, { backgroundLoad: true }).then(function (response) {

					var rows = response.Rows;
					var grandTotal = {
						NormalMatches: 0,
						PartialMatches: 0,
						ByHour: {}
					};

					if (rows && rows.length) {

						var auditReport = filter.channels.map(function (channel) {

							var ch = _.find(rows, 'ChannelId', channel.Id) || {
								ChannelId: channel.Id
							};

							ch.Channel = channel;
							$scope.dateRange.forEach(function (d) {

								var dateHaveMatch = false;
								var channelDate = _.find(ch.Dates, function (chDate) { return d.isSame(chDate.PlayDate, 'day'); });
								if (channelDate) {
									channelDate.PlayDate = d.clone();

									filter.spots.forEach(function (spot) {
										spot.displayName = spot.displayName || spot.Name || spot.fileName;
										var song = _.find(channelDate.Songs, 'SongId', spot.Guid);
										if (song) {
											dateHaveMatch = true;
											song.Spot = spot;
										}

									});

									channelDate.HaveMatch = dateHaveMatch;

								}
							});
							return ch;
						});


						auditReport.forEach(function (channel) {
							var songTotals = {};
							var songs = [];
							channel.songTotals = [];
							channel.totalsByHour = {};

							_.each(channel.Dates, function (cd) {

								//Calculate Day Subtotals
								cd.dayTotalsByHour = {};

								cd.NumberOfSongsWithNormalMatch = 0;
								cd.FirstNormalMatchIndex = null;

								cd.Songs.forEach(function (song, index) {

									songs.push(song);
									songTotals[song.SongId] = songTotals[song.SongId] || [];

									song.DayTotal = sumMatchTotals(song.CountByHour);

									for (var i = 0; i < song.CountByHour.length; i++) {
										grandTotal.ByHour[i] = grandTotal.ByHour[i] || {};
										cd.dayTotalsByHour[i] = cd.dayTotalsByHour[i] || {};
										songTotals[song.SongId][i] = songTotals[song.SongId][i] || {};

										increaseMatch([
											grandTotal,
											grandTotal.ByHour[i],
											cd.dayTotalsByHour[i],
											songTotals[song.SongId][i]
										], song.CountByHour[i]);

									}

									if (song.DayTotal.NormalMatches) {
										cd.NumberOfSongsWithNormalMatch++;

										if (cd.FirstNormalMatchIndex === null) {
											cd.FirstNormalMatchIndex = index;
										}

									}


								});

								_.each(cd.dayTotalsByHour, function (dayTotal, hour) {
									channel.totalsByHour[hour] = channel.totalsByHour[hour] || {};
									increaseMatch(channel.totalsByHour[hour], dayTotal);
								});

								cd.dayTotal = sumMatchTotals(cd.dayTotalsByHour);

							});

							channel.total = sumMatchTotals(channel.totalsByHour);

							for (var songId in songTotals) {
								if (songTotals.hasOwnProperty(songId)) {
									var song = _.find(songs, 'SongId', songId);
									var countByHour = songTotals[songId];
									channel.songTotals.push({
										spot: song ? song.Spot : {},
										countByHour: countByHour,
										total: sumMatchTotals(countByHour)
									});
								}
							}

						});

						$scope.auditReport = auditReport;
						$scope.grandTotal = grandTotal;

						//GET CHANNEL EMPTY hours
						var emptyHours = {
							normalHours: [],
							partialHours: []
						};

						Object.getOwnPropertyNames(grandTotal.ByHour).forEach(function (key) {
							if (grandTotal.ByHour[key].NormalMatches == 0) {
								emptyHours.normalHours.push(Number(key));
							}

							if ((grandTotal.ByHour[key].NormalMatches + grandTotal.ByHour[key].PartialMatches) == 0) {
								emptyHours.partialHours.push(Number(key)); //Normal match also indicates partial match
							}
						});

						normalHoursGroup = getEmptyHourGroup(emptyHours.normalHours);
						partialHoursGroup = getEmptyHourGroup(emptyHours.partialHours);

					} else {
						$scope.showMessage('NoData');
					}
				}).catch(function () {
					$scope.showMessage('Error');
				}).finally(function () {
					$scope.loading = false;
				});
			}


			function getEmptyHourGroup(emptyHours) {

				//hourGroup is constructed by 3
				var curentGroup = [];
				var groupedHours = [];
				emptyHours.forEach(function (eh) {
					if (!curentGroup.length) {
						curentGroup.push(eh);
					} else if (curentGroup[curentGroup.length - 1] === eh - 1) {
						curentGroup.push(eh);
					} else {
						if (curentGroup.length > 2) {
							groupedHours.push({
								from: curentGroup[0],
								to: curentGroup[curentGroup.length - 1]
							});
						}

						curentGroup = [eh];
					}

				});

				if (curentGroup.length > 2) {
					groupedHours.push({
						from: curentGroup[0],
						to: curentGroup[curentGroup.length - 1]
					});
				}

				var hours = getHours();
				groupedHours.forEach(function (group) {
					var groupLabel = group.from + '-' + group.to;
					var deleteCount = (group.to - group.from) + 1;
					var startIndex = hours.indexOf(group.from);
					//replace hours with label
					hours.splice(startIndex, deleteCount, groupLabel);
				});

				return hours;
			}

			function sumMatchTotals(sumFrom) {
				return {
					NormalMatches: _.sum(sumFrom, 'NormalMatches'),
					PartialMatches: _.sum(sumFrom, 'PartialMatches')
				};
			}

			function increaseMatch(matchesToIncrease, increaseFrom) {
				var arr = Array.isArray(matchesToIncrease) ? matchesToIncrease : [matchesToIncrease];

				_.each(arr, function (matchToIncrease) {
					matchToIncrease.NormalMatches = (matchToIncrease.NormalMatches || 0) + increaseFrom.NormalMatches;
					matchToIncrease.PartialMatches = (matchToIncrease.PartialMatches || 0) + increaseFrom.PartialMatches;
				});
			}

			$scope.onChannelThresholdUpdate = function () {
				runAuditLogReport(true);
			};

			$scope.updateShowPartials = function (value) {
				AuditService.filter.showPartials = value;
			};


		}]).directive('auditReportTable', [function () {
			return {
				restrict: 'E',
				scope: {
				},
				templateUrl: '/Reports/AuditLog/auditReportTable.html',
				controller: 'auditReportTableCtrl'
			};
		}]);
