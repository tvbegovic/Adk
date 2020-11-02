using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
	public class Company
	{
		public Guid Id;
		public string Name;

		public static List<Company> GetAll()
		{
			return Database.ListFetcher<Company>("SELECT id, company_name FROM advertisers", dr => new Company
				{
					Id = dr.GetGuid(0),
					Name = dr.GetString(1)
				}
			);
		}
		public static List<Company> Get(IEnumerable<Guid> channelIds)
		{
			if (channelIds.Any())
				return Database.ListFetcher<Company>(
					@"SELECT id, company_name FROM advertisers WHERE id " + Database.InClause(channelIds),
					dr => new Company
					{
						Id = dr.GetGuid(0),
						Name = dr.GetString(1)
					}
				);

			return new List<Company>();
		}
	}
}