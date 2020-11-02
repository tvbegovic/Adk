using DatabaseCommon;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class Brand
	{
		public Guid Id;
		public string Name;

		public static List<Brand> GetAll(IList<Guid> ids = null)
		{
			return Database.ListFetcher( string.Format("SELECT id, brand_name FROM brands {0}", ids != null ? "WHERE id " + Database.InClause(ids) : "") , dr =>
				new Brand {
					Id = dr.GetGuid( 0 ),
					Name = dr.GetString( 1 )
				} );
		}
		public static Brand Get( Guid id )
		{
			return Database.ItemFetcher( "SELECT brand_name FROM brands WHERE id = @id", dr =>
				new Brand {
					Id = id,
					Name = dr.GetString( 0 )
				}, "@id", id );
		}

		public static List<Brand> SearchBrandNamesByCriteria( string criteria, int? limit = null )
		{
			using ( var conn = Database.Get() ) {
				var cmd = conn.CreateCommand();
				string limitQuery = "";
				if ( limit != null ) {
					limitQuery = String.Format( "LIMIT {0}", limit );
				}

				string query =
					 string.Format(
						  @"SELECT id, brand_name
                            FROM brands
                            WHERE brand_name LIKE @criteria                    
                            ORDER BY brand_name ASC
                            {0}", limitQuery );

				cmd.Parameters.AddWithValue( "@criteria", "%" + criteria + "%" );

				return Database.ListFetcher( query, dr => new Brand() {
					Id = dr.GetGuid( 0 ),
					Name = dr.GetString( 1 )
				}, "@criteria", "%" + criteria + "%" );

			}
		}
	}
}
