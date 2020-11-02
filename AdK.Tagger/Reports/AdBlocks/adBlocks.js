angular.module('app.reports')
    .controller('adBlocksCtrl', ['$scope', 'Service', 'ValueFormatter', function ($scope, Service, ValueFormatter) {
        var initPageLoad = _.once(load);
        $scope.loading = true;

        $scope.onDirectivesInit = function () {
            if ($scope.haveId($scope.current.channel)
                && $scope.haveId($scope.current.include)
                && $scope.current.customDate
                //&& $scope.haveId($scope.current.channelOrDate)
                && $scope.haveId($scope.current.dayPart)) {
                initPageLoad();
            }
        };

        $scope.NumberOfVisibleDayParts = 0;
        $scope.DayPartVisibility = {};
        $scope.load = load;
        function load() {
            resetPlayer();
            $scope.loading = true;
            $scope.hideMessage();
            var request = {
                channelId: $scope.current.channel.Id,
                include: $scope.current.include.Id,
                date: ValueFormatter.getServerStringDateWithoutTime($scope.current.customDate),
                channelOrDate: 0, //$scope.current.channelOrDate.Id,
                dayPart: $scope.current.dayPart.Id
            };

            Service('MediaHouseAdBlocks', request).then(function (data) {
                $scope.serverData = {
                    PeriodStart: data.PeriodStart,
                    PeriodEnd: data.PeriodEnd
                };

                $scope.DayParts = data.DayParts;
                $scope.AdBlockTable = data.AdBlockTable;

                $scope.AdBlockTable.forEach(function (channel) {
                    channel.DayRows.forEach(function (dayRow) {
                        dayRow.AdBlockDayRowDayPartSummaries.forEach(function (dayPartSummary) {

                            $scope.DayPartVisibility[dayPartSummary.DayPart.Id] = !!$scope.DayPartVisibility[dayPartSummary.DayPart.Id] || dayPartSummary.AdCount > 0;
                        });
                    });
                });

                for (var prop in $scope.DayPartVisibility) {
                    $scope.NumberOfVisibleDayParts += $scope.DayPartVisibility[prop];
                }


                if (!data.AdBlockTable.length) {
                    $scope.showMessage('NoData');
                }
            }).catch(function () {
                $scope.showMessage('Error');
            }).finally(function () {
                $scope.loading = false;
            });
        }

        $scope.$on('channels-loaded', $scope.onDirectivesInit);

        $scope.ToggleDayRow = function (dayRow, channelRow) {

            if ($scope.PreviousDayRow && $scope.PreviousDayRow != dayRow)
                $scope.PreviousDayRow.Expand = false;
            $scope.PreviousDayRow = dayRow;

            dayRow.Expand = !dayRow.Expand;

            var dayRowRowSpan = calculateDayRowRowSpan(dayRow);

            channelRow.RowSpan = dayRowRowSpan + 1;

            resetPlayer();
        }

        $scope.ToggleAdBlockDayPart = function (adBlockDayPart, adBlock, dayRow, channelRow) {

            if ($scope.PreviousAdBlockDayPart && $scope.PreviousAdBlockDayPart != adBlockDayPart)
                $scope.PreviousAdBlockDayPart.Expand = false;
            $scope.PreviousAdBlockDayPart = adBlockDayPart;


            if ($scope.PreviousAdBlock && $scope.PreviousAdBlock != adBlock)
                $scope.PreviousAdBlock.Expanded = false;
            $scope.PreviousAdBlock = adBlock;



            var expand = !adBlockDayPart.Expand;
            //adBlock.AdBlockDayParts.forEach(function (item, index) { item.Expand = false; });
            adBlockDayPart.Expand = expand;
            adBlock.Expanded = expand;
            dayRow.adBlockDayPartExpanded = expand;


            var dayRowRowSpan = calculateDayRowRowSpan(dayRow);

            channelRow.RowSpan = dayRowRowSpan + 1;

            resetPlayer();
        }

        function calculateDayRowRowSpan(dayRow) {
            return (dayRow.AdBlocks.length * 4 * !!dayRow.Expand) + 4 + !!dayRow.adBlockDayPartExpanded * !!dayRow.Expand;
        }

        ///Player
        $scope.playPauseSong = function (adBlockAd, rowIndex) {
            $scope.player.playPauseSong(adBlockAd.Mp3Url, adBlockAd.Duration, rowIndex);
        };

        function resetPlayer() {
            if ($scope.player && $scope.player.reset) {
                $scope.player.reset();
            }
        }
    }
    ]);
