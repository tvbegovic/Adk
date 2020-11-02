using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace DatabaseCommon
{
	public static class CachedSettings
	{
		private static Dictionary<string, string> _cachedSettings;
		private static Dictionary<string, List<SettingItem>> _cachedModuleSettings;

		static CachedSettings()
		{
			ClearCache();
		}

		public static string Get( string module, string key )
		{
			string dicKey = GetDictionaryKey( module, key );
			if ( !_cachedSettings.ContainsKey( dicKey ) ) {
				_cachedSettings[dicKey] = _Select( module, key );
			}

			string value;
			_cachedSettings.TryGetValue( dicKey, out value );

			return value;
		}

		public static List<SettingItem> GetModuleSettings( string module )
		{
			if ( !_cachedModuleSettings.ContainsKey( module ) ) {
				_cachedModuleSettings[module] = _Select( module );
			}
			return _cachedModuleSettings[module];
		}


		public static void ClearCache()
		{
			_cachedSettings = new Dictionary<string, string>();
			_cachedModuleSettings = new Dictionary<string, List<SettingItem>>();
		}

		public static void ClearCacheItem( string module, string key )
		{
			string dicKey = GetDictionaryKey( module, key );
			_cachedSettings.Remove( dicKey );
			_cachedModuleSettings.Remove( module );
		}

		private static string GetDictionaryKey( string module, string key )
		{
			return String.Format( "{0}-{1}", module, key );
		}

		private static string _Select( string module, string key )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"SELECT value FROM settings WHERE module = @module AND `key` = @key";
				command.Parameters.AddWithValue( "@module", module );
				command.Parameters.AddWithValue( "@key", key );

				object oValue = command.ExecuteScalar();
				return oValue is string ? (string)oValue : null;
			}
		}
		private static List<SettingItem> _Select( string module )
		{
			var settings = new List<SettingItem>();

			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"SELECT module, `key`, value FROM settings WHERE module = @module";
				command.Parameters.AddWithValue( "@module", module );

				using ( var dr = command.ExecuteReader() ) {
					while ( dr.Read() ) {
						settings.Add( new SettingItem {
							Module = dr.GetString( 0 ),
							Key = dr.GetString( 1 ),
							Value = dr.GetString( 2 )
						} );
					}
				}

				return settings;
			}
		}

	}
}
