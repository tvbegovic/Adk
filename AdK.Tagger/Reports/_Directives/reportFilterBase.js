angular.module('app')
  .factory('ReportFilterBase', ['$q', 'UserSettings', function($q, UserSettings) {

    function getValue(value, ddValues) {
      if (value) {
        //!Important do not change == to strict === as there i cases when we comapre string id with int id.
        var ddValue = _.find(ddValues, function(dd) { return dd.Id == value; });
        return ddValue;
      }
      return null;
    }

    return {
      getDefaultDropDownValue: function(filterKey, ddValues, currentReportFilterId) {
        var deferred = $q.defer();

        UserSettings.getReportFilters(filterKey).then(function(lastFilterValue) {

          var defaultValue = getValue(currentReportFilterId, ddValues) ||
            getValue(lastFilterValue ? lastFilterValue.Value : null, ddValues) ||
            ddValues[0];

          deferred.resolve(defaultValue);

        }).catch(deferred.reject);

        return deferred.promise;

      },
      onDropDownChange: function(filterKey, value, currentReportFilterId, setValueCallback) {
        if (value.Id !== currentReportFilterId) {
          setValueCallback(value);
          UserSettings.updateReportFilter(filterKey, value.Id);
        }
      }
    };

  }]);
