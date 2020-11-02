using System.Collections.Generic;

namespace DatabaseCommon
{
    public enum UserSettingModule
	{
		ReportFilter = 1,
		Pagination = 2,
      UserDateFormat = 3,
		Defaults = 4
	}


	public class UserSettings
	{
		public static IEnumerable<UserSettingItem> Get( string userId )
		{
			string query = @"SELECT module, `key`, value, user_id
				FROM user_settings 
				WHERE user_id = @userId";

			return Database.ListFetcher( query, dr => new UserSettingItem() {
				Module = dr.GetString( 0 ),
				Key = dr.GetString( 1 ),
				Value = dr.GetString( 2 ),
				UserId = dr.GetString( 3 )
			}, "@userId", userId );
		}

        public static void Update( string userId, UserSettingModule module, string key, string value )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"INSERT INTO user_settings (user_id, module, `key`, value) 
								VALUES(@userId, @module, @key , @value) ON DUPLICATE KEY UPDATE    
								value=@value";

				command.Parameters.AddWithValue( "@userId", userId );
				command.Parameters.AddWithValue( "@module", module.ToString() );
				command.Parameters.AddWithValue( "@key", key );
				command.Parameters.AddWithValue( "@value", value );
				command.ExecuteNonQuery();

			}

		}

        public static string GetUserDateFormat(string userId)
        {
            string query = @"SELECT IFNULL((SELECT value
				FROM user_settings 
				WHERE user_id = @userId AND `key` = 'UserDateFormat'), 'yyyy-MM-dd')";

            return Database.ItemFetcher(query, dr => dr.GetString(0), "@userId", userId);
        }

        public static void UpdateUserDateFormat(string userId, string userDateFormat)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"INSERT INTO user_settings (user_id, module, `key`, value) 
								VALUES(@userId, @module, @key, @value) ON DUPLICATE KEY UPDATE    
								value=@value";

                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@module", UserSettingModule.UserDateFormat.ToString());
                command.Parameters.AddWithValue("@key", "UserDateFormat");
                command.Parameters.AddWithValue("@value", userDateFormat);
                command.ExecuteNonQuery();

            }

        }

    }
}
