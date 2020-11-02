using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
	public class Advertiser
	{
		public Guid Id;
		public string Name;


        public static List<Advertiser> GetAll(IList<Guid> ids = null)
        {
			return Database.ListFetcher(string.Format("SELECT id, company_name FROM advertisers {0}", ids != null ? "WHERE id " + Database.InClause(ids) : "" ), dr =>
                new Advertiser
                {
                    Id = dr.GetGuid(0),
                    Name = dr.GetString(1)
                });
        }
		public static Advertiser Get(Guid id)
		{
			return Database.ItemFetcher("SELECT company_name FROM advertisers WHERE id = @id", dr =>
				new Advertiser
				{
					Id = id,
					Name = dr.GetString(0)
				}, "@id", id);
		}
		public static List<Brand> GetBrands(Guid advertiserId)
		{
			return Database.ListFetcher("SELECT id, brand_name FROM brands WHERE advertiser_id = @advertiserId", dr =>
				new Brand
				{
					Id = dr.GetGuid(0),
					Name = dr.GetString(1)
				}, "@advertiserId", advertiserId);
		}
	}
}
