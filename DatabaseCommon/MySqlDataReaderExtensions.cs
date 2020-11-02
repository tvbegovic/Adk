using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using MySql.Data.MySqlClient;

namespace DatabaseCommon
{
	public static class MySqlDataReaderExtensions
	{
		public static string GetStringOrDefault( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? "" : dr.GetString( columnIndex );
		}

		public static int GetIntOrDefault( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? 0 : dr.GetInt32( columnIndex );
		}

		public static int? GetIntOrNull( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? (int?)null : dr.GetInt32( columnIndex );
		}

		public static double GetDoubleOrDefault( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? 0 : dr.GetDouble( columnIndex );
		}

		public static double? GetDoubleOrNull( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? (double?)null : dr.GetDouble( columnIndex );
		}

		public static decimal GetDecimalOrDefault( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? 0 : dr.GetDecimal( columnIndex );
		}

		public static decimal? GetDecimalOrNull( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? (decimal?)null : dr.GetDecimal( columnIndex );
		}

		public static bool GetBoolOrDefault( this MySqlDataReader dr, int columnIndex )
		{
			return !dr.IsDBNull( columnIndex ) && dr.GetBoolean( columnIndex );
		}

		public static bool? GetBoolOrNull( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? (bool?)null : dr.GetBoolean( columnIndex );
		}

		public static DateTime GetDateOrDefault( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? DateTime.MinValue : dr.GetDateTime( columnIndex );
		}

		public static DateTime? GetDateOrNull( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? (DateTime?)null : dr.GetDateTime( columnIndex );
		}

		public static Guid GetGuidOrDefault( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? Guid.Empty : dr.GetGuid( columnIndex );
		}

		public static Guid? GetGuidOrNull( this MySqlDataReader dr, int columnIndex )
		{
			return dr.IsDBNull( columnIndex ) ? (Guid?)null : dr.GetGuid( columnIndex );
		}

		private static Dictionary<string, int> _GetReaderNameIndexDictionary( MySqlDataReader dr )
		{
			var nameIndexes = new Dictionary<string, int>();
			for ( int i = 0; i < dr.FieldCount; i++ ) {
				nameIndexes.Add( dr.GetName( i ).ToLower(), i );
			}
			return nameIndexes;
		}


		/// <summary>
		/// Automatically populate all available field from MySqlDataReader row to the provided class property. 
		/// Map is done by the property name. If property name is different then db property DataAttribute 
		/// ColumnNameAttr can be used e.g [ColumnNameAttr("customBindingName")]
		/// </summary>
		public static T ReadSingleRow<T>( this MySqlDataReader dr, Dictionary<string, int> nameIndexes = null )
			where T : class, new()
		{
			if ( dr.Read() ) {
				return dr.BindSingleRow<T>();
			}

			return new T();
		}


		/// <summary>
		/// Get the list of provided objects from data table. 
		/// Iterate each row and call bind data on the row. 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="dt"></param>
		/// <returns></returns>
		public static List<T> ReadList<T>( this MySqlDataReader dr ) where T : class
		{
			var list = new List<T>();

			Dictionary<string, int> nameIndexes = null;


			while ( dr.Read() ) {
				if ( nameIndexes == null ) {
					nameIndexes = _GetReaderNameIndexDictionary( dr );
				}

				list.Add( dr.BindSingleRow<T>( nameIndexes ) );
			}

			return list;
		}


		public static T BindSingleRow<T>( this MySqlDataReader dr, Dictionary<string, int> nameIndexes = null ) where T : class
		{
			var obj = Activator.CreateInstance<T>();

			if ( nameIndexes == null ) {
				nameIndexes = _GetReaderNameIndexDictionary( dr );
			}

			// Get all properties
			var properties = typeof( T ).GetProperties();

			foreach ( var prop in properties ) {
				if ( prop.CanWrite ) {
					object[] attrs = prop.GetCustomAttributes( true );
					string columnName = prop.Name;

					foreach ( object attr in attrs ) {
						var columnNameAttr = attr as ColumnNameAttr;
						if ( columnNameAttr != null ) {
							columnName = columnNameAttr.Name;
						}
					}

					columnName = columnName.ToLower();
					//no database column, set default type value for column and break execution
					if ( !nameIndexes.ContainsKey( columnName ) ) {
						prop.SetValue( obj, Activator.CreateInstance( prop.PropertyType ), null );
						continue;
					}

					int columnIndex = nameIndexes[columnName];

					if ( prop.PropertyType == typeof( int ) )
						prop.SetValue( obj, dr.GetIntOrDefault( columnIndex ), null );
                    if (prop.PropertyType == typeof(long))
                        prop.SetValue(obj, dr.GetInt64(columnIndex), null);
                    if ( prop.PropertyType == typeof( int? ) )
						prop.SetValue( obj, dr.GetIntOrNull( columnIndex ), null );
					else if ( prop.PropertyType == typeof( double ) )
						prop.SetValue( obj, dr.GetDoubleOrDefault( columnIndex ), null );
					else if ( prop.PropertyType == typeof( double? ) )
						prop.SetValue( obj, dr.GetDoubleOrNull( columnIndex ), null );
					else if ( prop.PropertyType == typeof( decimal ) )
						prop.SetValue( obj, dr.GetDecimalOrDefault( columnIndex ), null );
					else if ( prop.PropertyType == typeof( decimal? ) )
						prop.SetValue( obj, dr.GetDecimalOrNull( columnIndex ), null );
					else if ( prop.PropertyType == typeof( string ) )
						prop.SetValue( obj, dr.GetStringOrDefault( columnIndex ), null );
					else if ( prop.PropertyType == typeof( bool ) )
						prop.SetValue( obj, dr.GetBoolOrDefault( columnIndex ), null );
					else if ( prop.PropertyType == typeof( bool? ) )
						prop.SetValue( obj, dr.GetBoolOrNull( columnIndex ), null );
					else if ( prop.PropertyType == typeof( DateTime ) )
						prop.SetValue( obj, dr.GetDateOrDefault( columnIndex ), null );
					else if ( prop.PropertyType == typeof( DateTime? ) )
						prop.SetValue( obj, dr.GetDateOrNull( columnIndex ), null );
					else if ( prop.PropertyType == typeof( Guid ) )
						prop.SetValue( obj, dr.GetGuidOrDefault( columnIndex ), null );
					else if ( prop.PropertyType == typeof( Guid? ) )
						prop.SetValue( obj, dr.GetGuidOrNull( columnIndex ), null );
				}
			}

			return obj;
		}


	}
}


