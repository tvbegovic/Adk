angular.module('app.reports')
  .directive('reportBrandFilter', ['Service', function (Service) {
      return {
          restrict: 'AE',
          scope: {
              onChange: '='
          },
          templateUrl: 'Reports/_Directives/reportBrandFilter.html',
          link: function (scope) {

              scope.selectedBrand = null;
              var brandSelected = false;

              scope.clearBrand = function () {
                  scope.selectedBrand = null;
                  if (brandSelected) {
                      brandSelected = false;
                  }
                  onChange({
                    Name: null,
                    Id: null
                  });
              };

              scope.$watch('selectedBrand', function (newVal) {
                  //This is the case when we reset advertiser we want to refresh the page with new results
                  if (!newVal && brandSelected) {
                      scope.clearBrand();
                  }
              });

              scope.onSelected = function (br) {
                  scope.selectedBrand = br;
                  brandSelected = true;
                  onChange(br);
              };

              scope.getBrandSuggestions = function (val) {
                  return Service('GetBrandNamesByCriteria', { criteria: val }, { backgroundLoad: true })
            .then(function (response) { return response; });
              };

              scope.validateSelection = function (br) {
                  if (scope.selectedBrand != br.srcElement.value) {
                      scope.selectedBrand = br.srcElement.value;
                  }
              };

              function onChange(br) {
                  scope.onChange(br);
              }

          }
      };
  } ]);


