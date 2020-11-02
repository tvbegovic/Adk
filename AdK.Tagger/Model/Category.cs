using DatabaseCommon;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class Category
	{
		public Guid Id { get; set; }
		public Guid IndustryId { get; set; }
		public string Name { get; set; }

		public static List<Category> GetAll(IList<Guid> ids = null)
		{
			using ( var connection = Database.Get() ) {
				var command = connection.CreateCommand();
				command.CommandText = string.Format(@"SELECT id, category_name, industry_id FROM ad_categories {0}", ids != null ? "WHERE id " + Database.InClause(ids) : "" );

				var categories = new List<Category>();

				using ( var dr = command.ExecuteReader() ) {
					while ( dr.Read() )
						categories.Add( new Category {
							Id = dr.GetGuid( 0 ),
							Name = dr.GetString( 1 ),
							IndustryId = dr.GetGuid( 2 )
						} );
				}

				return categories;
			}
		}
	}
}
