using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using DatabaseCommon;
using MySql.Data.MySqlClient;

namespace AdK.Tagger.Model
{
	
	public class Clip
	{
		public string pksid { get; set; }
		public string segment_youtube_id { get; set; }
		public string channel_name { get; set; }
		public string channel_pkid { get; set; }
		public string playlist_name { get; set; }
		public string clip_name { get; set; }
		public string segment_name { get; set; }
		public string user_name { get; set; }
		public string clip_status { get; set; }
		public string client { get; set; }
		public string competitor { get; set; }
		public string eventName{ get; set; }
		public string language { get; set; }
		public DateTime? clip_start { get; set; }
		public DateTime? clip_end { get; set; }
		public DateTime? segment_start { get; set; }
		public DateTime? segment_end { get; set; }
		public DateTime? export_date_time { get; set; }
		public bool? repeat { get; set; }
		public long id { get; set; }
		public int? segment_track_number { get; set; }
		public int? clip_index { get; set; }
		public int? segment_index { get; set; }
		public double? ave { get; set; }
		public double? ave_per_30_sec { get; set; }
		public double? tams { get; set; }
		public Guid? channel_id { get; set; }
		public string user_id { get; set; }

		public List<ClipTag> Tags { get; set; }

		public static long Create(Clip clip)
		{
			/*if (clip.segment_youtube_id == null)
				clip.segment_youtube_id = clip.pksid.Replace("-", "");*/
			if (clip.clip_index == null)
				clip.clip_index = -1;
			using(var conn = Database.Get())
			{
				var tr = conn.BeginTransaction();
				try
				{
					clip.id = Database.Insert(conn, tr, @"INSERT INTO `clip`
						(`pksid`,`channel_name`,`channel_pkid`,
						`playlist_name`,`clip_name`,`clip_start`,
						`clip_end`,`segment_name`,`segment_start`,
						`segment_end`,`segment_track_number`,
						`segment_youtube_id`,`clip_index`,
						`segment_index`,`ave`,`ave_per_30_sec`,
						`tams`,`export_date_time`,`user_name`,
						`clip_status`,`client`,`competitor`,
						`event`,`language`,`repeat`, channel_id, user_id)
						VALUES
						(@pksid,@channel_name,@channel_pkid,
						@playlist_name,@clip_name,@clip_start,
						@clip_end,@segment_name,@segment_start,
						@segment_end,@segment_track_number,
						@segment_youtube_id,@clip_index,
						@segment_index,@ave,@ave_per_30_sec,
						@tams,@export_date_time,@user_name,
						@clip_status,@client,@competitor,
						@event,@language,@repeat, @channel_id, @user_id)",
						"@pksid", clip.pksid,
						"@channel_name", clip.channel_name,
						"@channel_pkid", clip.channel_pkid,
						"@playlist_name", clip.playlist_name,
						"@clip_name", clip.clip_name,
						"@clip_start", clip.clip_start,
						"@clip_end", clip.clip_end,
						"@segment_name", clip.segment_name,
						"@segment_start", clip.segment_start,
						"@segment_end", clip.segment_end,
						"@segment_track_number", clip.segment_track_number,
						"@segment_youtube_id", clip.segment_youtube_id,
						"@clip_index", clip.clip_index,
						"@segment_index", clip.segment_index,
						"@ave", clip.ave,
						"@ave_per_30_sec", clip.ave_per_30_sec,
						"@tams", clip.tams,
						"@export_date_time", clip.export_date_time,
						"@user_name", clip.user_name,
						"@clip_status", clip.clip_status,
						"@client", clip.client,
						"@competitor", clip.competitor,
						"@event", clip.eventName,
						"@language", clip.language,
						"@repeat", clip.repeat,
						"@channel_id", clip.channel_id,
						"@user_id", clip.user_id);
					if (clip.Tags != null && clip.Tags.Count > 0)
					{
						var sql_clip_tag = "INSERT INTO clip_clip_tag (clip_id, tag_id) VALUES(@clip_id, @tag_id)";
						foreach (var tag in clip.Tags)
						{
							if(tag.id <= 0)
							{
								tag.id = ClipTag.Create(tag);
							}
							Database.ExecuteNonQuery(conn, tr, sql_clip_tag, "@clip_id", clip.id, "@tag_id", tag.id);
						}						
					}
					tr.Commit();
					return clip.id;
				}
				catch (Exception)
				{
					tr.Rollback();					
					throw;
				}
			}
			
			
		}

