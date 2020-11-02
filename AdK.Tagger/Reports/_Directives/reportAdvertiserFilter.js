angular.module('app.reports')
  .directive('reportAdvertiserFilter', ['Service', function (Service) {
      return {
          restrict: 'AE',
          scope: {
              onChange: '='
          },
          templateUrl: 'Reports/_Directives/reportAdvertiserFilter.html',
          link: function (scope) {

              scope.selectedAdvertiser = null;
              var advertiserSelected = false;

              scope.clearAdvertiser = function () {
                  scope.selectedAdvertiser = null;
                  if (advertiserSelected) {
                      advertiserSelected = false;
                  }
                  onChange({
                      Name: null,
                      Id: null
                  });
              };

              scope.$watch('selectedAdvertiser', function (newVal) {
                  //This is the case when we reset advertiser we want to refresh the page with new results
                  if (!newVal && advertiserSelected) {
                      scope.clearAdvertiser();
                  }
              });

              scope.onSelected = function (adv) {
                  scope.selectedAdvertiser = adv;
                  advertiserSelected = true;
                  onChange(adv);
              };

              scope.getAdvertiserSuggestions = function (val) {
                  return Service('GetAdvertiserNamesByCriteria', { criteria: val }, { backgroundLoad: true })
            .then(function (response) { return response; });
              };

              scope.validateSelection = function (adv) {
                  if (scope.selectedAdvertiser != adv.srcElement.value) {
                      scope.selectedAdvertiser = adv.srcElement.value;
                  }
              };

              function onChange(adv) {
                  scope.onChange(adv);
              }

          }
      };
  } ]);


