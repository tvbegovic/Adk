angular.module('app')
  .service('Brandvertiser', ['Service', function(Service) {
    var _b = {
      pageSize: 100,
      getPage: function(brands, advertisers, pageNum, filter) {
        return Service('GetBrandvertisers', {
          brands: brands,
          advertisers: advertisers,
          filter: filter,
          pageNum: pageNum,
          pageSize: _b.pageSize
        }, { backgroundLoad: true });
      },
      getKeyAccounts: function() {
        return Service('GetKeyAccounts');
      },
      addKeyAccount: function(brandvertiser) {
        return Service('AddKeyAccount', { brandvertiser: brandvertiser }, { backgroundLoad: true });
      },
      removeKeyAccount: function(brandvertiser) {
        return Service('RemoveKeyAccount', { brandvertiser: brandvertiser }, { backgroundLoad: true });
      }
    };
    return _b;
  }])