		public static void Delete(int id)
		{
			Database.ExecuteNonQuery("DELETE FROM clip WHERE id = @id", "@id", id);
		}

		public static long Update(Clip clip)
		{
			using (var conn = Database.Get())
			{
				var tr = conn.BeginTransaction();
				try
				{
					Database.ExecuteNonQuery(conn, tr, @"UPDATE `clip`
					SET
					`pksid` = @pksid,
					`channel_name` = @channel_name,
					`channel_pkid` = @channel_pkid,
					`playlist_name` = @playlist_name,
					`clip_name` = @clip_name,
					`clip_start` = @clip_start,
					`clip_end` = @clip_end,
					`segment_name` = @segment_name,
					`segment_start` = @segment_start,
					`segment_end` = @segment_end,
					`segment_track_number` = @segment_track_number,
					`segment_youtube_id` = @segment_youtube_id,
					`clip_index` = @clip_index,
					`segment_index` = @segment_index,
					`ave` = @ave,
					`ave_per_30_sec` = @ave_per_30_sec,
					`tams` = @tams,
					`export_date_time` = @export_date_time,
					`user_name` = @user_name,
					`clip_status` = @clip_status,
					`client` = @client,
					`competitor` = @competitor,
					`event` = @event,
					`language` = @language,
					`repeat` = @repeat,
					channel_id = @channel_id,
					user_id = @user_id
					WHERE `id` = @id",
					"@id", clip.id,
					"@pksid", clip.pksid,
					"@channel_name", clip.channel_name,
					"@channel_pkid", clip.channel_pkid,
					"@playlist_name", clip.playlist_name,
					"@clip_name", clip.clip_name,
					"@clip_start", clip.clip_start,
					"@clip_end", clip.clip_end,
					"@segment_name", clip.segment_name,
					"@segment_start", clip.segment_start,
					"@segment_end", clip.segment_end,
					"@segment_track_number", clip.segment_track_number,
					"@segment_youtube_id", clip.segment_youtube_id,
					"@clip_index", clip.clip_index,
					"@segment_index", clip.segment_index,
					"@ave", clip.ave,
					"@ave_per_30_sec", clip.ave_per_30_sec,
					"@tams", clip.tams,
					"@export_date_time", clip.export_date_time,
					"@user_name", clip.user_name,
					"@clip_status", clip.clip_status,
					"@client", clip.client,
					"@competitor", clip.competitor,
					"@event", clip.eventName,
					"@language", clip.language,
					"@repeat", clip.repeat,
					"@channel_id", clip.channel_id,
					"@user_id", clip.user_id
					);
					Database.ExecuteNonQuery(conn, tr, "DELETE FROM clip_clip_tag WHERE clip_id = @clip_id", "@clip_id", clip.id);
					if (clip.Tags != null && clip.Tags.Count > 0)
					{
						var sql_clip_tag = "INSERT INTO clip_clip_tag (clip_id, tag_id) VALUES(@clip_id, @tag_id)";
						foreach (var tag in clip.Tags)
						{
							if (tag.id <= 0)
							{
								tag.id = ClipTag.Create(tag);
							}
							Database.ExecuteNonQuery(conn, tr, sql_clip_tag, "@clip_id", clip.id, "@tag_id", tag.id);
						}
					}
					tr.Commit();
				}
				catch
				{
					tr.Rollback();
					throw;
				}
			}

					
			return 1;
		}

