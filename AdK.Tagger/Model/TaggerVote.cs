using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
	public class TaggerVote
	{
		public int Id;
		public string UserId;
		public int TaggerSongId;
		public SkipReason Skip;
		public DateTime TaggingStart;
		public DateTime TaggingEnd;
		public string Title;
		public List<int> TaggerTags;
		public int? TaggerCompanyId;
		public int? TaggerIndustryId;
		public int? TaggerCategoryId;
		public Guid? ProductId;

		public enum SkipReason : byte
		{
			NotSkipped = 0,
			NotIdentified = 1
		}

		public static void Vote(int taggerSongId, int[] taggerTagIds, int? taggerCompanyId, int? taggerIndustryId, int? taggerCategoryId, Guid? productId, string userId)
		{
			using (var connection = Database.Get())
			using (var transaction = connection.BeginTransaction())
			{
				var vote = _GetVote(connection, transaction, taggerSongId, userId);
				if (vote == null)
				{
					vote = new TaggerVote
					{
						UserId = userId,
						TaggerSongId = taggerSongId,
						Skip = SkipReason.NotSkipped,
						TaggerTags = taggerTagIds.ToList(),
						TaggerCompanyId = taggerCompanyId,
						TaggerIndustryId = taggerIndustryId,
						TaggerCategoryId = taggerCategoryId,
						ProductId = productId
					};

					vote._Insert(connection, transaction);
				}
				else
				{
					vote.Skip = SkipReason.NotSkipped;
					vote.TaggerCompanyId = taggerCompanyId;
					vote.TaggerIndustryId = taggerIndustryId;
					vote.TaggerCategoryId = taggerCategoryId;
					vote.ProductId = productId;

					vote._Update(connection, transaction);
					vote._UpdateTags(connection, transaction, taggerTagIds);
				}
				transaction.Commit();
			}
		}
		private static TaggerVote _GetVote(MySqlConnection connection, MySqlTransaction transaction, int taggerSongId, string userId)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"
				SELECT tagger_song_id, skip, tagging_start, tagging_end, tagger_company_id, tagger_industry_id, tagger_category_id, product_id
				FROM tagger_vote
				WHERE
					tagger_vote.tagger_song_id = @tagger_song_id AND
					tagger_vote.user_id = @user_id";
			command.Parameters.AddWithValue("@tagger_song_id", taggerSongId);
			command.Parameters.AddWithValue("@user_id", userId);

			TaggerVote vote = null;

			using (var dr = command.ExecuteReader())
			{
				if (dr.Read())
					vote = new TaggerVote
					{
						Id = dr.GetInt32(0),
						UserId = userId,
						TaggerSongId = taggerSongId,
						Skip = (SkipReason)dr.GetByte(1),
						TaggingStart = dr.GetDateTime(2),
						TaggingEnd = dr.GetDateTime(3),
						TaggerCompanyId = dr.IsDBNull(4) ? (int?)null : dr.GetInt32(4),
						TaggerIndustryId = dr.IsDBNull(5) ? (int?)null : dr.GetInt32(5),
						TaggerCategoryId = dr.IsDBNull(6) ? (int?)null : dr.GetInt32(6),
						ProductId = dr.IsDBNull(7) ? (Guid?)null : dr.GetGuid(7)
					};
			}

			return vote;
		}
		private static List<int> _GetVoteTags(MySqlConnection connection, MySqlTransaction transaction, int taggerVoteId)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT tagger_tag_id FROM tagger_vote_tag WHERE tagger_vote_id = @tagger_vote_id";
			command.Parameters.AddWithValue("@tagger_vote_id", taggerVoteId);

			var tagIds = new List<int>();
			using (var dr = command.ExecuteReader())
			{
				while (dr.Read())
					tagIds.Add(dr.GetInt32(0));
			}

			return tagIds;
		}
		private void _Insert(MySqlConnection connection, MySqlTransaction transaction)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"
				INSERT INTO tagger_vote (
					user_id,
					tagger_song_id,
					skip,
					tagging_start,
					tagging_end,
					tagger_company_id,
					tagger_industry_id,
					tagger_category_id,
					product_id)
				VALUES (
					@userId,
					@tagger_song_id,
					@skip,
					@tagging_start,
					@tagging_end,
					@tagger_company_id,
					@tagger_industry_id,
					@tagger_category_id,
					@product_id)";

			command.Parameters.AddWithValue("@userId", UserId);
			command.Parameters.AddWithValue("@tagger_song_id", TaggerSongId);
			command.Parameters.AddWithValue("@skip", (byte)Skip);
			command.Parameters.AddWithValue("@tagging_start", TaggingStart);
			command.Parameters.AddWithValue("@tagging_end", TaggingEnd);
			command.Parameters.AddWithValue("@tagger_company_id", TaggerCompanyId);
			command.Parameters.AddWithValue("@tagger_industry_id", TaggerIndustryId);
			command.Parameters.AddWithValue("@tagger_category_id", TaggerCategoryId);
			command.Parameters.AddWithValue("@product_id", ProductId == null ? null : ProductId.ToString());

			command.ExecuteNonQuery();
			this.Id = (int)command.LastInsertedId;

			if (TaggerTags != null)
				_InsertTags(connection, transaction, TaggerTags);
		}
		private void _InsertTags(MySqlConnection connection, MySqlTransaction transaction, IEnumerable<int> tagIds)
		{
			foreach (int tagId in tagIds)
				_InsertTag(connection, transaction, this.Id, tagId);
		}
		private static void _InsertTag(MySqlConnection connection, MySqlTransaction transaction, int voteId, int tagId)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"INSERT INTO tagger_vote_tag (tagger_vote_id, tagger_tag_id) VALUES (@tagger_vote_id, @tagger_tag_id)";
			command.Parameters.AddWithValue("@tagger_vote_id", voteId);
			command.Parameters.AddWithValue("@tagger_tag_id", tagId);

			command.ExecuteNonQuery();
		}
		private void _Update(MySqlConnection connection, MySqlTransaction transaction = null)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"
				UPDATE tagger_vote
				SET
					skip = @skip,
					tagging_start = @tagging_start,
					tagging_end = @tagging_end,
					tagger_company_id = @tagger_company_id,
					tagger_industry_id = @tagger_industry_id,
					tagger_category_id = @tagger_category_id,
					product_id = @product_id
				WHERE
					id = @id";

			command.Parameters.AddWithValue("@id", Id);
			command.Parameters.AddWithValue("@skip", (byte)Skip);
			command.Parameters.AddWithValue("@tagging_start", TaggingStart);
			command.Parameters.AddWithValue("@tagging_end", TaggingEnd);
			command.Parameters.AddWithValue("@tagger_company_id", TaggerCompanyId);
			command.Parameters.AddWithValue("@tagger_industry_id", TaggerIndustryId);
			command.Parameters.AddWithValue("@tagger_category_id", TaggerCategoryId);
			command.Parameters.AddWithValue("@product_id", ProductId == null ? null : ProductId.ToString());

			command.ExecuteNonQuery();
		}
		private void _UpdateTags(MySqlConnection connection, MySqlTransaction transaction, IEnumerable<int> tagIds)
		{
			var removedTags = TaggerTags.Except(tagIds);
			var addedTags = tagIds.Except(TaggerTags);

			_DeleteTags(connection, transaction, tagIds);
			_InsertTags(connection, transaction, addedTags);
		}
		private void _DeleteTags(MySqlConnection connection, MySqlTransaction transaction, IEnumerable<int> tagIds)
		{
			foreach (int tagId in tagIds)
			{
				var command = connection.CreateCommand();
				command.Transaction = transaction;
				command.CommandText = @"DELETE tagger_vote_tag WHERE vote_id = @vote_id AND tagger_tag_id = @tagger_tag_id";
				command.Parameters.AddWithValue("@tagger_vote_id", this.Id);
				command.Parameters.AddWithValue("@tagger_tag_id", tagId);

				command.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Finds the votes associated to both tag IDs
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="transaction"></param>
		/// <param name="masterTagId"></param>
		/// <param name="slaveTagId"></param>
		public static List<int> FindDuplicate(MySqlConnection connection, MySqlTransaction transaction, int masterTagId, int slaveTagId)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"
				SELECT tagger_vote_id FROM
				(SELECT tagger_vote_id FROM tagger_vote_tag WHERE tagger_tag_id = @masterId OR tagger_tag_id = @slaveId) as votes
				GROUP BY tagger_vote_id
				HAVING COUNT(tagger_vote_id) = 2";

			command.Parameters.AddWithValue("@masterId", masterTagId);
			command.Parameters.AddWithValue("@slaveId", slaveTagId);

			var voteIds = new List<int>();
			using (var dr=command.ExecuteReader())
			{
				while (dr.Read())
					voteIds.Add(dr.GetInt32(0));
			}
			return voteIds;
		}
		public static List<int> FindHavingTag(MySqlConnection connection, MySqlTransaction transaction, int tagId)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT tagger_vote_id FROM tagger_vote_tag WHERE tagger_tag_id = @tagId";
			command.Parameters.AddWithValue("@tagId", tagId);

			var voteIds = new List<int>();
			using (var dr = command.ExecuteReader())
			{
				while (dr.Read())
					voteIds.Add(dr.GetInt32(0));
			}
			return voteIds;
		}
		public static void DeleteDuplicate(MySqlConnection connection, MySqlTransaction transaction, List<int> voteIds, int slaveTagId)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"DELETE FROM tagger_vote_tag WHERE tagger_vote_id IN (" + string.Join(",", voteIds) + ") AND tagger_tag_id = @slaveId";

			command.Parameters.AddWithValue("@slaveId", slaveTagId);
			command.ExecuteNonQuery();
		}
		public static void RemapTag(MySqlConnection connection, MySqlTransaction transaction, int masterTagId, int slaveTagId)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"
				UPDATE tagger_vote_tag
				SET
					tagger_tag_id = @masterId
				WHERE
					tagger_tag_id = @slaveId";

			command.Parameters.AddWithValue("@masterId", masterTagId);
			command.Parameters.AddWithValue("@slaveId", slaveTagId);

			command.ExecuteNonQuery();
		}
		public static void SplitTag(MySqlConnection connection, MySqlTransaction transaction, int existingVoteTagId, int newVoteTagId)
		{
			var voteIds = FindHavingTag(connection, transaction, existingVoteTagId);
			foreach (int voteId in voteIds)
				_InsertTag(connection, transaction, voteId, newVoteTagId);
		}

		public class Statistics
		{
			public int Tagged;
			public int Skipped;
			public int Remaining;
		}
		public static Statistics GetStatistics(string userId, string filter)
		{
			using (var connection = Database.Get())
			{
				var command = connection.CreateCommand();

				bool hasFilter = !string.IsNullOrWhiteSpace(filter);
				string query = @"SELECT
					(SELECT COUNT(*) FROM tagger_vote WHERE skip = 0 AND user_id = @user_id) as Tagged,
					(SELECT COUNT(*) FROM tagger_vote WHERE skip <> 0 AND user_id = @user_id) as Skipped,
					(SELECT COUNT(*) FROM tagger_song WHERE
						{0}
						NOT EXISTS (SELECT 1 FROM tagger_vote WHERE tagger_song.id = tagger_vote.tagger_song_id AND tagger_vote.user_id = @user_id)) as NotTagged,
					(SELECT COUNT(*) FROM songs WHERE
						product_id IS NULL AND
						deleted = 0 AND
						NOT EXISTS (SELECT 1 FROM tagger_song WHERE tagger_song.song_id = songs.id)) as Remaining";
				command.CommandText = string.Format(query, hasFilter ? "(title LIKE @filter OR title LIKE @filter2) AND" : "");
				command.Parameters.AddWithValue("@user_id", userId);
				if (hasFilter)
				{
					command.Parameters.AddWithValue("@filter", filter + "%");
					command.Parameters.AddWithValue("@filter2", "% " + filter + "%");
				}

				using (var dr = command.ExecuteReader())
				{
					if (dr.Read())
					{
						int tagged = dr.GetInt32(0);
						int skipped = dr.GetInt32(1);
						int untagged = dr.GetInt32(2);
						int remaining = dr.GetInt32(3);
						
						return new Statistics
						{
							Tagged = tagged,
							Skipped = skipped,
							Remaining = remaining + untagged
						};
					}
					else
						return new Statistics();
				}
			}
		}

		public static void SkipSong(int taggerSongId, SkipReason skipReason, string userId)
		{
			using (var connection = Database.Get())
			using (var transaction = connection.BeginTransaction())
			{
				var vote = _GetVote(connection, transaction, taggerSongId, userId);

				if (vote == null)
				{
					vote = new TaggerVote
					{
						UserId = userId,
						TaggerSongId = taggerSongId,
						Skip = skipReason,
						TaggerCompanyId = null,
						TaggerIndustryId = null,
						TaggerCategoryId = null,
						ProductId = null
					};

					vote._Insert(connection, transaction);
				}
				else
				{
					vote.Skip = skipReason;
					vote._Update(connection, transaction);
				}

				transaction.Commit();
			}
		}
	}
}