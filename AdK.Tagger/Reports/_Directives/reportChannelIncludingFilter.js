angular.module('app.reports')
    .directive('reportChannelIncludingFilter', ['CurrentReport', 'MyChannels', 'UserSettings', function(CurrentReport, MyChannels, UserSettings) {
        return {
            restrict: 'AE',
            scope: {
                onInit: '&',
                onChange: '&'
            },
            templateUrl: 'Reports/_Directives/reportChannelIncludingFilter.html',
            link: function(scope) {
                var channelFilterKey = 'channelFilterKey';
                var includeFilterKey = 'includeFilterKey';

                UserSettings.getReportFilters().then(function(reportFilters) {
                    reportFilters = reportFilters || [];
                    var channelFilter = _.find(reportFilters, function(rf) { return rf.Key === channelFilterKey; });
                    var includeFilter = _.find(reportFilters, function(rf) { return rf.Key === includeFilterKey; });

                    //Including groups
                    scope.includeValues = [
                        { Id: 'GroupProperties', Name: 'Same Group' },
                        { Id: 'Competitors', Name: 'Competitors' }
                    ];

                    setIncludeValueIfExists(CurrentReport.Filter.include.Id) ||
                        setIncludeValueIfExists(includeFilter ? includeFilter.Value : null) ||
                        setIncludeValue(scope.includeValues[0]);

                    scope.changeIncludeValue = function(include) {
                        if (include.Id !== CurrentReport.Filter.include.Id) {
                            UserSettings.updateReportFilter(includeFilterKey, include.Id);
                            setIncludeValue(include);
                            scope.onChange();
                        }
                    };
                    function setIncludeValue(value) {
                        scope.selectedIncludeValue = CurrentReport.Filter.include = value;
                    }

                    function setIncludeValueIfExists(value) {
                        if (value) {
                            var ddValue = _.find(scope.includeValues, function(inc) { return inc.Id === value; });
                            if (ddValue) {
                                setIncludeValue(ddValue);
                                return true;
                            }
                        }
                        return false;
                    }

                    //Channel
                    scope.changeChannel = function(channel) {
                        if (channel.Id !== CurrentReport.Filter.channel.Id) {
                            UserSettings.updateReportFilter(channelFilterKey, channel.Id);
                            setChannelValue(channel);
                            scope.onChange();
                        }
                    };

                    function setChannelValue(value) {
                        scope.selectedChannel = CurrentReport.Filter.channel = value;
                    }

                    function setChannelValueIfExists(value) {
                        if (value) {
                            var ddValue = _.find(scope.channels, function(chn) { return chn.Id === value; });
                            if (ddValue) {
                                setChannelValue(ddValue);
                                return true;
                            }
                        }
                        return false;
                    }

                    MyChannels.getChannels().then(function(channels) {
                        scope.channels = channels;

                        if (!channels || !channels.length) {
                            CurrentReport.Filter.channel = {};
                            scope.selectedChannel = {};
                            scope.onInit({ channels: [] });
                            return;
                        }

                        setChannelValueIfExists(CurrentReport.Filter.channel.Id) ||
                            setChannelValueIfExists(channelFilter ? channelFilter.Value : null) ||
                            setChannelValue(scope.channels ? scope.channels[0] : null);

                        scope.onInit({ channels: [channels], include: CurrentReport.Filter.include });

                    });
                });


            }
        };
    }]);


