using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AdK.Tagger.Model
{
	public class Brandvertiser
	{
		public Guid Id;
		public string Name;
		public bool IsBrand;

		public static Brandvertiser Get(Guid id, bool isBrand)
		{
			var brandvertiser = new Brandvertiser
			{
				Id = id,
				IsBrand = isBrand
			};

			brandvertiser.Name = isBrand ?
				Brand.Get(id).Name :
				Advertiser.Get(id).Name;

			return brandvertiser;
		}

		public List<Brand> GetBrands()
		{
			if (IsBrand)
				return new List<Brand> { new Brand { Id = Id, Name = Name } };
			else
				return Advertiser.GetBrands(Id);
		}
	}
	public class BrandvertiserPage
	{
		public List<Brandvertiser> Brandvertisers;
		public bool Brands;
		public bool Advertisers;
		public string Filter;
		public int TotalCount;
		public int PageNum;
		public int PageSize;

		public BrandvertiserPage(bool brands, bool advertisers, int pageNum, int pageSize, string filter=null)
		{
			Debug.Assert((brands || advertisers) && pageNum >= 0 && pageSize > 0);
			Brands = brands;
			Advertisers = advertisers;
			Filter = filter;
			PageNum = pageNum;
			PageSize = pageSize;
		}
		public void Count()
		{
			string query = "SELECT ";
			if (Brands)
			{
				query += "(SELECT COUNT(*) FROM brands";
				if (!string.IsNullOrWhiteSpace(Filter))
					query += " WHERE brand_name LIKE @filter";
				query += ")";
			}
			if (Brands && Advertisers)
				query += " + ";
			if (Advertisers)
			{
				query += "(SELECT COUNT(*) FROM advertisers";
				if (!string.IsNullOrWhiteSpace(Filter))
					query += " WHERE company_name LIKE @filter";
				query += ")";
			}

			TotalCount = string.IsNullOrWhiteSpace(Filter) ? Database.Count(query) : Database.Count(query, "@filter", "%" + Filter + "%");
		}
		public void Load()
		{
			string query = "";
			if (Brands)
			{
				query += "SELECT id, brand_name, 1 FROM brands";
				if (!string.IsNullOrWhiteSpace(Filter))
					query += " WHERE brand_name LIKE @filter";
			}
			if (Brands && Advertisers)
				query += " UNION ALL ";
			if (Advertisers)
			{
				query += "SELECT id, company_name, 0 FROM advertisers";
				if (!string.IsNullOrWhiteSpace(Filter))
					query += " WHERE company_name LIKE @filter";
			}
			query += " ORDER BY " + (Brands ? "brand_name" : "company_name") + " LIMIT @start,@length";

			Brandvertisers = new List<Brandvertiser>();
			using (var conn = Database.Get())
			{
				var cmd = conn.CreateCommand();
				cmd.CommandText = query;
				if (!string.IsNullOrWhiteSpace(Filter))
					cmd.Parameters.AddWithValue("@filter", "%" + Filter + "%");
				cmd.Parameters.AddWithValue("@start", PageSize * PageNum);
				cmd.Parameters.AddWithValue("@length", PageSize);

				using (var dr = cmd.ExecuteReader())
				{
					while (dr.Read())
					{
						Brandvertisers.Add(new Brandvertiser
						{
							Id = dr.GetGuid(0),
							Name = dr.GetString(1),
							IsBrand = dr.GetInt32(2) == 1
						});
					}
				}
			}
		}
	}
}