using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
	public class City
	{
		public long osm_id;
		public decimal lat;
		public decimal lng;
		public string name;
		public string country_code;
		public string kind; // city (large) or town (smaller)
		public int? population;

		public void Save()
		{
			if (name == null)
				return;

			using (var db = Database.Get())
			using (var tran = db.BeginTransaction())
			{
				if (db.RecordExists(tran, "city", "osm_id", this.osm_id))
					Database.ExecuteNonQuery(db, tran, @"
UPDATE city
SET lat = @lat, lng = @lng, name = @name, country_code = @country_code, kind = @kind, population = @population
WHERE osm_id = @osm_id",
				"@lat", lat,
				"@lng", lng,
				"@name", name,
				"@country_code", country_code,
				"@kind", kind,
				"@population", population);
				else
					Database.Insert(db, tran, @"
INSERT INTO city(osm_id, lat, lng, name, country_code, kind, population)
VALUES (@osm_id, @lat, @lng, @name, @country_code, @kind, @population)",
				"@osm_id", osm_id,
				"@lat", lat,
				"@lng", lng,
				"@name", name,
				"@country_code", country_code,
				"@kind", kind,
				"@population", population);
				tran.Commit();
			}
		}
		public static List<City> GetAll()
		{
			return Database.ListFetcher("SELECT osm_id, lat, lng, name, country_code, kind, population FROM city",
				_FromDataReader);
		}
		public static List<City> GetByCountry(string countryCode)
		{
			return Database.ListFetcher("SELECT osm_id, lat, lng, name, country_code, kind, population FROM city WHERE country_code = @country_code",
				_FromDataReader,
				"@country_code", countryCode);
		}
		public static List<City> GetByChannel(Guid channelId)
		{
			return Database.ListFetcher(@"SELECT osm_id, lat, lng, name, country_code, kind, population FROM channel_city LEFT JOIN city ON channel_city.city_id = city.osm_id WHERE channel_city.channel_id = @channel_id",
				_FromDataReader,
				"@channel_id", channelId);
		}
		public static List<long> GetIdsByChannel(Guid channelId)
		{
			return Database.ListFetcher(@"SELECT city_id FROM channel_city WHERE channel_city.channel_id = @channel_id",
				dr => dr.GetInt64(0),
				"@channel_id", channelId);
		}
		public static List<Tuple<Guid, List<long>>> GetIdsByChannel()
		{
			return Database.ListFetcher(@"SELECT channel_id, city_id FROM channel_city",
				dr => new
				{
					ChannelId = dr.GetGuid(0),
					CityId = dr.GetInt64(1)
				})
				.GroupBy(cc => cc.ChannelId)
				.Select(g => new Tuple<Guid, List<long>>(g.Key, g.Select(cc => cc.CityId).ToList()))
				.ToList();
		}
		private static City _FromDataReader(MySqlDataReader dr)
		{
			return new City
			{
				osm_id = dr.GetInt64(0),
				lat = dr.GetDecimal(1),
				lng = dr.GetDecimal(2),
				name = dr.GetString(3),
				country_code = dr.GetString(4),
				kind = dr.GetString(5),
				population = dr.GetNullableInt(6)
			};
		}
		public static Dictionary<Guid, int> CountByChannel()
		{
			var countByChannel = new Dictionary<Guid, int>();
			Database.ListFetcher("SELECT channel_id, COUNT(*) FROM channel_city GROUP BY channel_id",
				(dr) => new
				{
					ChannelId = dr.GetGuid(0),
					Count = dr.GetInt32(1)
				}).ForEach(a => countByChannel[a.ChannelId] = a.Count);
			return countByChannel;
		}

		public static bool AddToChannel(Guid channelId, long cityId)
		{
			bool added = false;
			using (var db = Database.Get())
			using (var tran = db.BeginTransaction())
			{
				if (!Database.Exists(db, tran, "SELECT 1 FROM channel_city WHERE channel_id = @channel_id AND city_id = @city_id",
					"@channel_id", channelId,
					"@city_id", cityId))
				{
					Database.ExecuteNonQuery("INSERT INTO channel_city(channel_id, city_id) VALUES (@channel_id, @city_id)",
						"@channel_id", channelId,
						"@city_id", cityId);
					tran.Commit();
					added = true;
				}
			}
			return added;
		}
		public static bool RemoveFromChannel(Guid channelId, long cityId)
		{
			return Database.Delete("DELETE FROM channel_city WHERE channel_id = @channel_id AND city_id = @city_id",
				"@channel_id", channelId,
				"@city_id", cityId);
		}

		public static List<Guid> GetChannelIds(long cityId)
		{
			return Database.ListFetcher(@"SELECT channel_id FROM channel_city WHERE city_id = @city_id", dr => dr.GetGuid(0), "@city_id", cityId);
		}
	}
}