		public static List<Clip> GetClipsByCriteria(Guid channelId, string userId, DateTime? clipStart, DateTime? clipEnd)
		{
			//var channel = Channel.Get(new[] { channelId }).FirstOrDefault();
			//var cId = channel != null ? channel.ExternalId : null;
			using(var conn = Database.Get())
			{
				var clips = Database.ListFetcher(conn, null, @"SELECT `clip`.`id`,
							`clip`.`pksid`,	`clip`.`channel_name`,`clip`.`channel_pkid`,`clip`.`playlist_name`,
							`clip`.`clip_name`,`clip`.`clip_start`,	`clip`.`clip_end`,`clip`.`segment_name`,
							`clip`.`segment_start`,`clip`.`segment_end`,`clip`.`segment_track_number`,
							`clip`.`segment_youtube_id`,`clip`.`clip_index`,`clip`.`segment_index`,
							`clip`.`ave`,`clip`.`ave_per_30_sec`,`clip`.`tams`,`clip`.`export_date_time`,
							`clip`.`user_name`,`clip`.`clip_status`,`clip`.`client`,`clip`.`competitor`,
							`clip`.`event`,	`clip`.`language`,`clip`.`repeat`,clip.channel_id, clip.user_id
						FROM `clip` WHERE channel_id = @cId AND
						(
						  (
							(segment_start >= @clipStart OR @clipStart IS NULL) AND
						    (segment_start < @clipEnd OR @clipStart IS NULL)
						  )
						  OR
						  (
							(segment_end >= @clipStart OR @clipStart IS NULL) AND
						    (segment_end < @clipEnd OR @clipStart IS NULL)
						  )
						)",
						dr => new Clip
						{
							id = dr.GetInt32(0),
							pksid = dr.GetStringOrDefault(1),
							channel_name = dr.GetStringOrDefault(2),
							channel_pkid = dr.GetStringOrDefault(3),
							playlist_name = dr.GetStringOrDefault(4),
							clip_name = dr.GetStringOrDefault(5),
							clip_start = dr.GetDateOrNull(6),
							clip_end = dr.GetDateOrNull(7),
							segment_name = dr.GetStringOrDefault(8),
							segment_start = dr.GetDateOrNull(9),
							segment_end = dr.GetDateOrNull(10),
							segment_track_number = dr.GetIntOrNull(11),
							segment_youtube_id = dr.GetStringOrDefault(12),
							clip_index = dr.GetIntOrNull(13),
							segment_index = dr.GetIntOrNull(14),
							ave = dr.GetDoubleOrNull(15),
							ave_per_30_sec = dr.GetDoubleOrNull(16),
							tams = dr.GetDoubleOrNull(17),
							export_date_time = dr.GetDateOrNull(18),
							user_name = dr.GetStringOrDefault(19),
							clip_status = dr.GetStringOrDefault(20),
							client = dr.GetStringOrDefault(21),
							competitor = dr.GetStringOrDefault(22),
							eventName = dr.GetStringOrDefault(23),
							language = dr.GetStringOrDefault(24),
							repeat = dr.GetBoolOrNull(25),
							channel_id = dr.GetGuidOrNull(26),
							user_id = dr.GetStringOrDefault(27)
						},
						"@cId", channelId,
						"@clipStart", clipStart,
						"@clipEnd", clipEnd);
				foreach (var c in clips)
				{
					c.Tags = Database.ListFetcher(conn, null, @"SELECT clip_tag.id, clip_tag.name FROM clip_tag INNER JOIN clip_clip_tag ON clip_tag.id = clip_clip_tag.tag_id
					WHERE clip_clip_tag.clip_id = @id ",
						dr => new ClipTag
						{
							id = dr.GetInt32(0),
							name = dr.GetStringOrDefault(1)
						}, "@id", c.id);
				}
				return clips;
			}
			
			
		}
	}

