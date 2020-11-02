angular.module('app.reports')
  .directive('reportCategoryFilter', ['CurrentReport', 'CachedApiCalls', '$timeout', function(CurrentReport, CachedApiCalls, $timeout) {
    return {
      restrict: 'AE',
      scope: {
        selectedIndustry: '=',
        onChange: '&'
      },
      templateUrl: 'Reports/_Directives/reportTagsInputFilterTemplate.html',
      link: function(scope) {
        scope.filterName = 'Categories';
        scope.tagsPlaceholder = 'Add a category';
        var allCategories = [];
        var categoriesForSelectedIndustry = [];
        CurrentReport.Filter.categories = [];

        //Get all categories form DB and cache it for latter use
        CachedApiCalls.getAllCategories().then(function(response) {
          allCategories = response;
          setIndustryCategoriers();
        });

        scope.$watch('selectedIndustry', function(newValue, oldValue) {
          if (newValue !== oldValue) {
            //clear category tags selection on industry change
            scope.selectedTags = [];
            CurrentReport.Filter.categories = [];
            setIndustryCategoriers();
          }
        });

        function setIndustryCategoriers() {
          if (scope.selectedIndustry && scope.selectedIndustry.toLowerCase() !== 'all') {
            categoriesForSelectedIndustry = allCategories.filter(function(ac) { return ac.IndustryId === scope.selectedIndustry; });
          } else {
            categoriesForSelectedIndustry = allCategories;
          }
        }

        scope.getTagsAutocomplete = function(query) {
          return categoriesForSelectedIndustry.filter(function(ic) {
            return ic.Name.toLowerCase().indexOf(query.toLowerCase()) !== -1;
          });
        };

        scope.onTagAdded = onCategoryChange;
        scope.onTagRemoved = onCategoryChange;

        function onCategoryChange() {

          //$timeout is used to fix issue with undefined scope.selectedTags on first category adding
          $timeout(function() {
            if (scope.selectedTags && scope.selectedTags.length) {
              CurrentReport.Filter.categories = scope.selectedTags.map(function(st) { return st.Id; });
              scope.onChange();
            } else if (CurrentReport.Filter.categories && CurrentReport.Filter.categories.length) {
              CurrentReport.Filter.categories = [];
              scope.onChange();
            }

          }, 5);
        }

      }
    };

  }]);
