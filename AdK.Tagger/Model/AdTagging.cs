using DatabaseCommon;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class AdTagging
	{
		public int Id;
		public Guid SongId;
		public string PksId;
		public decimal Duration;
		public string UserId;
		public byte SchemaVersion;
		public AdEntryStatus Status;
		public string TaggingIssue;
		public OneAd OneAd;

		public AdTagging() { }
		public AdTagging(int id, Guid songId, string pksId, decimal duration, string adTitle, string userId, byte schemaVersion, AdEntryStatus status, string taggingIssue, string oneAdXml)
		{
			Id = id;
			SongId = songId;
			PksId = pksId;
			Duration = duration;
			UserId = userId;
			SchemaVersion = schemaVersion;
			Status = status;
			TaggingIssue = taggingIssue;
			OneAd = GetOneAd(schemaVersion, oneAdXml);
			if (adTitle != null)
				OneAd.AdTitle = adTitle;
		}

		public static OneAd GetOneAd(byte schemaVersion, string xml)
		{
			switch (schemaVersion)
			{
				case OneAd.SchemaVersion:
					return OneAd.Deserialize(xml);
				default:
					return null;
			}
		}

		public void Save()
		{
			if (Id == 0)
				Insert();
			else
				Update();
		}
		public void Insert()
		{
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = @"INSERT INTO ad_tagging (song_id, user_id, schema_version, status, tagging_issue, one_ad_xml) VALUES (@song_id, @user_id, @schema_version, @status, @tagging_issue, @one_ad_xml)";
				command.Parameters.AddWithValue("@song_id", SongId.ToString());
				command.Parameters.AddWithValue("@user_id", UserId);
				command.Parameters.AddWithValue("@schema_version", OneAd.SchemaVersion);
				command.Parameters.AddWithValue("@status", (byte)Status);
				command.Parameters.AddWithValue("@tagging_issue", TaggingIssue);
				command.Parameters.AddWithValue("@one_ad_xml", OneAd.Serialize());

				command.ExecuteNonQuery();

				Id = (int)command.LastInsertedId;
			}
		}
		public void Update()
		{
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = @"UPDATE ad_tagging SET schema_version = @schema_version, status = @status, tagging_issue = @tagging_issue, @one_ad_xml WHERE id = @id";
				command.Parameters.AddWithValue("@schema_version", OneAd.SchemaVersion);
				command.Parameters.AddWithValue("@status", (byte)Status);
				command.Parameters.AddWithValue("@taggingIssue", TaggingIssue);
				command.Parameters.AddWithValue("@one_ad_xml", OneAd.Serialize());
				command.Parameters.AddWithValue("@id", Id);

				command.ExecuteNonQuery();
			}
		}

		public static AdTagging Get(int id)
		{
			AdTagging adTagging = null;
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = @"
SELECT at.song_id, s.pksid, s.duration, at.user_id, at.schema_version, at.status, at.tagging_issue, at.one_ad_xml
FROM ad_tagging at
LEFT JOIN songs s ON at.song_id = s.id
WHERE at.id = @id";
				command.Parameters.AddWithValue("@id", id);

				using (var reader = command.ExecuteReader())
				{
					if (reader.Read())
						adTagging = new AdTagging(
							id,
							reader.GetGuid(0),
							reader.GetString(1),
							reader.GetDecimal(2),
							null,
							reader.GetString(3),
							reader.GetByte(4),
							(AdEntryStatus)reader.GetByte(5),
							reader.GetString(6),
							reader.GetString(7)
						);
				}
			}
			return adTagging;
		}
		public static AdTagging Get(Guid songId, string userId, byte schemaVersion)
		{
			AdTagging adTagging = null;
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = @"
SELECT at.id, s.pksid, s.duration, at.status, at.tagging_issue, at.one_ad_xml
FROM ad_tagging at
LEFT JOIN songs s ON at.song_id = s.id
WHERE at.song_id = @songId AND at.user_id = @userId AND at.schema_version = @schemaVersion";
				command.Parameters.AddWithValue("@songId", songId.ToString());
				command.Parameters.AddWithValue("@userId", userId);
				command.Parameters.AddWithValue("@schemaVersion", schemaVersion);

				using (var reader = command.ExecuteReader())
				{
					if (reader.Read())
						adTagging = new AdTagging(
							reader.GetInt32(0),
							songId,
							reader.GetString(1),
							reader.GetDecimal(2),
							null,
							userId,
							schemaVersion,
							(AdEntryStatus)reader.GetByte(3),
							reader.GetString(4),
							reader.GetString(5)
						);
				}
			}
			return adTagging;
		}
		public static List<AdTagging> Get(string userId, byte schemaVersion)
		{
			var adTaggings = new List<AdTagging>();
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = @"
SELECT at.id, at.song_id, s.pksid, s.duration, at.status, at.tagging_issue, at.one_ad_xml
FROM ad_tagging at
LEFT JOIN songs s ON at.song_id = s.id
WHERE at.user_id = @userId AND at.schema_version = @schemaVersion";
				command.Parameters.AddWithValue("@userId", userId);
				command.Parameters.AddWithValue("@schemaVersion", schemaVersion);

				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var adTagging = new AdTagging(
							reader.GetInt32(0),
							reader.GetGuid(1),
							reader.GetString(2),
							reader.GetDecimal(3),
							null,
							userId,
							schemaVersion,
							(AdEntryStatus)reader.GetByte(4),
							reader.GetString(5),
							reader.GetString(6)
						);
						adTaggings.Add(adTagging);
					}
				}
			}
			return adTaggings;
		}
		public static AdTagging PickNotTagged(string userId, byte schemaVersion)
		{
			AdTagging adTagging = null;
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = @"
SELECT s.id, s.pksid, s.duration, s.title FROM songs s
LEFT JOIN ad_tagging at ON s.id = at.song_id
WHERE s.deleted = 0
AND @userId NOT IN (SELECT at2.user_id FROM ad_tagging at2 WHERE at2.song_id = s.id AND at2.schema_version = @schemaVersion)
ORDER BY RAND()
LIMIT 1";
				command.Parameters.AddWithValue("@userId", userId);
				command.Parameters.AddWithValue("@schemaVersion", schemaVersion);

				using (var reader = command.ExecuteReader())
				{
					if (reader.Read())
						adTagging = new AdTagging(
							0,
							reader.GetGuid(0),
							reader.GetString(1),
							reader.GetDecimal(2),
							reader.GetString(3),
							userId,
							OneAd.SchemaVersion,
							AdEntryStatus.None,
							string.Empty,
							string.Empty
						);
				}
			}
			return adTagging;
		}

		public static Statistics GetStatistics(string userId, byte schemaVersion)
		{
			var stats = new Statistics { SchemaVersion = schemaVersion };
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = @"SELECT status, COUNT(id) FROM ad_tagging WHERE user_id = @userId AND schema_version = @schemaVersion GROUP BY status";
				command.Parameters.AddWithValue("@userId", userId);
				command.Parameters.AddWithValue("@schemaVersion", schemaVersion);

				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var status = (AdEntryStatus)reader.GetByte(0);
						var count = reader.GetInt32(1);

						switch (status)
						{
							case AdEntryStatus.SavePartial:
								stats.Partial = count;
								break;
							case AdEntryStatus.SaveComplete:
								stats.Completed = count;
								break;
							case AdEntryStatus.Skip:
								stats.Skipped = count;
								break;
						}
					}
				}
			}

			return stats;
		}
		public class Statistics
		{
			public byte SchemaVersion;
			public int Completed;
			public int Partial;
			public int Skipped;
		}
	}
}