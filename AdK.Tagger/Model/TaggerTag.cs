using DatabaseCommon;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
	public class TaggerTag
	{
		public int Id;
		public string CreatorId;
		public string Name;

		public static List<TaggerTag> Find(string q)
		{
			using (var connection = Database.Get())
			{
				var command = connection.CreateCommand();
				command.CommandText = @"
					SELECT id, user_id, name FROM tagger_tag WHERE deleted = 0 AND (name LIKE @q OR name LIKE @q2)";
				command.Parameters.AddWithValue("@q", q + "%");
				command.Parameters.AddWithValue("@q2", "% " + q + "%");

				var tags = new List<TaggerTag>();

				using (var dr = command.ExecuteReader())
				{
					while (dr.Read())
						tags.Add(TaggerTag._FromReader(dr));
				}

				return tags;
			}
		}
		public static List<TagUsage> FindWithUsage(string prefix, string q)
		{
			using (var connection = Database.Get())
			{
				var command = connection.CreateCommand();
				command.CommandText = string.Format(_tagUsageSelectSql, @"
					tagger_tag.deleted = 0 AND" +
					(string.IsNullOrEmpty(prefix) ? "" : " name LIKE @prefix AND") +
					@"(tagger_tag.name LIKE @q OR tagger_tag.name LIKE @q2)");

				if (!string.IsNullOrEmpty(prefix))
					command.Parameters.AddWithValue("@prefix", prefix + "%");
				command.Parameters.AddWithValue("@q", q + '%');
				command.Parameters.AddWithValue("@q2", "% " + q + '%');

				var tags = new List<TagUsage>();

				using (var dr = command.ExecuteReader())
				{
					while (dr.Read())
						tags.Add(TagUsage.FromReader(dr));
				}

				return tags;
			}
		}
		public static TagUsage GetUsage(int tagId)
		{
			using (var connection = Database.Get())
			{
				var command = connection.CreateCommand();
				command.CommandText = string.Format(_tagUsageSelectSql, @"tagger_tag.id = @tagId");
				command.Parameters.AddWithValue("@tagId", tagId);

				using (var dr = command.ExecuteReader())
				{
					if (dr.Read())
						return TagUsage.FromReader(dr);
				}

				return null;
			}
		}
		private const string _tagUsageSelectSql = @"
			SELECT tagger_tag.id, tagger_tag.user_id, tagger_tag.name,
			COUNT(DISTINCT(tagger_vote.tagger_song_id)) as samples,
			(SELECT COUNT(brand_id) FROM tagger_tag_brands WHERE tagger_tag_brands.tag_id = tagger_tag.id) as brands,
			(SELECT COUNT(company_id) FROM tagger_tag_companies WHERE tagger_tag_companies.tag_id = tagger_tag.id) as companies
			FROM tagger_vote_tag
			INNER JOIN tagger_tag ON tagger_vote_tag.tagger_tag_id = tagger_tag.id
			INNER JOIN tagger_vote ON tagger_vote.id = tagger_vote_tag.tagger_vote_id
			WHERE {0}
			GROUP BY tagger_tag.id";

		public static TaggerTag Get(int id)
		{
			using (var connection = Database.Get())
			using (var transaction = connection.BeginTransaction())
				return _Get(connection, transaction, id);
		}
		public static void LoadOrCreate(Service.IdName tag, string creatorId)
		{
			if (tag == null || tag.Id.HasValue)
				return;

			using (var connection = Database.Get())
			{
				using (var transaction = connection.BeginTransaction())
				{
					_Load(connection, transaction, tag);

					if (!tag.Id.HasValue)
					{
						_Create(connection, transaction, tag, creatorId);

						transaction.Commit();
					}
				}
			}
		}

		public static bool Merge(TaggerUser user, int masterId, int slaveId, string name)
		{
			using (var db = Database.Get())
			using (var transaction = db.BeginTransaction())
			{
				var master = _Get(db, transaction, masterId);
				var slave = _Get(db, transaction, slaveId);

				if (master != null && slave != null)
				{
					if (master.Name != name)
						master._Rename(db, transaction, name);
					TaggerMergeLog.Log(db, transaction, user, master, slave, isSplit: false);

					var duplicateVoteIds = TaggerVote.FindDuplicate(db, transaction, masterId, slaveId);
					if (duplicateVoteIds.Any())
						TaggerVote.DeleteDuplicate(db, transaction, duplicateVoteIds, slaveId);

					TaggerVote.RemapTag(db, transaction, masterId, slaveId);

					slave._Delete(db, transaction);

					transaction.Commit();
					return true;
				}
			}
			return false;
		}

		public static TaggerTag Split(TaggerUser user, int masterId, string name1, string name2)
		{
			using (var db = Database.Get())
			using (var transaction = db.BeginTransaction())
			{
				var master = _Get(db, transaction, masterId);
				var slave = new Service.IdName { Name = name2 };
				_Load(db, transaction, slave);

				if (master != null && !slave.Id.HasValue)
				{
					if (master.Name != name1)
						master._Rename(db, transaction, name1);

					var slaveTag = _Create(db, transaction, slave, user.Id);

					TaggerMergeLog.Log(db, transaction, user, master, slaveTag, isSplit: true);

					TaggerVote.SplitTag(db, transaction, masterId, slaveTag.Id);

					transaction.Commit();

					foreach (var attribute in TaggerAttribute.Get(masterId))
						TaggerAttribute.Add(slaveTag.Id, attribute.Type, attribute.Id);

					return slaveTag;
				}
			}
			return null;
		}

		private static void _Load(MySqlConnection connection, MySqlTransaction transaction, Service.IdName tag)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT id FROM tagger_tag WHERE name = @tag_name";
			command.Parameters.AddWithValue("@tag_name", tag.Name);

			using (var reader = command.ExecuteReader())
			{
				if (reader.Read())
					tag.Id = reader.GetInt32(0);
			}
		}

		private static TaggerTag _Create(MySqlConnection connection, MySqlTransaction transaction, Service.IdName tag, string creatorId)
		{
			var insertCommand = connection.CreateCommand();
			insertCommand.Transaction = transaction;
			insertCommand.CommandText = @"INSERT INTO tagger_tag (name, user_id) VALUES (@name, @userId)";
			insertCommand.Parameters.AddWithValue("@name", tag.Name);
			insertCommand.Parameters.AddWithValue("@userId", creatorId);

			insertCommand.ExecuteNonQuery();
			tag.Id = (int)insertCommand.LastInsertedId;

			return new TaggerTag
			{
				Id = tag.Id.Value,
				Name = tag.Name,
				CreatorId = creatorId
			};
		}

		private void _Rename(MySqlConnection connection, MySqlTransaction transaction, string name)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"
				UPDATE tagger_tag
				SET
					name = @name
				WHERE
					id = @id";

			command.Parameters.AddWithValue("@id", this.Id);
			command.Parameters.AddWithValue("@name", name);

			command.ExecuteNonQuery();
			this.Name = name;
		}

		private void _Delete(MySqlConnection db, MySqlTransaction transaction)
		{
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"DELETE FROM tagger_tag WHERE id = @id";

			command.Parameters.AddWithValue("@id", this.Id);

			command.ExecuteNonQuery();
		}
		private static TaggerTag _Get(MySqlConnection db, MySqlTransaction transaction, int tagId)
		{
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT id, user_id, name FROM tagger_tag WHERE id = @id";
			command.Parameters.AddWithValue("@id", tagId);

			using (var dr = command.ExecuteReader())
			{
				if (dr.Read())
					return TaggerTag._FromReader(dr);
			}

			return null;
		}
		private static TaggerTag _FromReader(MySqlDataReader dr)
		{
			return new TaggerTag
			{
				Id = dr.GetInt32(0),
				CreatorId = dr.IsDBNull(1) ? (string)null : dr.GetString(1),
				Name = dr.GetString(2)
			};
		}

		public class TagStatistics
		{
			public int Total;
		}
		public static TagStatistics GetStatistics()
		{
			using (var connection = Database.Get())
			{
				var command = connection.CreateCommand();
				command.CommandText = @"SELECT
					(SELECT COUNT(*) FROM tagger_tag WHERE deleted = 0) as Total";

				using (var dr = command.ExecuteReader())
				{
					if (dr.Read())
					{
						int total = dr.GetInt32(0);

						return new TagStatistics
						{
							Total = total
						};
					}
					else
						return new TagStatistics();
				}
			}
		}

		public class TagUsage
		{
			public TaggerTag Tag;
			public int SampleCount;
			public int BrandCount;
			public int CompanyCount;

			internal static TagUsage FromReader(MySqlDataReader dr)
			{
				var tag = TaggerTag._FromReader(dr);
				return new TagUsage
				{
					Tag = tag,
					SampleCount = dr.GetInt32(3),
					BrandCount = dr.GetInt32(4),
					CompanyCount = dr.GetInt32(5)
				};
			}
		}
	}
}