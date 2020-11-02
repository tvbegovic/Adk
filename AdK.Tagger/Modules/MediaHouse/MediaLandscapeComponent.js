function MediaLandscapeComponentController($element, $filter, MediaHouses, confirmPopup, focus) {
	var ctrl = this;

	ctrl.current = {
		holding: null,
		newHoldingName: '',
		channel: null,
		group: null,
		editingChannel: false,
		editingGroup: false,
		editingHolding: false
	};
	ctrl.holdingClicked = selectHolding;
	function selectHolding(holding) {
		ctrl.current.holding = holding;
		ctrl.current.holdingGroups = _.reduce(MediaHouses.groupDic, function (list, group) {
			if (group.HoldingId === ctrl.current.holding.Id)
				list.push(group);
			return list;
		}, []);
		selectGroup(_.first(ctrl.current.holdingGroups));
	}
	function selectAnyHolding() {
		if (!_.isEmpty(MediaHouses.holdingDic)) {
			var holding = MediaHouses.holdingDic[_.first(Object.keys(MediaHouses.holdingDic))];
			selectHolding(holding);
		} else {
			ctrl.current.holding = null;
			//ctrl.current.holdingGroups = [];
		}
	}
	ctrl.editHolding = function () {
		ctrl.current.editingHolding = true;
		ctrl.current.newHoldingName = ctrl.current.holding.Name;
		focus('newHoldingName');
	};
	ctrl.addHolding = function () {
		ctrl.current.holding = null;
		ctrl.current.newHoldingName = '';
		focus('newHoldingName');
	};
	ctrl.saveHolding = function () {
		if (!ctrl.current.holding) {
			var holding = MediaHouses.newHolding();
			holding.Name = ctrl.current.newHoldingName;
			MediaHouses.createHolding(holding);
			selectHolding(holding);
		} else {
			ctrl.current.editingHolding = false;
			ctrl.current.holding.Name = ctrl.current.newHoldingName;
			MediaHouses.updateHolding(ctrl.current.holding);
		}
	};
	ctrl.deleteHolding = function () {
		confirmPopup.open("Delete Holding", null, "Do you want to delete the holding '" + ctrl.current.holding.Name + "' and depending groups?").then(function () {
			MediaHouses.deleteHolding(ctrl.current.holding).then(selectAnyHolding);
		});
	};
	ctrl.mineChanged = function () {
		if (ctrl.current.holding)
			_.forEach(MediaHouses.holdingDic, function (h) {
				if (h.Id !== ctrl.current.holding.Id)
					h.Mine = false;
			});
		MediaHouses.setMyHolding(ctrl.current.holding.Mine ? ctrl.current.holding : null);
	};

	ctrl.groupClicked = selectGroup;
	function selectGroup(group) {
		ctrl.current.group = group;
		MediaHouses.channels.forEach(function (channel) {
			channel.Selected = false;
		});
	}
	ctrl.editGroup = function () {
		ctrl.current.editingGroup = true;
	};
	ctrl.addGroup = function () {
		ctrl.current.group = null;
		ctrl.current.newGroupName = '';
		focus('newGroupName');
	};
	ctrl.saveGroup = function () {
		if (!ctrl.current.group) {
			var group = MediaHouses.newGroup();
			group.Name = ctrl.current.newGroupName;
			ctrl.current.newGroupName = '';
			group.HoldingId = ctrl.current.holding.Id;
			ctrl.current.holdingGroups.push(group);
			MediaHouses.createGroup(group);
			ctrl.current.group = group;
		} else {
			ctrl.current.editingGroup = false;
			MediaHouses.updateGroup(ctrl.current.group);
		}
	};
	ctrl.deleteGroup = function () {
		confirmPopup.open("Delete Group", null, "Do you want to delete the group '" + ctrl.current.group.Name + "'?").then(function () {
			MediaHouses.deleteGroup(ctrl.current.group).then(function () {
				_.remove(ctrl.current.holdingGroups, ctrl.current.group);
				ctrl.current.group = null;
			});
		});
	};

	ctrl.isChannelInGroup = function (channel) {
		return channel && _.includes(ctrl.current.group.ChannelIds, channel.Id);
	};
	ctrl.isChannelNotInGroup = function (channel) {
		return !ctrl.isChannelInGroup(channel);
	};
	ctrl.canAdd = function () {
		return _.some(MediaHouses.channels, function (channel) {
			return channel.Selected && ctrl.isChannelNotInGroup(channel);
		});
	};
	ctrl.canRemove = function () {
		return _.some(MediaHouses.channels, function (channel) {
			return channel.Selected && ctrl.isChannelInGroup(channel);
		});
	};
	ctrl.addChannels = function () {
		var filter = $filter('filter');
		filter(MediaHouses.channels, ctrl.current.channelFilter).forEach(function (channel) {
			if (channel.Selected) {
				ctrl.current.group.ChannelIds.push(channel.Id);
				channel.Selected = false;
			}
		});
		ctrl.saveGroup();
	};
	ctrl.removeChannels = function () {
		MediaHouses.channels.forEach(function (channel) {
			if (channel.Selected) {
				_.remove(ctrl.current.group.ChannelIds, function (id) { return id === channel.Id; });
				channel.Selected = false;
			}
		});
		ctrl.saveGroup();
	};

	//ctrl.$on('escaped', function () {
	//	ctrl.$apply(function () {
	//		if (!ctrl.current.holding)
	//			selectAnyHolding();
	//	});
	//});

	MediaHouses.ready.then(function () {
		ctrl.mediaHouses = MediaHouses;

		selectAnyHolding();
		if (!ctrl.current.holding)
			ctrl.addHolding();
	});
	
}

angular.module('app')
	.component('mediaLandscape',
		{
			templateUrl: '/Modules/MediaHouse/MediaLandscapeComponent.html',
			bindings: {
				
			},
			controller: ['$element', '$filter', 'MediaHouses', 'confirmPopup', 'focus', MediaLandscapeComponentController]
		});
