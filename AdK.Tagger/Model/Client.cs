using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Model
{
	public class Client
	{
		public int id { get; set; }
		public string name { get; set; }

		public List<FeedFilter> feeds { get; set; }

		public static List<Client> GetAll()
		{
			var clients = new List<Client>();
			Database.ListFetcher(
				@"SELECT clients.id, clients.name, feed_filters.id, feed_filters.client, feed_filters.include_mp3, feed_filters.timestamp 
				FROM feed_filters INNER JOIN clients_feedfilter ON feed_filters.id = clients_feedfilter.feed_filter_id
				RIGHT OUTER JOIN clients ON clients.id = clients_feedfilter.client_id WHERE COALESCE(feed_filters.deleted,0) = 0",
				dr => {
					var id = dr.GetInt32(0);
					var client = clients.FirstOrDefault(c => c.id == id);
					if(client == null)
					{
						client = new Client
						{
							id = dr.GetInt32(0),
							name = dr.GetStringOrDefault(1),
							feeds = new List<FeedFilter>()
						};
						clients.Add(client);
					}
					var feed_id = dr.GetIntOrNull(2);
					if(feed_id != null)
					{
						var feed = new FeedFilter
						{
							Id = dr.GetInt32(2),
							ClientName = dr.GetStringOrDefault(3),
							IncludeMp3 = dr.GetBoolean(4),
							Timestamp = dr.GetDateOrNull(5)
						};
						client.feeds.Add(feed);
					}
					return client;
				});
			return clients;
		}

		public static Client Get(int id)
		{
			return Database.ItemFetcher("SELECT id,name FROM clients WHERE id = @id",
				dr => new Client
				{
					id = dr.GetInt32(0),
					name = dr.GetStringOrDefault(1)
				},
				"@id", id
				);
		}

		public static void Create(Client c)
		{
			c.id = (int)Database.Insert(
				@"INSERT INTO clients (name) VALUES (@name)",
				"@name", c.name
			);
		}

		public static void Update(Client c)
		{
			Database.ExecuteNonQuery("UPDATE clients SET name = @name WHERE id = @id", "@name", c.name, "@id", c.id);
		}

		public static void Delete(int id)
		{
			Database.Delete("DELETE FROM clients_feedfilter WHERE client_id = @id", "@id");
			Database.Delete("DELETE FROM contacts_feedfilter WHERE contact_id IN (SELECT contact_id FROM contacts WHERE client_id = @id)", "@id", id);
			Database.Delete("DELETE FROM contacts WHERE client_id = @id", "@id", id);
			Database.Delete("DELETE FROM clients WHERE id = @id", "@id", id);
		}

		public static void DeleteMultiple(IList<int> ids)
		{
			Database.Delete(string.Format("DELETE FROM contacts_feedfilter WHERE contact_id IN (SELECT contact_id FROM contacts WHERE client_id {0} )", Database.InClause(ids)));
			Database.Delete("DELETE FROM clients_feedfilter WHERE client_id " + Database.InClause(ids));
			Database.Delete("DELETE FROM contacts WHERE client_id " + Database.InClause(ids));
			Database.Delete("DELETE FROM clients WHERE id " + Database.InClause(ids));
		}

		public static void AddFeeds(int client_id, IList<int> feedFilterIds)
		{
			var sql = "INSERT INTO clients_feedfilter(client_id, feed_filter_id) VALUES (@client_id, @feed_filter_id)";
			using (var conn = Database.Get())
			{
				var tr = conn.BeginTransaction();
				try
				{
					foreach (var fId in feedFilterIds)
					{
						Database.Insert(conn, tr, sql, "@client_id", client_id, "@feed_filter_id", fId);
					}
					tr.Commit();
				}
				catch (Exception)
				{
					tr.Rollback();
					throw;
				}

			}
		}

		public static void RemoveFeeds(int client_id, IList<int> feedFilterIds)
		{
			var sql = "DELETE FROM clients_feedfilter WHERE client_id = @client_id AND  feed_filter_id = @feed_filter_id";
			using (var conn = Database.Get())
			{
				var tr = conn.BeginTransaction();
				try
				{
					foreach (var fId in feedFilterIds)
					{
						Database.Delete(conn, tr, sql, "@client_id", client_id, "@feed_filter_id", fId);
					}
					sql = "DELETE FROM contacts_feedfilter WHERE contact_id IN (SELECT contact_id FROM contacts WHERE client_id = @client_id) AND feed_filter_id = @feed_filter_id";
					foreach (var fId in feedFilterIds)
					{
						Database.Delete(conn, tr, sql, "@client_id", client_id, "@feed_filter_id", fId);
					}
					tr.Commit();
				}
				catch (Exception)
				{
					tr.Rollback();
					throw;
				}

			}
		}

		public static List<ClientSearchTerm> GetSearchPhrasesForClient(int client_id)
		{
			var search = Database.ListFetcher<ClientSearchTerm>(
				@"SELECT client_searches.id, client_id, IF(common_search.id IS NOT NULL, common_search.name, client_searches.search_term) as search_term, client_searches.common_id
				FROM client_searches LEFT OUTER JOIN common_search ON client_searches.common_id = common_search.id
				WHERE client_id = @id",	"@id", client_id);
			foreach(var s in search)
			{
				if (s.common_id > 0)
				{
					s.Common = new CommonSearch { id = s.common_id.Value, name = s.search_term };
					s.Common.Terms = Database.ListFetcher<CommonSearchTerm>("SELECT * FROM common_search_tag WHERE common_id = @id", "@id", s.common_id);
				}
				else
					s.Common = null;
				
			}
			return search;
		}

		public static List<string> GetUniquePhrasesForClients()
		{
			return Database.ListFetcher<string>("SELECT DISTINCT client_searches.search_term FROM client_searches",dr=> { return dr.GetString(0); }).ToList();
		}

		public static ClientSearchTerm AddSearchPhraseToClient(ClientSearchTerm cst)
		{
			cst.id = Convert.ToInt32(Database.Insert("INSERT INTO client_searches(client_id, search_term, common_id) VALUES(@client_id, @term,@common_id)", "@client_id", cst.client_id, "@term", cst.search_term, "@common_id", cst.common_id));
			if(cst.common_id != null && cst.Common == null)
			{
				cst.Common = Database.ItemFetcher<CommonSearch>("SELECT * FROM common_search WHERE id = @id", "@id", cst.common_id);
				cst.Common.Terms = Database.ListFetcher<CommonSearchTerm>("SELECT * FROM common_search_tag WHERE common_id = @id", "@id", cst.common_id);
			}
			return cst;
		}

		public static void RemoveSearchPhraseFromClient(int id)
		{
			Database.ExecuteNonQuery("DELETE FROM client_searches WHERE id = @id", "@id", id);
		}

		public static List<CommonSearch> GetCommonTerms()
		{
			var common = Database.ListFetcher<CommonSearch>("SELECT common_search.* FROM common_search");
			var terms = Database.ListFetcher<CommonSearchTerm>("SELECT common_search_tag.* FROM common_search_tag");
			foreach (var c in common)
				c.Terms = terms.Where(t => t.common_id == c.id).ToList();
			return common;
		}

		public static ClientSearchTerm UpdateSearchTerm(ClientSearchTerm term)
		{
			var newCommon = false;
			if(term.common_id == null && term.Common != null && term.Common.Terms.Count > 0)
			{
				newCommon = true;
				term.common_id = Convert.ToInt32(Database.Insert("INSERT INTO common_search(name) VALUES(@name)", "@name", term.search_term));
				term.Common.id = term.common_id.Value;
				term.Common.name = term.search_term;
			}
			Database.ExecuteNonQuery("UPDATE client_searches SET search_term = @term, common_id = @common_id WHERE id = @id","@id", term.id, "@term", term.search_term,"@common_id", term.common_id);
			if(term.Common != null && term.Common.Terms.Count > 0)
			{
				List<CommonSearchTerm> oldTerms = new List<CommonSearchTerm>();
				if(!newCommon)
				{
					oldTerms = Database.ListFetcher<CommonSearchTerm>("SELECT common_search_tag.* FROM common_search_tag WHERE common_id = @id", "@id", term.common_id);
					var toRemove = oldTerms.Where(t => term.Common.Terms.Count(x => x.id == t.id) == 0).Select(t=>t.id).ToList();
					if(toRemove.Count > 0)
						Database.ExecuteNonQuery(string.Format("DELETE FROM common_search_tag WHERE id {0}", Database.InClause(toRemove)));
				}
				var newTerms = term.Common.Terms.Where(x => x.id <= 0).ToList();
				foreach(var t in newTerms)
				{
					t.id = Convert.ToInt32(Database.Insert("INSERT INTO common_search_tag (name, common_id) VALUES(@name,@common_id)", "@name", t.name,"@common_id", term.common_id));
				}
								
			}
			return term;
		}
	}

	public class ClientSearchTerm
	{
		public int id { get; set; }
		public int? client_id { get; set; }
		public string search_term { get; set; }
		public int? common_id { get; set; }
		public CommonSearch Common { get; set; }
	}

	public class CommonSearch
	{
		public int id { get; set; }
		public string name { get; set; }
		public List<CommonSearchTerm> Terms { get; set; }
	}

	public class CommonSearchTerm
	{
		public int id { get; set; }
		public int? common_id { get; set; }
		public string name { get; set; }
	}
}
