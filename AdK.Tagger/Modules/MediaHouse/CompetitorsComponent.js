function CompetitorsComponentController($element, $q, MediaHouses, fs) {
	var ctrl = this;

	ctrl.current = {
		channel: null,
		competitors: null
	};
	$q.all({
		channels: MediaHouses.getChannels(),
		myChannels: MediaHouses.getMyChannels(),
		competitors: MediaHouses.getCompetitors()
	}).then(function (res) {
		ctrl.channels = res.channels;
		ctrl.myChannels = res.myChannels;
		ctrl.competitors = res.competitors;

		var channelGrid = new fs.Grid(res.channels);
		new fs.Column(channelGrid, 'Channel Name', 'Name', new fs.FilterSubstring());
		new fs.Column(channelGrid, 'City', 'City', new fs.FilterFromData(res.channels, 'City'));
		new fs.Column(channelGrid, 'Country', 'Country', new fs.FilterFromData(res.channels, 'Country'));
		new fs.Column(channelGrid, 'Media', 'MediaType', new fs.FilterFromData(res.channels, 'MediaType'));
		ctrl.channelGrid = channelGrid;

		ctrl.orderByChannelGrid = function (channel) {
			return ctrl.channelGrid.orderKey(channel);
		};

		var competitorGrid = new fs.Grid(res.channels);
		new fs.Column(competitorGrid, 'Channel Name', 'Name', new fs.FilterSubstring());
		new fs.Column(competitorGrid, 'City', 'City', new fs.FilterFromData(res.channels, 'City'));
		new fs.Column(competitorGrid, 'Country', 'Country', new fs.FilterFromData(res.channels, 'Country'));
		new fs.Column(competitorGrid, 'Media', 'MediaType', new fs.FilterFromData(res.channels, 'MediaType'));
		ctrl.competitorGrid = competitorGrid;

		ctrl.orderByCompetitorGrid = function (channel) {
			return ctrl.competitorGrid.orderKey(channel);
		};

		if (res.myChannels.length)
			ctrl.myChannelChanged(res.myChannels[0]);
	});

	ctrl.myChannelChanged = function (channel) {
		ctrl.current.channel = channel;
	};
	ctrl.addCompetitor = function () {
		var competitor = {
			MyChannelId: ctrl.current.channel.Id,
			OtherChannelId: ctrl.current.otherChannel.Id
		};
		ctrl.competitors.push(competitor);
		MediaHouses.addCompetitor(competitor);
		ctrl.current.otherChannel = null;
	};
	ctrl.removeCompetitor = function () {
		var competitor = {
			MyChannelId: ctrl.current.channel.Id,
			OtherChannelId: ctrl.current.competitor.Id
		};
		_.remove(ctrl.competitors, function (competitor) { return isCompetitorChannel(competitor, ctrl.current.competitor); });
		MediaHouses.removeCompetitor(competitor);
		ctrl.current.competitor = null;
	};
	function isCompetitorChannel(competitor, channel) {
		var is =
			competitor.MyChannelId === ctrl.current.channel.Id &&
			competitor.OtherChannelId === channel.Id;
		return is;
	}
	ctrl.isCompetitor = function (channel) {
		if (!ctrl.current.channel || channel.Id === ctrl.current.channel.Id)
			return false;
		var match = function (competitor) {
			return isCompetitorChannel(competitor, channel);
		};
		return _.some(ctrl.competitors, match);
	};
	ctrl.isntCompetitor = function (channel) {
		if (!ctrl.current.channel || channel.Id === ctrl.current.channel.Id)
			return false;
		return !ctrl.isCompetitor(channel);
	};
}


angular.module('app')
	.component('competitors',
		{
			templateUrl: '/Modules/MediaHouse/CompetitorsComponent.html',
			bindings: {

			},
			controller: ['$element', '$q', 'MediaHouses', 'fs', CompetitorsComponentController]
		});
