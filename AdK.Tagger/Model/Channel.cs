using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
	public class Channel
	{
		public Guid Id;
		public string Name;
		public string City;
		public string Country;
		public string MediaType;
		public decimal MatchThreshold;
		public string ArchivePath;
		public string ExternalId;
		public int? SubscribedDaysBack;

		public static List<Channel> GetAll()
		{
			return Database.ListFetcher<Channel>(
				@"SELECT id, station_name, city, country, media_type, match_threshold, remote_archive_path, external_id FROM channels WHERE station_name NOT LIKE '* %' ORDER BY station_name",
				dr => new Channel
				{
					Id = dr.GetGuid(0),
					Name = dr.GetString(1).Trim(), // Some names starting with a space mess up sorting
					City = dr.GetNullableString(2),
					Country = dr.GetNullableString(3),
					MediaType = dr.GetNullableString(4),
					MatchThreshold = dr.GetDecimalOrDefault( 5 ) * 100,
					ArchivePath = dr.GetNullableString(6),
					ExternalId = dr.GetNullableString(7)
				}
			);
		}

		public static List<Guid> GetIdsByUser(string userId)
		{
			var holding = Holding.GetMine(userId);
			if (holding != null)
				return holding.GetGroups().SelectMany(g => g.ChannelIds).ToList();

			return new List<Guid>();
		}
		public static List<Channel> GetByUser(string userId)
		{
			var channelIds = GetIdsByUser(userId);
			return Get(channelIds);
		}
		public static List<Channel> Get(IEnumerable<Guid> channelIds)
		{
			if (channelIds.Any())
				return Database.ListFetcher<Channel>(
					@"SELECT id, station_name, city, country, media_type, match_threshold FROM channels WHERE id " + Database.InClause(channelIds),
					dr => new Channel
					{
						Id = dr.GetGuid(0),
						Name = dr.GetString(1),
						City = dr.GetNullableString(2),
						Country = dr.GetNullableString(3),
						MediaType = dr.GetNullableString(4),
						MatchThreshold = dr.GetDecimalOrDefault(5) * 100,
					}
				);

			return new List<Channel>();
		}

		public static List<Channel> GetSubscribed(string userId)
		{
			return Database.ListFetcher(
				@"SELECT channels.id, channels.station_name, channels.city,channels.country, channels.media_type, channels.match_threshold, channels.remote_archive_path,
				channels.external_id, channels_vp_subs.days_back FROM channels
				 INNER JOIN channels_vp_subs ON channels.id = channels_vp_subs.channel_id 
				 WHERE station_name NOT LIKE '* %' AND channels_vp_subs.user_id = @user_id ORDER BY station_name",
				dr => new Channel
				{
					Id = dr.GetGuid(0),
					Name = dr.GetString(1).Trim(), // Some names starting with a space mess up sorting
					City = dr.GetNullableString(2),
					Country = dr.GetNullableString(3),
					MediaType = dr.GetNullableString(4),
					MatchThreshold = dr.GetDecimalOrDefault(5) * 100,
					ArchivePath = dr.GetNullableString(6),
					ExternalId = dr.GetNullableString(7),
					SubscribedDaysBack = dr.GetIntOrNull(8)
				},
				"@user_id", userId
			);
		}

	}
}
