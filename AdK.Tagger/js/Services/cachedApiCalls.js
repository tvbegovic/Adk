angular.module('app')
.factory('CachedApiCalls', ['Service', function (Service) {

    var userMarketsCache;
    var categoriesCache;
    var industriesCache;
    var channelsCache;
    var advertisersCache;
    var brandsCache;
    var productsCache;

    return {
        getAllCategories: function () {
            if (!categoriesCache) {
                categoriesCache = Service('GetAllCategories', null, { backgroundLoad: true });
            }
            return categoriesCache;
        },
        getAllIndustries: function () {
            if (!industriesCache) {
                industriesCache = Service('GetAllIndustries', null, { backgroundLoad: true });
            }
            return industriesCache;
        },
        getAllChannels: function () {
            if (!channelsCache) {
                channelsCache = Service('GetChannels', null, { backgroundLoad: true });
            }
            return channelsCache;
        },
        getUserMarkets: function () {
            if (!userMarketsCache) {
                userMarketsCache = Service('GetUserMarkets', null, { backgroundLoad: true });
            }
            return userMarketsCache;
        },
        clearUserMarketsCache: function () {
            userMarketsCache = null;
        },
        getAllAdvertisers: function () {
            if (!advertisersCache) {
                advertisersCache = Service('GetAllAdvertisers', null, { backgroundLoad: true });
            }
            return advertisersCache;
        },
        getAllBrands: function () {
            if (!brandsCache) {
                brandsCache = Service('GetAllBrands', null, { backgroundLoad: true });
            }
            return brandsCache;
        },
        getAllProducts: function () {
            if (!productsCache) {
                productsCache = Service('GetProducts', null, { backgroundLoad: true });
            }
            return productsCache;
        }
    };

}]);
