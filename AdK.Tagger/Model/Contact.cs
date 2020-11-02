using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Model
{
	public class Contact
	{
		public int contact_id { get; set; }
		public string email { get; set; }
		public int? client_id { get; set; }
		public Guid? user_id { get; set; }
		public string name { get; set; }

		public TaggerUser User { get; set; }

		public Client Client { get; set; }

		public List<FeedFilter> FeedFilters { get; set; }

		public static Contact GetForUser(string userId)
		{
			return Database.ItemFetcher(
				@"SELECT contacts.contact_id, contacts.email, users.email, users.username, contacts.name
				FROM contacts INNER JOIN users ON contacts.user_id = users.id 
				WHERE user_id = @id",
				dr => new Contact
				{
					contact_id = dr.GetInt32(0),
					email = dr.GetStringOrDefault(1),
					name = dr.GetStringOrDefault(4),
					User = new TaggerUser {
						Email = dr.GetStringOrDefault(2),
						Name = dr.GetStringOrDefault(3)
					}
				},
				"@id", userId
				);
		}

		public static List<Contact> GetForClient(int client_id)
		{
			return Database.ListFetcher(@"SELECT contact_id, client_id, contacts.user_id, contacts.email, contacts.name
							FROM contacts 							
							WHERE client_id = @client_id",
				dr => new Contact
				{
					contact_id = dr.GetInt32(0),
					client_id = dr.GetIntOrNull(1),
					user_id = dr.GetGuidOrNull(2),
					email = dr.GetStringOrDefault(3),
					name = dr.GetStringOrDefault(4)
				},
				"@client_id",client_id);
		}

		public static List<Contact> SearchContacts(string text)
		{
			return Database.ListFetcher(@"SELECT contact_id, client_id, contacts.user_id, contacts.email, contacts.name
							FROM contacts 							
							WHERE email LIKE @text",
				dr => new Contact
				{
					contact_id = dr.GetInt32(0),
					client_id = dr.GetIntOrNull(1),
					user_id = dr.GetGuidOrNull(2),
					email = dr.GetStringOrDefault(3),
					name = dr.GetStringOrDefault(4)
				},
				"@text", "%" + text + "%");
		}

		public static Contact Get(int id)
		{
			return Database.ItemFetcher("SELECT contact_id, client_id,user_id, email,name FROM clients WHERE contact_id = @id",
				dr => new Contact
				{
					contact_id = dr.GetInt32(0),
					client_id = dr.GetIntOrNull(1),
					user_id = dr.GetGuidOrNull(2),
					email = dr.GetStringOrDefault(3),
					name = dr.GetStringOrDefault(4)
				},
				"@id", id
				);
		}

		public static void Create(Contact c)
		{
			
			c.contact_id = (int)Database.Insert(
				@"INSERT INTO contacts (user_id, client_id, email,name) VALUES (@user_id, @client_id,@email,@name)",
				"@user_id", c.user_id,
				"@client_id",c.client_id,
				"@email", c.email,
				"@name", c.name
			);
		}

		public static void Update(Contact c)
		{
			
			Database.ExecuteNonQuery("UPDATE contacts SET user_id = @user_id, client_id = @client_id,email = @email, name = @name WHERE contact_id = @id",
				"@user_id", c.user_id, 
				"@client_id",c.client_id,
				"@email", c.email,
				"@name", c.name,
				"@id", c.contact_id);
		}

		public static void Delete(int id)
		{
			Database.Delete("DELETE FROM contacts_feedfilter WHERE contact_id = @id", "@id",id);
			Database.Delete("DELETE FROM feed_report_contact WHERE contact_id = @id", "@id", id);
			Database.Delete("DELETE FROM contacts WHERE contact_id = @id", "@id", id);
		}

		public static void DeleteMultiple(IList<int> ids)
		{
			Database.Delete("DELETE FROM contacts_feedfilter WHERE contact_id " + Database.InClause(ids));
			Database.Delete("DELETE FROM feed_report_contact WHERE contact_id " + Database.InClause(ids));
			Database.Delete("DELETE FROM contacts WHERE contact_id " + Database.InClause(ids));
		}

		public static void AddFeed(int contact_id, int feedFilterId)
		{
			AddFeeds(contact_id, new[] { feedFilterId });
		}

		public static void AddFeeds(int contact_id, IList<int> feedFilterIds)
		{
			var sql = "INSERT INTO contacts_feedfilter(contact_id, feed_filter_id) VALUES (@contact_id, @feed_filter_id)";
			using (var conn = Database.Get())
			{
				var tr = conn.BeginTransaction();
				try
				{
					foreach (var fId in feedFilterIds)
					{
						Database.Insert(conn, tr, sql, "@contact_id", contact_id, "@feed_filter_id", fId);
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

		public static void RemoveFeeds(int contact_id, IList<int> feedFilterIds)
		{
			var sql = "DELETE FROM contacts_feedfilter WHERE contact_id = @contact_id AND  feed_filter_id = @feed_filter_id";
			using (var conn = Database.Get())
			{
				var tr = conn.BeginTransaction();
				try
				{
					foreach (var fId in feedFilterIds)
					{
						Database.Delete(conn, tr, sql, "@contact_id", contact_id, "@feed_filter_id", fId);
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

		public static void RemoveFeed(int contact_id, int feedFilterId)
		{
			Database.Delete("DELETE FROM contacts_feedfilter WHERE contact_id = @contact_id AND feed_filter_id = @feed_filter_id", "@contact_id", contact_id, "@feed_filter_id", feedFilterId);
		}

		public static object GetSearchPhrasesForContact(int contact_id)
		{
			return Database.ListFetcher<ClientSearchTerm>("SELECT * FROM contact_searches WHERE contact_id = @id", "@id", contact_id);
		}

		public static List<string> GetUniquePhrasesForContacts()
		{
			return Database.ListFetcher<string>("SELECT DISTINCT contact_searches.search_term FROM contact_searches", dr=> { return dr.GetString(0); }).ToList();
		}

		public static object AddSearchPhraseToContact(ContactSearchTerm cst)
		{
			return Database.Insert("INSERT INTO contact_searches(contact_id, search_term) VALUES(@contact_id, @term)", "@contact_id", cst.contact_id, "@term", cst.search_term);
		}

		public static void RemoveSearchPhraseFromContact(int id)
		{
			Database.ExecuteNonQuery("DELETE FROM contact_searches WHERE id = @id", "@id", id);
		}
	}

	public class ContactSearchTerm
	{
		public int id { get; set; }
		public int? contact_id { get; set; }
		public string search_term { get; set; }
	}
}
