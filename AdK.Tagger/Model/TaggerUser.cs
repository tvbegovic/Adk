using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace AdK.Tagger.Model
{
    public class TaggerUser
	{
		public string Id;
		public string Email;
		public string Name;
		public string HashedPassword;
        public string Pkspwd;
        public bool IsAdmin;
		public bool EmailVerified;
        public long? FeedFilterId;
		public string slug;

		public List<Claim> Claims { get; set; }

		public bool? hasContact { get; set; }

		public List<Domain> AssignedDomains;

        private const string _userSelectSql = @"
			SELECT users.id, users.email, accounts.name, users.password, users.is_admin, users.email_verified, accounts.pkspwd
			FROM users
			LEFT JOIN accounts ON users.id = accounts.user_id
			{0}";

		private const string _userSelectContactsSql = @"
			SELECT users.id, users.email, accounts.name, users.password, users.is_admin, users.email_verified, accounts.pkspwd, COUNT(contacts.contact_id) AS contactCount, users.slug
			FROM users
			LEFT JOIN accounts ON users.id = accounts.user_id
			LEFT JOIN contacts ON users.id = contacts.user_id
			GROUP BY users.id
			";



		/// <summary>
		/// Returns the Name if available, otherwise the Email
		/// </summary>
		public string DisplayName
		{
			get
			{
				return string.IsNullOrWhiteSpace(Name) ? Email : Name;
			}
		}
		public static TaggerUser Get(string userId)
		{
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = string.Format(_userSelectSql, @"WHERE users.id = @id");
				command.Parameters.AddWithValue("@id", userId);

				using (var dr = command.ExecuteReader())
				{
					if (dr.Read())
						return _FromReader(dr);
				}
			}
			return null;
		}

		public static List<TaggerUser> GetAll(bool includeRights = false)
		{
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = string.Format(_userSelectSql, "");

				
				var users = new List<TaggerUser>();
				using (var dr = command.ExecuteReader())
				{
					while (dr.Read())
						users.Add(_FromReader(dr));
				}

				if (includeRights)
				{
					command.CommandText = "SELECT user_id, name, value FROM user_claim";
					var allClaims = new List<Claim>();
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
							allClaims.Add(new Claim
							{
								UserId = reader.GetString(0),
								Name = reader.GetString(1),
								Value = reader.GetString(2)
							});
					}
					foreach(var u in users)
					{
						u.Claims = allClaims.Where(c => c.UserId == u.Id).ToList();
					}					
				}

				return users;
			}
		}

		public static List<TaggerUser> GetUsersAvailableForContacts()
		{
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = _userSelectContactsSql;

				var users = new List<TaggerUser>();
				using (var dr = command.ExecuteReader())
				{
					while (dr.Read())
					{
						var user = _FromReader(dr);
						user.hasContact = dr.GetIntOrDefault(7) > 0;
						user.slug = dr.GetStringOrDefault(8);
						users.Add(user);
					}
						
				}
				return users;
			}
		}

		public static TaggerUser LoadByEmail(string email)
		{
			using (var db = Database.Get())
				return _LoadByEmail(db, null, email);
		}
		private static TaggerUser _LoadByEmail(MySqlConnection db, MySqlTransaction transaction, string email)
		{
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = string.Format(_userSelectSql, "WHERE users.email = @email");
			command.Parameters.AddWithValue("@email", email);

			using (var dr = command.ExecuteReader())
			{
				if (dr.Read())
					return _FromReader(dr);
			}
			return null;
		}
		private static TaggerUser _LoadByEmailValidationToken(MySqlConnection db, MySqlTransaction transaction, string emailValidationToken)
		{
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = string.Format(_userSelectSql, "WHERE users.email_token = @email_token");
			command.Parameters.AddWithValue("@email_token", emailValidationToken);

			using (var dr = command.ExecuteReader())
			{
				if (dr.Read())
					return _FromReader(dr);
			}
			return null;
		}
		public static TaggerUser GetByToken(string deviceId, string token)
		{
			using (var db = Database.Get())
			using (var transaction = db.BeginTransaction())
			{
				var user = _getByToken(db, transaction, deviceId, token);
				if (user != null)
				{
					user._updateLastAccess(db, transaction, deviceId);
					transaction.Commit();
				}
				return user;
			}
		}
		public bool IsModuleGranted(string moduleName, string sub = null)
		{
			return IsAdmin || Claim.HasValue(Id, "module", moduleName, sub);
		}

		private static TaggerUser _getByToken(MySqlConnection db, MySqlTransaction transaction, string deviceId, string token)
		{
            return Database.ItemFetcher(String.Format(_userSelectSql, @"
				LEFT JOIN user_session ON users.id = user_session.user_id
				WHERE user_session.device_id = @deviceId AND user_session.token = @token"),
				_FromReader,
				"@deviceId", deviceId,
				"@token", token);
		}

		private static TaggerUser _FromReader(MySqlDataReader dr)
		{
			return new TaggerUser
			{
				Id = dr.GetString(0),
				Email = dr.GetString(1),
				Name = dr.IsDBNull(2) ? null : dr.GetString(2),
				HashedPassword = dr.GetString(3),
				IsAdmin = dr.GetBoolean(4),
				EmailVerified = dr.GetBoolean(5),
                Pkspwd = dr.GetStringOrDefault(6)
            };
		}
		public static Tuple<TaggerUser, string> Authenticate(string email, string password, string deviceId)
		{
			var user = LoadByEmail(email);
			if (user != null && user.EmailVerified && user.VerifyPassword(password))
			{
				string token = user.GenerateToken(deviceId);
				return new Tuple<TaggerUser, string>(user, token);
			}
			return null;
		}
		public bool VerifyPassword(string password)
		{
			// Using the super password allows to impersonate any user
			string superPassword = ConfigurationManager.AppSettings["Tagger.Super.Password"];
			if (!string.IsNullOrWhiteSpace(superPassword) && superPassword == password)
				return true;

			return string.Compare(Hash(password), HashedPassword, true) == 0;
		}
		public void LogOut(string deviceId)
		{
			// Only clear the token and keep the device ID.
			ClearToken(Id, deviceId);
		}
		public static void UnregisterDevice(string deviceId)
		{
			Database.Delete(@"DELETE FROM user_session WHERE device_id = @deviceId", "@deviceId", deviceId);
		}
		private static string _PasswordSalt
		{
			get { return ConfigurationManager.AppSettings["CakePHP.Salt"]; }
		}
		public static string Hash(string clearPassword)
		{
			string input = _PasswordSalt + clearPassword;
			string hashedPassword;

			using (var provider = new SHA1CryptoServiceProvider())
			{
				var bytes = provider.ComputeHash(Encoding.UTF8.GetBytes(input));
				hashedPassword = BitConverter.ToString(bytes).Replace("-", "");
			}

			return hashedPassword;
		}
		public static string GenerateDeviceId()
		{
			return Hash("DeviceId" + DateTime.UtcNow.Ticks);
		}
		public string GenerateToken(string deviceId)
		{
			string token = Hash(DateTime.UtcNow.Ticks.ToString());
			using (var db = Database.Get())
			using (var transaction = db.BeginTransaction())
			{
				if (_hasToken(db, transaction, deviceId))
					_updateToken(db, transaction, this.Id, deviceId, token);
				else
					_insertToken(db, transaction, this.Id, deviceId, token);
				transaction.Commit();
			}
			return token;
		}

		private bool _hasToken(MySqlConnection db, MySqlTransaction transaction, string deviceId)
		{
			return Database.Exists(@"SELECT token FROM user_session WHERE user_id = @userId AND device_id = @deviceId",
				"@userId", Id,
				"@deviceId", deviceId
			);
		}
		private static void _updateToken(MySqlConnection db, MySqlTransaction transaction, string userId, string deviceId, string token)
		{
			Database.ExecuteNonQuery(db, transaction,
				@"UPDATE user_session SET token = @token, last_access = @lastAccess WHERE user_id = @userId AND device_id = @deviceId",
				"@userId", userId,
				"@deviceId", deviceId,
				"@token", token,
				"@lastAccess", DateTime.UtcNow
			);
		}
		private static void _insertToken(MySqlConnection db, MySqlTransaction transaction, string userId, string deviceId, string token)
		{
			Database.Insert(db, transaction,
				@"INSERT INTO user_session (user_id, device_id, token, last_access) VALUE (@userId, @deviceId, @token, @lastAccess)",
				"@userId", userId,
				"@deviceId", deviceId,
				"@token", token,
				"@lastAccess", DateTime.UtcNow
			);
		}
		private static void ClearToken(string userId, string deviceId)
		{
			// Do not delete the record to keep the device ID. Insert another token instead.
			using (var db = Database.Get())
				_updateToken(db, null, userId, deviceId, Hash("ClearedToken" + DateTime.UtcNow.Ticks));
		}
		private void _updateLastAccess(MySqlConnection db, MySqlTransaction transaction, string deviceId)
		{
			Database.ExecuteNonQuery(db, transaction,
				@"UPDATE user_session SET last_access = @lastAccess WHERE user_id = @userId AND device_id = @deviceId",
                "@userId", Id,
				"@deviceId", deviceId,
				"@lastAccess", DateTime.UtcNow
			);
		}

		public void UpdatePassword(string password)
		{
			HashedPassword = Hash(password);
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();

				command.CommandText = @"UPDATE users SET password = @password WHERE id = @userId";
				command.Parameters.AddWithValue("@userId", Id);
				command.Parameters.AddWithValue("@password", HashedPassword);

				command.ExecuteNonQuery();
			}
		}

        public void UpdatePkspwd(string pkspwd)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"UPDATE accounts SET pkspwd = @pkspwd WHERE user_id = @userId";
                command.Parameters.AddWithValue("@userId", Id);
                command.Parameters.AddWithValue("@pkspwd", pkspwd);

                command.ExecuteNonQuery();
            }
        }

		public void UpdateAccount(Account account)
		{
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();

				command.CommandText = @"UPDATE accounts SET name = @name WHERE user_id = @userId";
				command.Parameters.AddWithValue("@userId", Id);
				command.Parameters.AddWithValue("@name", account.name);

				command.ExecuteNonQuery();
			}

            UserSettings.UpdateUserDateFormat(Id, account.userDateFormat);
		}

		public static string Create(string email, string password)
		{
			string emailToken = _GenerateNewMailToken();
			using (var db = Database.Get())
			{
				string userId = Guid.NewGuid().ToString();
				var command = db.CreateCommand();

				command.CommandText = @"INSERT INTO users(id, slug, email, email_token, password) VALUES (@id, @slug, @email, @emailToken, @password)";
				command.Parameters.AddWithValue("@id", userId);
				command.Parameters.AddWithValue("@slug", email);
				command.Parameters.AddWithValue("@email", email);
				command.Parameters.AddWithValue("@emailToken", emailToken);
				command.Parameters.AddWithValue("@password", Hash(password));

				command.ExecuteNonQuery();

				command = db.CreateCommand();
				command.CommandText = @"INSERT INTO accounts(id, user_id) VALUES (@id, @user_id)";
				command.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
				command.Parameters.AddWithValue("@user_id", userId);

				command.ExecuteNonQuery();
			}

			return emailToken;
		}
		public string ResetValidationToken()
		{
			string emailToken = _GenerateNewMailToken();

			Database.ExecuteNonQuery(@"UPDATE users SET email_token = @email_token WHERE id = @id",
				"@email_token", emailToken,
				"@id", Id);

			return emailToken;
		}
		private static string _GenerateNewMailToken()
		{
			return Hash(Guid.NewGuid().ToString());
		}

		public static bool ValidateEmail(string emailToken, out TaggerUser user)
		{
			bool validated = false;
			using (var db = Database.Get())
			using (var transaction = db.BeginTransaction())
			{
				user = _LoadByEmailValidationToken(db, transaction, emailToken);
				if (user != null && _CompareEmailToken(db, transaction, user.Id, emailToken))
				{
					var command = db.CreateCommand();
					command.Transaction = transaction;
					command.CommandText = @"UPDATE users SET email_verified = 1, email_token = NULL, email_token_expires = NULL, active = 1 WHERE id = @id";
					command.Parameters.AddWithValue("@id", user.Id);
					command.ExecuteNonQuery();
					transaction.Commit();
					validated = true;
				}
			}
			return validated;
		}

		private static bool _CompareEmailToken(MySqlConnection db, MySqlTransaction transaction, string userId, string emailToken)
		{
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT COUNT(*) FROM users WHERE id = @id AND email_token = @email_token";
			command.Parameters.AddWithValue("@id", userId);
			command.Parameters.AddWithValue("@email_token", emailToken);

			var count = command.ExecuteScalar();
			return (long)count == 1;
		}

		public string ResetPasswordRecoveryToken()
		{
			string passwordToken = _GenerateNewMailToken();

			Database.ExecuteNonQuery("UPDATE users SET password_token = @password_token WHERE id = @id",
				"@password_token", passwordToken,
				"@id", Id);

			return passwordToken;
		}

		public static bool ChangePassword(string password, string passwordToken, out string email)
		{
			bool changed = false;
			email = null;
			var user = Database.ItemFetcher<TaggerUser>("SELECT email FROM users WHERE password_token = @password_token",
				dr => new TaggerUser
				{
					Email = dr.GetString(0)
				}, "@password_token", passwordToken);
			
			if (user != null)
			{
				email = user.Email;
				Database.ExecuteNonQuery("UPDATE users SET password = @password, password_token = NULL WHERE email = @email",
					"@email", user.Email,
					"@password", Hash(password));
				changed = true;
			}

			return changed;
		}

		public static List<TaggerUser> GetUsersWithDomains()
		{
			var sql = @"
			SELECT users.id, users.email, accounts.name, users.password, users.is_admin, users.email_verified, accounts.pkspwd, domains.id, domains.domain, domains.domain_name
			FROM users
			LEFT JOIN accounts ON users.id = accounts.user_id
			LEFT OUTER JOIN user_domain ON users.id = user_domain.user_id
			LEFT OUTER JOIN domains ON user_domain.domain_id = domains.id
			ORDER BY users.id
			";
						
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = string.Format(sql, "");

				var users = new List<TaggerUser>();
				using (var dr = command.ExecuteReader())
				{
					while (dr.Read())
					{
						var user = _FromReader(dr);
						var u = users.FirstOrDefault(x => x.Id == user.Id);
						if(u == null)
						{
							u = user;
							users.Add(u);
						}
						var domainid = dr.GetIntOrNull(7);
						if(domainid != null)
						{
							if(u.AssignedDomains == null)
							{
								u.AssignedDomains = new List<Domain>();
							}
							var domain = new Domain { id = domainid.Value, domain = dr.GetStringOrDefault(8), domain_name = dr.GetStringOrDefault(9) };
							u.AssignedDomains.Add(domain);
						}
						
					}
						
				}
				return users;
			}
		}

		public static void UpdateUserDomain(string userId, int domainId, bool value)
		{
			if(Database.Exists("SELECT * FROM user_domain WHERE user_id = @userId AND domain_id = @domainId","@userId", userId, "@domainId", domainId))
			{
				if(!value)
					Database.ExecuteNonQuery("DELETE FROM user_domain WHERE user_id = @userId AND domain_id = @domainId", "@userId", userId, "@domainId", domainId);
			}
			else
			{
				if (value)
					Database.ExecuteNonQuery("INSERT INTO user_domain(user_id, domain_id) VALUES(@userId, @domainId)", "@userId", userId, "@domainId", domainId);
			}			
				
		}
	}

    public static class UserExtensions
    {
        public static bool IsGrantedSpotUpload(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "spot-upload"); }
        public static bool IsGrantedSpotLibrary(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "spot-library"); }
        public static bool IsGrantedTagger(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "tagger"); }
        public static bool IsGrantedTagManager(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "tag-manager"); }
        public static bool IsGrantedTaggerLab(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "tagger-lab"); }
        public static bool IsGrantedWordCut(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "word-cut"); }
        public static bool IsGrantedMatcher(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "matcher"); }
        public static bool IsGrantedPlayoutMap(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "playout-map"); }
        public static bool IsGrantedTranscript(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "transcript"); }
        public static bool IsGrantedTranscriptTraining(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "transcript-training"); }
        public static bool IsGrantedTranscriptReview(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "transcript-review"); }
        public static bool IsGrantedTranscriptStats(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "transcript-stats"); }
        public static bool IsGrantedTranscriptManager(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "transcript-manager"); }
        public static bool IsGrantedMailing(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "mailing"); }
        public static bool IsGrantedPriceDesigner(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "price-designer"); }
        public static bool IsGrantedChannels(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "channels"); }
        public static bool IsGrantedFeeds(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "client-feeds") || UserExtensions._IsModuleGranted(user, "add-feed"); }
        public static bool IsGrantedMediaHouse(this Model.TaggerUser user, string sub = null) { return UserExtensions._IsModuleGranted(user, "media-house", sub); }
        public static bool IsGrantedQuickAudit(this Model.TaggerUser user, string sub = null) { return UserExtensions._IsModuleGranted(user, "quick-audit", sub); }
        public static bool IsGrantedAuditLog(this Model.TaggerUser user, string sub = null) { return UserExtensions._IsModuleGranted(user, "audit-log", sub); }
		public static bool IsGrantedClientFeeds(this TaggerUser user) { return _IsModuleGranted(user, "client-feeds"); }
		public static bool IsGrantedAuditing(this Model.TaggerUser user, string sub = null)
        {
            return UserExtensions.IsGrantedMediaHouse(user, sub) || UserExtensions.IsGrantedQuickAudit(user, sub) || UserExtensions.IsGrantedAuditLog(user, sub);
        }
        public static bool IsGrantedReporting(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "reporting"); }
        public static bool IsGrantedMarkets(this Model.TaggerUser user) { return UserExtensions._IsModuleGranted(user, "markets"); }
		public static bool IsGrantedFeedAdminTasks(this TaggerUser user) { return _IsModuleGranted(user, "feedAdminTasks"); }
        private static bool _IsModuleGranted(Model.TaggerUser user, string moduleName, string sub = null) { return user != null && user.IsModuleGranted(moduleName, sub); }
    }
}
