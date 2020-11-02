angular.module('app')
  .factory('Paged', function() {
    var LOADING = 1;
    var LOADED = 2;
    return function(pageSize, loadPage) {
      var self = this;
      this.totalCount = null;
      var pages = null;
      this.items = [];
      this.need = function(index) {
        var neededPage = Math.floor(index / pageSize);
        if (!pages) {
          pages = [];
          pages.length = neededPage + 1;
        }
        if (!pages[neededPage]) {
          pages[neededPage] = LOADING;
          loadPage(neededPage).then(function(res) {
            if (self.totalCount === null) {
              self.totalCount = res.TotalCount;
              pages.length = Math.ceil(self.totalCount / pageSize);
              for (var i = 0; i < self.totalCount; ++i) {
                self.items[i] = { index: i };
              }
            }
            pages[neededPage] = LOADED;
            for (var i = 0; i < res.Items.length; ++i) {
              var index = neededPage * pageSize + i;
              res.Items[i].index = index;
              self.items[index] = res.Items[i];
            }
            //for (var i = res.Items.length; i < pageSize; ++i)
            //	self.items[neededPage * pageSize + i] = {};
          });
        }
      };
    };
  });
