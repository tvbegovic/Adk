using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using System;
using System.Globalization;

namespace DatabaseCommon
{

	public static class Settings
	{
		public static void Set( string module, string key, string value )
		{
			using ( var db = Database.Get() )
			using ( var transaction = db.BeginTransaction() ) {
				if ( _Exists( db, transaction, module, key ) ) {
					if ( value == null )
						_Delete( db, transaction, module, key );
					else
						_Update( db, transaction, module, key, value );
				}
				else {
					if ( value != null )
						_Insert( db, transaction, module, key, value );
				}

				CachedSettings.ClearCacheItem( module, key );

				transaction.Commit();
			}

		}
		public static void Set( string module, string key, bool bValue )
		{
			Set( module, key, bValue.ToString() );
		}
		public static void Set( string module, string key, int iValue )
		{
			Set( module, key, iValue.ToString() );
		}
		public static void Set( string module, string key, decimal dValue )
		{
			Set( module, key, dValue.ToString( CultureInfo.InvariantCulture ) );
		}
		public static void Set( string module, string key, DateTime? dtValue )
		{
			Set( module, key, dtValue.HasValue ? dtValue.Value.ToString( "o" ) : null );
		}

		public static string Get( string module, string key, string sDefault = null )
		{
			return CachedSettings.Get( module, key ) ?? sDefault;
		}

		public static bool Get( string module, string key, bool bDefault )
		{
			string value = Get( module, key );

			if ( value != null ) {
				if ( string.Compare( value, "true", true ) == 0 || value == "1" )
					return true;
				if ( string.Compare( value, "false", true ) == 0 || value == "0" )
					return false;
			}
			return bDefault;
		}

		public static int Get( string module, string key, int iDefault )
		{
			string value = Get( module, key );

			int iValue;
			if ( value != null && int.TryParse( value, out iValue ) )
				return iValue;

			return iDefault;
		}

		public static decimal Get( string module, string key, decimal dDefault )
		{
			string value = Get( module, key );

			decimal dValue;
			if ( value != null && decimal.TryParse(
				value,
				NumberStyles.AllowDecimalPoint,
				CultureInfo.InvariantCulture,
				out dValue ) )
				return dValue;

			return dDefault;
		}

		public static DateTime? Get( string module, string key, DateTime? dtDefault )
		{
			string value = Get( module, key );

			DateTime dtValue;
			if ( value != null && DateTime.TryParse( value, null, DateTimeStyles.RoundtripKind, out dtValue ) )
				return (DateTime?)dtValue;

			return dtDefault;
		}

		public static IEnumerable<SettingItem> GetModuleSettings( string[] modules )
		{
			var settings = new List<SettingItem>();
			for ( int i = 0; i < modules.Count(); i++ ) {
				settings.AddRange( CachedSettings.GetModuleSettings( modules[i] ) );
			}
			return settings;
		}

		private static bool _Exists( MySqlConnection db, MySqlTransaction transaction, string module, string key )
		{
			return _Select( db, transaction, module, key ) != null;
		}
		private static string _Select( MySqlConnection db, MySqlTransaction transaction, string module, string key )
		{
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT value FROM settings WHERE module = @module AND `key` = @key";
			command.Parameters.AddWithValue( "@module", module );
			command.Parameters.AddWithValue( "@key", key );

			object oValue = command.ExecuteScalar();
			return oValue is string ? (string)oValue : null;
		}
		private static void _Insert( MySqlConnection db, MySqlTransaction transaction, string module, string key, string value )
		{
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"INSERT INTO settings(module, `key`, value) VALUES (@module, @key, @value)";
			command.Parameters.AddWithValue( "@module", module );
			command.Parameters.AddWithValue( "@key", key );
			command.Parameters.AddWithValue( "@value", value );
			command.ExecuteNonQuery();
		}
		private static void _Update( MySqlConnection db, MySqlTransaction transaction, string module, string key, string value )
		{
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"UPDATE settings SET value = @value WHERE module = @module AND `key` = @key";
			command.Parameters.AddWithValue( "@module", module );
			command.Parameters.AddWithValue( "@key", key );
			command.Parameters.AddWithValue( "@value", value );
			command.ExecuteNonQuery();
		}
		private static void _Delete( MySqlConnection db, MySqlTransaction transaction, string module, string key )
		{
			var command = db.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"DELETE FROM settings WHERE module = @module AND `key` = @key";
			command.Parameters.AddWithValue( "@module", module );
			command.Parameters.AddWithValue( "@key", key );
			command.ExecuteNonQuery();
		}
	}
}