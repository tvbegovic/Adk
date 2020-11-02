using DatabaseCommon;
using System;
using System.Collections.Generic;

namespace AdKontrol.Tagger.Model
{
	public class ChannelCity
	{
		public Guid ChannelId;
		public int CityId;

		public static List<City> GetByChannel(Guid channelId)
		{
			return Database.ListFetcher("SELECT city_idid, lat, lng, radius, address FROM channel_coordinate WHERE channel_id = @channel_id",
				(dr) => new ChannelCity
				{
					Id = dr.GetInt32(0),
					ChannelId = channelId,
					Lat = dr.GetDecimal(1),
					Lng = dr.GetDecimal(2),
					Radius = dr.GetDecimal(3),
					Address = dr.GetNullableString(4)
				},
				"@channel_id", channelId);
		}
		public static Dictionary<Guid, int> CountByChannel()
		{
			var countByChannel = new Dictionary<Guid, int>();
			Database.ListFetcher("SELECT channel_id, COUNT(*) FROM channel_coordinate GROUP BY channel_id",
				(dr) => new
				{
					ChannelId = dr.GetGuid(0),
					Count = dr.GetInt32(1)
				}).ForEach(a => countByChannel[a.ChannelId] = a.Count);
			return countByChannel;
		}
		public bool Update()
		{
			return Database.ExecuteNonQuery("UPDATE channel_coordinate SET lat = @lat, lng = @lng, radius = @radius, address = @address WHERE id = @id",
				"@id", Id,
				"@lat", Lat,
				"@lng", Lng,
				"@radius", Radius,
				"@address", Address) == 1;
		}
		public bool Insert()
		{
			Id = (int)Database.Insert("INSERT INTO channel_coordinate(channel_id, lat, lng, radius, address) VALUES (@channel_id, @lat, @lng, @radius, @address)",
				"@channel_id", ChannelId,
				"@lat", Lat,
				"@lng", Lng,
				"@radius", Radius,
				"@address", Address);
			return true;
		}
		public static bool Delete(int channelCoordinateId)
		{
			return Database.Delete("DELETE FROM channel_coordinate WHERE id = @id", "@id", channelCoordinateId);
		}
	}
}