angular.module('app')
	.controller('editFeedFilterGroupCtrl', ['$scope', 'Service', 'confirmPopup', 'focus', 'CachedApiCalls', '$attrs',
        function ($scope, Service, confirmPopup, focus, CachedApiCalls, $attrs) {



            $scope.filterGroup = {
                FeedFilterId: $scope.feedFilterId,
                Exclude: $scope.exclude,
                FeedFilterRulesMarkets: [],
                FeedFilterRulesDomains: [],
                FeedFilterRulesIndustries: [],
                FeedFilterRulesCategories: [],
                FeedFilterRulesAdvertisers: [],
                FeedFilterRulesBrands: []
            };


            function init() {
                if ($scope.filterGroupId && $scope.filterGroupId != undefined) {
                    Service('GetFilterGroup', { filterGroupId: $scope.filterGroupId }).then(function success(response) {
                        $scope.filterGroup = response;
                    }, function error(response) {
                        console.error('GetFilterGroup Error', response.status, response.data);
                    });
                }
            }

            //init();
            $attrs.$observe('filterGroupId', function (newValue) { init(); });

            $scope.saveFilterGroup = function () {
                Service('SaveFilterGroup', {
                    filterGroup: $scope.filterGroup
                }).then(function (response) {
                    $scope.isVisible = false;
                    $scope.onGroupSave();
                });
            }

            $scope.deleteFilterGroup = function () {
                confirmPopup.open('Delete filter group', null, 'Are you sure you want to delete this filter group?')
									.then(function () {
											return Service('DeleteFilterGroup', { filterGroupId: $scope.filterGroupId });
									}).then(function () {
											$scope.isVisible = false;
									});
            }

            $scope.cancelEditFilters = function () {
                $scope.isVisible = false;
            }


            //Autocompletes
            //Initialization
            $scope.keyChain = {
                domains: 'domains'
                , markets: 'markets'
                , advertisers: 'advertisers'
                , brands: 'brands'
                , industries: 'industries'
                , categories: 'categories'
            };

            var lastAjaxTerms = {};
            $scope.loadingSwitches = {};
            var itemArrays = {};

            for (var key in $scope.keyChain) {
                lastAjaxTerms[$scope.keyChain[key]] = { term: '' };
                $scope.loadingSwitches[$scope.keyChain[key]] = { loading: false };
                itemArrays[$scope.keyChain[key]] = [];
            }

            //Markets
            $scope.getMarketsAutocomplete = function (term) {
                return getAutoCompleteSuggestions(term, $scope.keyChain.markets);
            };
            //Domains
            $scope.getDomainsAutocomplete = function (term) {
                return getAutoCompleteSuggestions(term, $scope.keyChain.domains);
            };
            //Advertisers
            $scope.getAdvertisersAutocomplete = function (term) {
                return getFilteredCachedAutoCompleteSuggestions(term, $scope.keyChain.advertisers);
            };
            //Brands
            $scope.getBrandsAutocomplete = function (term) {
                return getFilteredCachedAutoCompleteSuggestions(term, $scope.keyChain.brands);
            };
            //Industries
            $scope.getIndustriesAutocomplete = function (term) {
                return getFilteredCachedAutoCompleteSuggestions(term, $scope.keyChain.industries);
            };
            //Categories
            $scope.getCategoriesAutocomplete = function (term) {
                return getFilteredCachedAutoCompleteSuggestions(term, $scope.keyChain.categories);
            };

            //Cache
            //  Industries
            CachedApiCalls.getAllIndustries().then(function (response) {
                var key = $scope.keyChain.industries;
                itemArrays[key] = [];
                for (var i = 0; i < response.length; i++)
                    itemArrays[key].push({ Id: response[i].Id, DisplayName: response[i].Name });
            });
            //  Categories
            CachedApiCalls.getAllCategories().then(function (response) {
                var key = $scope.keyChain.categories;
                itemArrays[key] = [];
                for (var i = 0; i < response.length; i++)
                    itemArrays[key].push({ Id: response[i].Id, DisplayName: response[i].Name });
            });
            //  Advertisers
            CachedApiCalls.getAllAdvertisers().then(function (response) {
                var key = $scope.keyChain.advertisers;
                itemArrays[key] = [];
                for (var i = 0; i < response.length; i++)
                    itemArrays[key].push({ Id: response[i].Id, DisplayName: response[i].Name });
            });
            //  Brands
            CachedApiCalls.getAllBrands().then(function (response) {
                var key = $scope.keyChain.brands;
                itemArrays[key] = [];
                for (var i = 0; i < response.length; i++)
                    itemArrays[key].push({ Id: response[i].Id, DisplayName: response[i].Name });
            });

            //Helper functions
            function getAutoCompleteSuggestions(term, key) {
                if (!lastAjaxTerms[key].term || term.indexOf(lastAjaxTerms[key].term.substring(0, 2)) !== 0) {
                    lastAjaxTerms[key].term = term.substring(0, 2);
                    $scope.loadingSwitches[key].loading = true;

                    return Service('GetAjaxSuggestion', {
                        term: term, forModel: key
                    }).then(function (response) {
                        itemArrays[key] = response || [];
                        return getFilteredAutoCompleteSuggestions(term, itemArrays[key]);
                    }).finally(function () {
                        $scope.loadingSwitches[key].loading = false;
                    });
                } else {
                    return getFilteredAutoCompleteSuggestions(term, itemArrays[key]);
                }
            }

            function getFilteredAutoCompleteSuggestions(term, itemArray) {
                var itemsToReturn = [];
                for (var i = 0; itemArray.length > i; i++) {
                    var item = itemArray[i];
                    if (item.DisplayName.toLowerCase().indexOf(term.toLowerCase()) !== -1) {
                        itemsToReturn.push(item);
                        if (itemsToReturn.length === 10) {
                            break;
                        }
                    }
                }
                return itemsToReturn;
            }

            function getFilteredCachedAutoCompleteSuggestions(term, key) {
                return itemArrays[key].filter(function (item) {
                    return item.DisplayName.toLowerCase().indexOf(term.toLowerCase()) !== -1;
                });
            }

        }]).directive('editFeedFilterGroup', [function () {
            return {
                restrict: 'E',
                scope: {
                    feedFilterId: '@',
                    filterGroupId: '@',
                    clientName: '@',
                    exclude: '@',

                    isVisible: '=',
                    onGroupSave: '='
                },
                templateUrl: '/Modules/Feeds/editFeedFilterGroup.html',
                controller: 'editFeedFilterGroupCtrl'
            };
        }]);
