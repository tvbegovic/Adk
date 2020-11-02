angular.module('app')
  .service('Pager', ['UserSettings', function(UserSettings) {
    var settingModule = 'Pagination';
    var settingKey = 'ItemsPerPage';
    var _currentSize = 10;
    var _init = false;

    function Pager() {
      var _pager = this;
      this.index = 1;
      this.size = _currentSize;
      this.sizes = [10, 15, 20, 30, 50];
      this.itemCount = 0;
      this.skip = 0;
      this.maxIndex = 1;

      if (!_init) {
        UserSettings.getSettings(settingModule, settingKey).then(function(itemsPerPage) {
          if (itemsPerPage && itemsPerPage.Value) {
            var savedSize = parseInt(itemsPerPage.Value);
            if (!isNaN(savedSize) && savedSize != _pager.size) {
              _pager.size = _currentSize = savedSize;
              _init = true;
            }
          }
        });
      }
    }

    Pager.prototype.reset = function() {
      this.index = 1;
    };

    Pager.prototype.previous = function() {
      if (this.index > 1) {
        --this.index;
        this.update();
      }
    };

    Pager.prototype.next = function() {
      if (this.index < this.maxIndex) {
        ++this.index;
        this.update();
      }
    };

    Pager.prototype.update = function() {
      // undefined or 0 caused by data binding
      if (!this.index) {
        this.index = 1;
      }

      if (this.size != _currentSize) {
        _currentSize = this.size;
        UserSettings.updateSettings(settingModule, settingKey, this.size);
      }

      this.maxIndex = Math.max(Math.ceil(this.itemCount / this.size), 1);
      this.index = Math.max(Math.min(this.index, this.maxIndex), 1); // verify index in [1..maxIndex]
      this.skip = (this.index - 1) * this.size;
    };

    Pager.prototype.setItemCount = function(itemCount) {
      this.itemCount = itemCount;
      this.update();
    };

    return Pager;

  }])
  .directive('tablePager', [function() {
    return {
      restrict: 'E',
      templateUrl: '/Reports/pager.html',
      scope: {
        pager: '=manager'
      }
    };
  }]);
