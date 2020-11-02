using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Model
{
	public class Domain
	{
		public int id { get; set; }
		public string domain { get; set; }
		public string domain_name { get; set; }
		public int? timezoneoffset { get; set; }

		public static Domain GetByFullName(string domain)
		{
			return Database.ItemFetcher("SELECT id, domain, domain_name,timezoneoffset FROM domains WHERE domain_name = @name OR domain = @name",
				dr => new Domain
				{
					id = dr.GetInt32(0),
					domain = dr.GetStringOrDefault(1),
					domain_name = dr.GetStringOrDefault(2),
					timezoneoffset = dr.GetIntOrNull(3)
				},
				"@name", domain
				);

		}

		public static List<Domain> GetAll()
		{
			return Database.ListFetcher<Domain>("SELECT id, domain, domain_name, timezoneoffset FROM domains",
				dr => new Domain
				{
					id = dr.GetInt32(0),
					domain = dr.GetStringOrDefault(1),
					domain_name = dr.GetStringOrDefault(2),
					timezoneoffset = dr.GetIntOrNull(3)
				});
		}

		public static List<Domain> GetByIds(IList<int?> ids)
		{
			return Database.ListFetcher("SELECT id, domain, domain_name,timezoneoffset FROM domains WHERE id " + Database.InClause(ids),
				dr => new Domain
				{
					id = dr.GetInt32(0),
					domain = dr.GetStringOrDefault(1),
					domain_name = dr.GetStringOrDefault(2),
					timezoneoffset = dr.GetIntOrNull(3)
				}
				);

		}

	}
}
