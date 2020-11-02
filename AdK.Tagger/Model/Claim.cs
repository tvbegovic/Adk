using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AdK.Tagger.Model
{
	public class Claim
	{
		public int Id;
		public string UserId;
		public string Name;
		public string Value;

		public static List<Claim> GetByUser(string userId)
		{
			using (var db = Database.Get())
				return _GetByUser(db, null, userId);
		}
		public static List<Claim> GetInUse()
		{
			var claims = new List<Claim>();

			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = @"SELECT DISTINCT name, value FROM user_claim";

				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
						claims.Add(new Claim
						{
							Name = reader.GetString(0),
							Value = reader.GetString(1)
						});
				}
			}
			return claims;
		}

		private static List<Claim> _GetByUser(MySqlConnection db, MySqlTransaction transaction, string userId)
		{
			var claims = new List<Claim>();

			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT id, user_id, name, value FROM user_claim WHERE user_id = @user_id";
			command.Parameters.AddWithValue("@user_id", userId);

			using (var reader = command.ExecuteReader())
			{
				while (reader.Read())
					claims.Add(Claim.FromDataReader(reader));
			}

			return claims;
		}

		public static List<string> GetValues(string userId, string name)
		{
			return GetByUser(userId)
				.Where(c => c.Name == name)
				.Select(c => c.Value)
				.ToList();
		}
		public static string GetSingleValue(string userId, string name)
		{
			return GetValues(userId, name)
				.SingleOrDefault();
		}
		public static bool HasValue(string userId, string name, string value, string subValue = null)
		{
			if (!string.IsNullOrEmpty(subValue))
				value += "." + subValue;
			return GetValues(userId, name)
				.Any(v => v == value);
		}
		public static bool SetModule(string userId, string claim, bool granted)
		{
			bool isGranted = HasValue(userId, "module", claim);
			if (isGranted != granted)
			{
				using (var db = Database.Get())
				{
					var command = db.CreateCommand();
					command.CommandText = granted ?
						@"INSERT INTO user_claim (user_id, name, value) VALUES (@user_id, 'module', @value)" :
						@"DELETE FROM user_claim WHERE user_id = @user_id AND name = 'module' AND value = @value";
					command.Parameters.AddWithValue("@user_id", userId);
					command.Parameters.AddWithValue("@value", claim);
					command.ExecuteNonQuery();
				}
			}
			return true;
		}

		public static void SetDefaultRights(string userId)
		{
            string defaultRights = Application.IsDokaznice
                   ? "spot-upload|spot-library|channels|quick-audit|audit-log|media-house"
                   : Transcript.Configuration.Get().NewUserDefaultRights;

			if (!string.IsNullOrEmpty(defaultRights))
			{
				var claims = defaultRights.Split(new char[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string claim in claims)
					SetModule(userId, claim.Trim(), true);
			}
		}

		public void AddToUser(string userId)
		{
			using (var db = Database.Get())
				this._Insert(db, null);
		}

		public void ReplaceOrAddToUser(string userId)
		{
			using (var db = Database.Get())
			using (var transaction = db.BeginTransaction())
			{
				var claims = _GetByUser(db, transaction, userId);
				var claim = claims.FirstOrDefault(c => c.Name == this.Name);
				if (claim != null)
				{
					claim.Value = this.Value;
					claim._Update(db, transaction);
				}
				else
				{
					this._Insert(db, transaction);
				}
				transaction.Commit();
			}
		}

		private void _Insert(MySqlConnection db, MySqlTransaction transaction)
		{
			Debug.Assert(this.Id == 0);
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"INSERT INTO user_claim(user_id, name, value) VALUES (@user_id, @name, @value)";
			command.Parameters.AddWithValue("@user_id", this.UserId);
			command.Parameters.AddWithValue("@name", this.Name);
			command.Parameters.AddWithValue("@value", this.Value);

			command.ExecuteNonQuery();
			this.Id = (int)command.LastInsertedId;
		}
		private void _Update(MySqlConnection db, MySqlTransaction transaction)
		{
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"UPDATE user_claim SET value = @value WHERE id = @id";
			command.Parameters.AddWithValue("@id", this.Id);
			command.Parameters.AddWithValue("@value", this.Value);

			command.ExecuteNonQuery();
		}

		private static Claim FromDataReader(MySqlDataReader reader)
		{
			return new Claim
			{
				Id = reader.GetInt32(0),
				UserId = reader.GetString(1),
				Name = reader.GetString(2),
				Value = reader.GetString(3)
			};
		}

		public List<string> GetUsersHaving()
		{
			var userIds = new List<string>();

			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = @"SELECT user_id FROM user_claim WHERE name = @name AND value = @value";
				command.Parameters.AddWithValue("@name", this.Name);
				command.Parameters.AddWithValue("@value", this.Value);

				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
						userIds.Add(reader.GetString(0));
				}
			}

			return userIds;
		}
        public static string[] GetAllClaims()
        {
            return new string[]
            {
                "spot-upload",
                "spot-library",
                "tagger",
                "tagger-lab",
                "word-cut",
                "matcher",
                "playout-map",
                "transcript-training",
                "transcript",
                "transcript-review",
                "transcript-stats",
                "transcript-manager",
                "tag-manager",
                "mailing",
                "price-designer",
                "media-house",
                "advertiser-reports",
                "agency-reports",
                "mediahouse-reports",
                "reporting",
                "markets",
                "channels",
                "quick-audit",
                "audit-log",
                "client-feeds",
                "add-feed",
                "price-designer1.1",
				"clients",
				"user-countries",
				"webplayer",
				"price-designer2"
			};
        }
	}
}
