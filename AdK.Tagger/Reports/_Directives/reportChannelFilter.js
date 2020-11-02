angular.module('app.reports')
  .directive('reportChannelFilter', ['CurrentReport', 'MyChannels', 'ReportFilterBase', function(CurrentReport, MyChannels, ReportFilterBase) {
    return {
      restrict: 'AE',
      scope: {
        onInit: '&',
        onChange: '&',
        ignoreIncludes: '@'
      },
      templateUrl: 'Reports/_Directives/reportDropDownTemplate.html',
      link: function(scope) {
        scope.filterName = 'Channel';
        var filterKey = 'channelFilterKey';

        if (scope.ignoreIncludes) {
          CurrentReport.Filter.include = {
            Id: 'None',
            Name: 'None'
          };
        }

        MyChannels.getChannels().then(function(channels) {
          scope.dropDownValues = channels;

          if (!channels || !channels.length) {
            CurrentReport.Filter.channel = {};
            scope.selectedChannel = {};
            scope.onInit({ channels: [] });
            return;
          } else {
            ReportFilterBase.getDefaultDropDownValue(filterKey, scope.dropDownValues, CurrentReport.Filter.channel.Id).then(function(value) {
              setValue(value);
              scope.onInit({ channels: [channels] });
            });
          }

        });

        scope.changeValue = function(value) {
          ReportFilterBase.onDropDownChange(filterKey, value, CurrentReport.Filter.channel.Id, function(value) {
            setValue(value);
            scope.onChange();
          });
        };

        function setValue(value) {
          scope.selectedValue = CurrentReport.Filter.channel = value;
        }

      }
    };
  }]);
