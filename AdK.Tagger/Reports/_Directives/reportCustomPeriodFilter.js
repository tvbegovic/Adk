angular.module('app.reports')
    .directive('reportCustomPeriodFilter', ['$document', 'CurrentReport', 'UserSettings', function($document, CurrentReport, UserSettings) {
        return {
            restrict: 'AE',
            scope: {
                onChange: '&',
                onInit: '&',
                periodStart: '=',
                periodEnd: '=',
                compare: '=',
                previousStart: '=',
                previousEnd: '='
            },
            templateUrl: 'Reports/_Directives/reportCustomPeriodFilter.html',
            link: function(scope, element) {
                var filterKey = 'customPeriodKind';
                var init = true;

                scope.dropDownValues = [
                    { PeriodKind: 'Last7Days', Name: 'Last 7 days' },
                    { PeriodKind: 'Last30Days', Name: 'Last 30 days' },
                    { PeriodKind: 'MonthToDate', Name: 'Month to Date' },
                    { PeriodKind: 'LastFullMonth', Name: 'Last Full Month' },
                    { PeriodKind: 'QuarterToDate', Name: 'Quarter to Date' },
                    { PeriodKind: 'LastFullQuarter', Name: 'Last Full Quarter' },
                    { PeriodKind: 'Last2Quarters', Name: 'Last 2 Quarters' },
                    { PeriodKind: 'Last3Quarters', Name: 'Last 3 Quarters' },
                    { PeriodKind: 'Last4Quarters', Name: 'Last 4 Quarters' },
                    { PeriodKind: 'YearToDate', Name: 'Year to date' },
                    { PeriodKind: 'CustomRange', Name: 'Custom Range' }
                ];

                UserSettings.getReportFilters(filterKey).then(function(response) {
                    var lastFilterValue = response ? angular.fromJson(response.Value) : {};

                    CurrentReport.Filter.periodInfo.DateFrom = CurrentReport.Filter.periodInfo.DateFrom || lastFilterValue.DateFrom;
                    CurrentReport.Filter.periodInfo.DateTo = CurrentReport.Filter.periodInfo.DateTo || lastFilterValue.DateTo;

                    setPeriodIfExists(CurrentReport.Filter.periodInfo) ||
                        setPeriodIfExists(lastFilterValue) ||
                        setPeriod(scope.dropDownValues[0]);

                    scope.onInit();

                    //Init done
                    init = false;

                });

                scope.changeValue = function(value) {
                    if (value.PeriodKind !== CurrentReport.Filter.periodInfo.PeriodKind) {
                        setPeriod(value);
                        if (value.PeriodKind != 'CustomRange') {
                            updateReportFilter();
                            scope.onChange();
                        }
                    }
                };

                function updateReportFilter() {
                    UserSettings.updateReportFilter(filterKey, angular.toJson({
                        PeriodKind: CurrentReport.Filter.periodInfo.PeriodKind,
                        DateFrom: CurrentReport.Filter.periodInfo.DateFrom,
                        DateTo: CurrentReport.Filter.periodInfo.DateTo
                    }));
                }

                var unwatchDateTo;
                var unwatchDateFrom;
                function setPeriod(value) {
                    scope.selectedValue = value;
                    CurrentReport.Filter.periodInfo.PeriodKind = value.PeriodKind;
                    CurrentReport.Filter.periodInfo.Name = value.Name;

                    if (value.PeriodKind == 'CustomRange') {
                        if (scope.periodStart && scope.periodEnd) {
                            scope.dateFrom = CurrentReport.Filter.periodInfo.DateFrom = moment.utc(scope.periodStart, 'DD.MM.YYYY.').toDate();
                            scope.dateTo = CurrentReport.Filter.periodInfo.DateTo = moment.utc(scope.periodEnd, 'DD.MM.YYYY.').toDate();
                        } else {
                            scope.dateFrom = CurrentReport.Filter.periodInfo.DateFrom;
                            scope.dateTo = CurrentReport.Filter.periodInfo.DateTo;
                        }

                        unwatchDateTo = scope.$watch('dateTo', function(newDate, oldDate) {
                            if (newDate && newDate !== oldDate) {
                                CurrentReport.Filter.periodInfo.DateTo = newDate;
                                updateReportFilter();
                                scope.onChange();
                            }
                        });

                        unwatchDateFrom = scope.$watch('dateFrom', function(newDate, oldDate) {
                            if (newDate && newDate !== oldDate) {
                                CurrentReport.Filter.periodInfo.DateFrom = newDate;
                                updateReportFilter();
                                scope.onChange();
                            }
                        });

                        if (!init) {
                            openDateRangePicker();
                        }

                    } else {
                        if (unwatchDateFrom && unwatchDateTo) {
                            unwatchDateTo = null;
                            unwatchDateFrom = null;
                        }
                        closeDateRangePicker();
                    }
                }

                function setPeriodIfExists(value) {
                    if (value) {
                        var ddValue = _.find(scope.dropDownValues, function(dd) { return dd.PeriodKind === value.PeriodKind; });
                        if (ddValue) {
                            setPeriod(ddValue);
                            return true;
                        }
                    }
                    return false;
                }

                scope.dateOptions = {
                    formatYear: 'yy',
                    maxDate: new Date(),
                    startingDay: 1,
                    dp1Opened: false,
                    dp2Opened: false
                };

                scope.openDp1 = function() {
                    scope.dateOptions.dp1Opened = true;
                };
                scope.openDp2 = function() {
                    scope.dateOptions.dp2Opened = true;
                };

                scope.closeDatePicker = function() {
                    closeDateRangePicker();
                };

                scope.toggleDatePicker = function() {
                    scope.openDateRangePicker ? closeDateRangePicker() : openDateRangePicker();
                };

                function isLastDayOfMonth(dt) {
                    var test = new Date(dt.getTime());
                    test.setDate(test.getDate() + 1);
                    return test.getDate() === 1;
                }

                var outsideClickOn = false;
                function openDateRangePicker() {
                    scope.openDateRangePicker = true;
                    outsideClickOn = false;
                    $document.bind('click', clickedOutside);
                }
                function closeDateRangePicker() {
                    scope.openDateRangePicker = false;
                    $document.unbind('click', clickedOutside);
                }
                var clickedOutside = function(event) {
                    if (scope.openDateRangePicker == true && outsideClickOn == true) {
                        var isClickedElementChildOfPopup = element[0] === event.target || element[0].contains(event.target);

                        if (isClickedElementChildOfPopup) {
                            return;
                        }

                        scope.$apply(function() {
                            closeDateRangePicker();
                        });
                    }
                    outsideClickOn = true;
                };

                scope.$on('$destroy', function() {
                    $document.unbind('click', clickedOutside);
                });

            }

        };
    }]);


