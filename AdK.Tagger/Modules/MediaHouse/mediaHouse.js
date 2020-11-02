angular.module('app')
  .factory('MediaHouses', ['Service', 'Authenticate', function(Service, Authenticate) {
    var _m = {
      getAll: function() {
        return Service('GetMediaHouses').then(function(data) {
          _m.channels = data.Channels;
          _m.groups = data.Groups;
          _m.holdings = data.Holdings;

          _m.groupDic = data.Groups.reduce(function(dic, group) {
            group.ChannelIds.forEach(function(channelId) {
              var mediaHouse = _.find(_m.channels, 'Id', channelId);
              mediaHouse.GroupId = group.Id;
            });
            dic[group.Id] = group;
            return dic;
          }, {});
          _m.holdingDic = data.Holdings.reduce(function(dic, holding) {
            dic[holding.Id] = holding;
            return dic;
          }, {});
        });
      },
      getChannels: function() {
        return Service('GetChannels');
      },
      getMyChannels: function() {
        return Service('GetMyChannels');
      },
      newGroup: function() {
        var g = {
          Name: 'New Group',
          UserId: Authenticate.user.id,
          ChannelIds: []
        };
        _m.groupDic[0] = g;
        return g;
      },
      createGroup: function(group) {
        return Service('CreateGroup', { group: group }).then(function(dbGroup) {
          group.Id = dbGroup.Id;
          delete _m.groupDic[0];
          _m.groupDic[group.Id] = group;
        });
      },
      updateGroup: function(group) {
        return Service('UpdateGroup', { group: group }, { backgroundLoad: true });
      },
      deleteGroup: function(group) {
        return Service('DeleteGroup', { group: group }).then(function() {
          delete _m.groupDic[group.Id];
        });
      },
      newHolding: function() {
        var h = {
          Name: 'New Holding',
          UserId: Authenticate.user.id
        };
        return h;
      },
      createHolding: function(holding) {
        return Service('CreateHolding', { holding: holding }).then(function(dbHolding) {
          holding.Id = dbHolding.Id;
          _m.holdingDic[holding.Id] = holding;
        });
      },
      updateHolding: function(holding) {
        return Service('UpdateHolding', { holding: holding }, { backgroundLoad: true });
      },
      deleteHolding: function(holding) {
        return Service('DeleteHolding', { holding: holding }).then(function() {
          delete _m.holdingDic[holding.Id];
        });
      },
      setMyHolding: function(holding) {
        return Service('SetMyHolding', { holdingId: holding ? holding.Id : null }, { backgroundLoad: true });
      },
      getCompetitors: function() {
        return Service('GetCompetitors');
      },
      addCompetitor: function(competitor) {
        return Service('AddCompetitor', { competitor: competitor }, { backgroundLoad: true });
      },
      removeCompetitor: function(competitor) {
        return Service('RemoveCompetitor', { competitor: competitor }, { backgroundLoad: true });
      }
    };
    _m.ready = _m.getAll();
    return _m;
  }]);
