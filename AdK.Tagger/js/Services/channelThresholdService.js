angular.module('app')
	.factory('channelThresholdService', ['Service', function (Service) {

		return {
			getForAudit: function (auditId, auditChannels) {
				return Service('GetAuditChannelsThreshold', { auditId: auditId }, { backgroundLoad: true }).then(function (auditChannelsThresholds) {
					//change default auditChannel Threshold with user defined
					_.each(auditChannelsThresholds, function (ac) {
						var channel = _.find(auditChannels, function (c) { return c.Id === ac.ChannelId; });

						//swap default threshold with user defined
						if (channel) {
							channel.MatchThreshold = ac.Threshold;
						}
					});

					// threshold is percentage in zero based move it to 100 base percent
					_.each(auditChannels, function (c) {

						if (c.MatchThreshold <= 1) {
							c.MatchThreshold = Math.round(c.MatchThreshold * 100);
						}

					});

					return auditChannels;

				});
			}
		};

	}]);
