angular.module('app')
  .controller('marketsCtrl', ['$scope', '$modal', '$timeout', 'Service', 'CachedApiCalls',
    function($scope, $modal, $timeout, Service, CachedApiCalls) {

      var noBackgroundLoad = { backgroundLoad: true };
      $scope.selected = false;
      var visibleMarketsCache;
      var marketChannelsGroup;
      $scope.newMarket = { name: '' };
      $scope.filter = {
        channelName: ''
      };

      /*************************************************
       * HACK: make channel column fixed when scrooling
      ***************************************************/
      var $controlColumn = document.getElementById('control-column');
      document.getElementById('tables-wrapper').addEventListener('scroll', function() {
        var leftScroll = document.getElementById('tables-wrapper').scrollLeft + 'px';
        var $channelColumns = document.querySelectorAll('.channel.fixed');
        $controlColumn.style.left = leftScroll;
        for (var i = 0; i < $channelColumns.length; i++) {
          $channelColumns[i].style.left = leftScroll;
        }
      });


      //Assume worst case tht we will change something in markets
      CachedApiCalls.clearUserMarketsCache();

      CachedApiCalls.getAllChannels().then(function(channels) {
        $scope.channels = channels;
      });

      loadMarkets();
      loadMarketChannels();

      $scope.addMarket = function() {
        if ($scope.newMarket.name) {
          var request = {
            name: $scope.newMarket.name.trim()
          };

          $scope.newMarket.name = '';
          Service('AddMarket', request, noBackgroundLoad).then(function() {
            loadMarkets(noBackgroundLoad);
          });
        }
      };

      $scope.deleteMarket = function(marketId) {
        openConfirmationModal(marketId).then(function() {
          var request = { id: marketId };
          Service('DeleteMarket', request, noBackgroundLoad).then(function() {
            loadMarkets(noBackgroundLoad);
          });
        });
      };

      $scope.setEditMode = function(market) {
        //ignore clicking on item when already in edit mode
        if (market._editMode) {
          return;
        }

        $scope.closeMarketEditMode();
        market._editMode = true;
        market._newName = market.Name;
        //element is disabled wait for element to become enabled
        $timeout(function() {
          document.getElementById('market-input-' + market.Id).select();
        }, 10);
      };

      $scope.closeMarketEditMode = function() {
        $scope.markets.forEach(function(market) {
          market._editMode = false;
        });
      };

      $scope.updateMarket = function(market) {
        if (market.Name !== market._originalName) {
          market._originalName = market.Name;
          updateMarket(market.Id, market.Name);
        }
        market._editMode = false;
      };

      /**
       * HACK: there is issue when ng-keydown is not getting escape btn
       * this is way to handle escape key with input ng-blur
       ***/
      $scope.cancelMarketEdit = function(market) {
        //ng-blur can be called with escape key and with valid update
        //we need timout to ensure that update will ocure first before canceling
        $timeout(function() {
          market.Name = market._originalName;
          market._editMode = false;
        }, 250);
      };


      $scope.clearNewMarketInput = function() {
        $scope.newMarket.name = '';
      };

      $scope.toggleChannelMarket = function(channelId, marketId) {

        var request = {
          channelId: channelId,
          marketId: marketId,
          active: !isChannelInMarket(marketId, channelId)
        };

        //Optimisticli update UI before executing request. On exception update state to one in DB
        if (request.active) {
          //add channel to market
          if (!marketChannelsGroup[marketId]) {
            marketChannelsGroup[marketId] = {};
          }
          marketChannelsGroup[marketId][channelId] = channelId;
        } else {
          //remove channel from market
          delete marketChannelsGroup[marketId][channelId];
        }

        toggleMarketChannel(request);

      };

      $scope.isChannelInMarket = isChannelInMarket;
      function isChannelInMarket(marketId, channelId) {
        if (marketId && channelId && marketChannelsGroup) {
          var marketGroup = marketChannelsGroup[marketId];
          return marketGroup ? Boolean(marketGroup[channelId]) : false;
        }

        return false;
      }

      var toggleMarketChannel = _.debounce(function(request) {
        Service('ToggleMarketChannel', request, noBackgroundLoad).catch(function() {
          loadMarketChannels();
        });
      }, 200);

      var updateMarket = _.debounce(function(marketId, newName) {
        var request = {
          marketId: marketId,
          name: newName
        };

        Service('UpdateMarket', request, noBackgroundLoad)
          .catch(function() {
            //refresh state only on exception else UI will be automaticly updated
            loadMarkets();
          });
      });

      function openConfirmationModal(marketId) {
        var market = findMarket(marketId);

        return $modal.open({
          templateUrl: 'marketDeleteConfirmationModal.html',
          controller: ['$scope', '$modalInstance', function($scope, $modalInstance) {
            $scope.market = market;
            $scope.ok = $modalInstance.close;
            $scope.cancel = function() { $modalInstance.dismiss('cancel'); };
          }]
        }).result;

      }

      $scope.getVisibleMarkets = function() {
        if (!visibleMarketsCache) {
          if ($scope.markets) {
            visibleMarketsCache = $scope.markets.filter(function(market) { return !market._isHidden; });
          }
        }
        return visibleMarketsCache;
      };

      $scope.haveFilteredMarkets = function() {
        return visibleMarketsCache && visibleMarketsCache.length < $scope.markets.length;
      };

      $scope.openMarketsFilterModal = function() {
        var markets = $scope.markets;
        $modal.open({
          templateUrl: 'marketTogleMarketsVisibility.html',
          controller: ['$scope', '$modalInstance', function($scope, $modalInstance) {
            $scope.markets = markets;
            $scope.done = function() { $modalInstance.close($scope.markets); };
            $scope.toggleMarketVisibility = function(market) {
              market._isHidden = !market._isHidden;
              resetVisibleMarketsCache();
            };
          }],
          size: 'sm'
        });

      };

      function findMarket(marketId) {
        return _.find($scope.markets, function(market) {
          return market.Id = marketId;
        });
      }

      function loadMarkets(options) {
        Service('GetUserMarkets', null, options).then(function(markets) {
          if (!markets || !markets.length) { $scope.displayNoMarktesMessage = true; }
          else { $scope.displayNoMarktesMessage = false; }
          (markets || []).forEach(function(market) {
            market._originalName = market.Name;
          });
          $scope.markets = markets;
          resetVisibleMarketsCache();
        }).catch(function() {

        });
      }

      function resetVisibleMarketsCache() {
        visibleMarketsCache = null;
      }

      function loadMarketChannels() {
        Service('GetUserMarketChannelsGroup').then(function(response) {
          //Create dictionary for fast access
          marketChannelsGroup = {};
          response.forEach(function(marketGroup) {
            marketChannelsGroup[marketGroup.MarketId] = marketGroup.MarketChannels;
          });

        });
      }

    }]);