	public class ClipTag
	{
		public long id { get; set; }
		public string name { get; set; }

		public static long Create(ClipTag tag, MySqlTransaction tr = null)
		{
			var conn = tr != null ? tr.Connection : Database.Get();
			//Check duplicate
			var existing = Database.ItemFetcher<ClipTag>(conn, tr, "SELECT * FROM clip_tag WHERE name = @name", "@name", tag.name);
			if (existing.id > 0)
				return existing.id;
			return Database.Insert(conn, tr, "INSERT INTO clip_tag(name) VALUES(@name)", "@name", tag.name);
		}

		public static List<ClipTag> Search(string text)
		{
			text = "%" + text + "%";
			return Database.ListFetcher<ClipTag>("SELECT * FROM clip_tag WHERE name LIKE @text",
				dr => new ClipTag
				{
					id = dr.GetInt32(0),
					name = dr.GetString(1)
				},				
				"text", text);
		}

		public static List<ClipTag> GetAll()
		{
			return Database.ListFetcher<ClipTag>("SELECT * FROM clip_tag",
				dr => new ClipTag
				{
					id = dr.GetInt32(0),
					name = dr.GetString(1)
				});
		}
	}

	public class ClipDto
	{
		public const string isoFormat = "yyyy-MM-dd HH:mm:ss.fff";

		public string pksid { get; set; }
		public string segment_youtube_id { get; set; }
		public string channel_name { get; set; }
		public string channel_pkid { get; set; }
		public string playlist_name { get; set; }
		public string clip_name { get; set; }
		public string segment_name { get; set; }
		public string user_name { get; set; }
		public string clip_status { get; set; }
		public string client { get; set; }
		public string competitor { get; set; }
		public string eventName { get; set; }
		public string language { get; set; }
		public string clip_start { get; set; }
		public string clip_end { get; set; }
		public string segment_start { get; set; }
		public string segment_end { get; set; }
		public string export_date_time { get; set; }
		public bool? repeat { get; set; }
		public long id { get; set; }
		public int? segment_track_number { get; set; }
		public int? clip_index { get; set; }
		public int? segment_index { get; set; }
		public double? ave { get; set; }
		public double? ave_per_30_sec { get; set; }
		public double? tams { get; set; }
		public Guid? channel_id { get; set; }
		public string user_id { get; set; }

		public List<ClipTag> Tags { get; set; }

		public static ClipDto FromClip(Clip clip)
		{
			return new ClipDto
			{
				ave = clip.ave,
				ave_per_30_sec = clip.ave_per_30_sec,
				channel_id = clip.channel_id,
				channel_name = clip.channel_name,
				channel_pkid = clip.channel_pkid,
				client = clip.client,
				clip_end = clip.clip_end != null ? clip.clip_end.Value.ToString(isoFormat) : null,
				clip_index = clip.clip_index,
				clip_name = clip.clip_name,
				clip_start = clip.clip_start != null ? clip.clip_start.Value.ToString(isoFormat) : null,
				clip_status = clip.clip_status,
				competitor = clip.competitor,
				eventName = clip.eventName,
				export_date_time = clip.export_date_time != null ? clip.export_date_time.Value.ToString(isoFormat) : null,
				id = clip.id,
				language = clip.language,
				pksid = clip.pksid,
				playlist_name = clip.playlist_name,
				repeat = clip.repeat,
				segment_end = clip.segment_end != null ? clip.segment_end.Value.ToString(isoFormat) : null,
				segment_index = clip.segment_index,
				segment_name = clip.segment_name,
				segment_start = clip.segment_start != null ? clip.segment_start.Value.ToString(isoFormat) : null,
				segment_track_number = clip.segment_track_number,
				segment_youtube_id = clip.segment_youtube_id,
				Tags = clip.Tags,
				tams = clip.tams,
				user_id = clip.user_id,
				user_name = clip.user_name
			};
		}
	}
}
