using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.HSSF.UserModel;
using System.IO;
using System.Data;
using AdK.Tagger.Model.AppSettings;
using MySql.Data.MySqlClient;
using System.Threading;

namespace AdK.Tagger.Model
{
    public class FeedResultItem
    {
        public FeedResultItem()
        {
            Channels = new List<FeedResultItemChannel>();
        }

        public string AdvertiserId { get; set; }
        public string AdvertiserName { get; set; }
        public string BrandId { get; set; }
        public string BrandName { get; set; }
        public string AdTranscriptId { get; set; }
        public string AdTranscript { get; set; }
        public string RegionsIds { get; set; }
        public string MarketsIds { get; set; }
        public string Regions { get; set; }
        public string Markets { get; set; }
        public string IndustryId { get; set; }
        public string IndustryName { get; set; }
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<FeedResultItemChannel> Channels { get; set; }
        public string Mp3Url { get; set; }
        public decimal? Duration { get; set; }
        public string Mp3Id { get; set; }
        public DateTime? SongCreated { get; set; }
        public string SongTitle { get; set; }
        public string MediaType { get; set; }
		public int? FeedFilterId { get; set; }
		public bool? SongExcluded { get; set; }
		public Guid? SongId { get; set; }
    }

    public class RawFeedResultItem
    {
        public string AdvertiserId { get; set; }
        public string AdvertiserName { get; set; }
        public string BrandId { get; set; }
        public string BrandName { get; set; }
        public string AdTranscriptId { get; set; }
        public string AdTranscript { get; set; }
        public string RegionsIds { get; set; }
        public string MarketsIds { get; set; }
        public string Regions { get; set; }
        public string Markets { get; set; }
        public string IndustryId { get; set; }
        public string IndustryName { get; set; }
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string ChannelId { get; set; }
        public string ChannelName { get; set; }
        public DateTime? ChannelFirstAiringDateTime { get; set; }
        public string MP3PKSID { get; set; }
        public decimal? Duration { get; set; }
        public string MediaType { get; set; }
        public string Title { get; set; }
		public bool? SongExcluded { get; set; }
		public Guid? SongId { get; set; }

		public IList<int?> MarketIdsList {get;set;}
		public IList<int?> DomainIdsList {get;set;}
    }

	public class RawFeedUngroupedResult
	{
		public string AdvertiserId { get; set; }
        public string BrandId { get; set; }
        public string IndustryId { get; set; }        
        public string CategoryId { get; set; }
        public string ChannelId { get; set; }
		public double? MatchStart {get;set;}
		public double? MatchEnd {get;set;}
        public DateTime? MatchOccured {get;set;}
		public string MP3PKSID { get; set; }
        public decimal? Duration { get; set; }
		public string SongTitle {get;set;}
        public decimal? MatchThreshold {get;set;}		
		public Guid? SongId { get; set; }
		public string MediaType {get;set;}
		public int? MarketId {get;set;}
		public int? DomainId {get;set;}
		public bool? SongExcluded { get; set; }
		public bool Valid
		{
			get
			{
				if(MatchStart == null || MatchEnd == null)
					return false;
				return (MatchEnd.Value - MatchStart.Value) > Convert.ToDouble(Duration * MatchThreshold);
			}
		}
	}

    public class FeedResultItemChannel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime? FirstAiringDateTime { get; set; }
        public string FirstAiring { get { return FirstAiringDateTime.HasValue ? FirstAiringDateTime.Value.ToString("s") : null; } }
    }

    public class AjaxDomain
    {
        public string Domain { get; set; }
        public string DisplayName { get; set; }
    }

    public class AjaxFeedFilter
    {
        //public string advertiserBrand { get; set; }
        public string brand { get; set; }
        public string channel { get; set; }
        public string regionMarket { get; set; }
        public string firstAiring { get; set; }
        public string adTranscript { get; set; }
    }

    public class FeedResultSongDetails
    {
        public string Mp3Url { get; set; }
        public string VideoUrl { get; set; }
        public decimal? Duration { get; set; }
        public string Title { get; set; }
        public string Advertiser { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public string Industry { get; set; }
        public string Region { get; set; }
        public string Market { get; set; }
        public DateTime? Created { get; set; }
        public string FullTranscript { get; set; }
        public List<FeedResultItemChannel> Channels { get; set; }
    }

	public class FeedEmails
	{
		public int id { get; set; }
		public string email { get; set; }
		public int? contact_id { get; set; }
		public Contact Contact;
	}

    public class FeedFilter
    {
        public FeedFilter()
        {
            FilterGroups = new List<FeedFilterRuleGroup>();
        }
        public long Id { get; set; }
        public string ClientName { get; set; }
        public bool IncludeMp3 { get; set; }
        public DateTime? Timestamp { get; set; }		
        public DateTime? LastEmailSent { get; set; }
		public DateTime? ExpirationDate { get; set; }

        public List<Contact> MailingList { get; set; }

        public List<FeedFilterRuleGroup> FilterGroups { get; set; }
    }
    public class FeedFilterRuleGroup//or RuleGroup, as it's named in the DB
    {
        public long Id { get; set; }
        public long FeedFilterId { get; set; }
        public bool Exclude { get; set; }

        public List<FeedFilterRule> FeedFilterRulesMarkets { get; set; }
        public List<FeedFilterRule> FeedFilterRulesDomains { get; set; }
        public List<FeedFilterRule> FeedFilterRulesIndustries { get; set; }
        public List<FeedFilterRule> FeedFilterRulesCategories { get; set; }
        public List<FeedFilterRule> FeedFilterRulesAdvertisers { get; set; }
        public List<FeedFilterRule> FeedFilterRulesBrands { get; set; }
    }

    public class FeedFilterRuleGroupRow
    {
        public long Id { get; set; }
        public long FeedFilterId { get; set; }
        public bool Exclude { get; set; }

        public string FeedFilterRulesMarket { get; set; }
        public string FeedFilterRulesDomain { get; set; }
        public string FeedFilterRulesIndustry { get; set; }
        public string FeedFilterRulesCategory { get; set; }
        public string FeedFilterRulesAdvertiser { get; set; }
        public string FeedFilterRulesBrand { get; set; }
    }

    public class Feed
    {
        public long Id { get; set; }
        public string Client { get; set; }
        public string Market { get; set; }
        public string Domain { get; set; }
        public string EmailSent { get; set; }
        public string LastTimestamp { get; set; }
		
		public int? NewMatchCount { get; set; }
		public string ExpirationDate { get; set; }
	}

    public class FeedFilterRule
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
    }

	public class FeedReport
	{
		public int Id { get; set; }
		public string reportId { get; set; }
		public long? FeedFilterId { get; set; }
		public DateTime? TimeInserted { get; set; }
		public bool? IncludeMp3 { get; set; }
		public DateTime? FeedTimeStamp { get; set; }

		public int? ItemCount { get; set; }

		public List<FeedResultEmailItem> Items { get; set; }
		public List<Contact> Contacts { get; set; }
	}

    public class FeedResultEmailItem
    {
        public string ReportId { get; set; }
        public long FeedFilterId { get; set; }
        public string ClientId { get; set; }
        public DateTime? Timestamp { get; set; }
        public string AdvertiserId { get; set; }
        public string BrandId { get; set; }
        public string AdTranscriptId { get; set; }
        public string RegionsIds { get; set; }
        public string MarketsIds { get; set; }
        public string IndustryId { get; set; }
        public string AdCategoryId { get; set; }
        public string Channel1Id { get; set; }
        public DateTime? FirstAiring1 { get; set; }
        public string Channel2Id { get; set; }
        public DateTime? FirstAiring2 { get; set; }
        public string Channel3Id { get; set; }
        public DateTime? FirstAiring3 { get; set; }
        public bool IncludeMp3 { get; set; }
        public string PKSID { get; set; }
        public bool SentInEmail { get; set; }
        public DateTime TimeInserted { get; set; }
    }

	public class FeedResultEmailInfo
	{
		public string ReportId { get; set; }
		public DateTime? TimeInserted { get; set; }
		public int? AdCount { get; set; }
	}

    public class Feeds
    {
		public const int MaxChannelsFirstAiring = 1;

		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		public static List<Feed> GetFeeds()
        {
            var query = @"SELECT ff.id, ff.client, m.name, fre.last_email, ff.timestamp, GROUP_CONCAT(DISTINCT d.domain_name ORDER BY d.domain_name SEPARATOR ', '),
							feed_newmatchcount.new_matches, ff.expiration_date
                            from feed_filters as ff
                            left join rule_groups as rg on ff.id = rg.feed_filter_id
                            left join rule_markets as rm on rg.id = rm.rule_group_id
                            left join rule_domains as rd on rg.id = rd.rule_group_id
                            left join domains as d on rd.domain = d.domain
                            left join markets as m on rm.market_id = m.id
                            left join ( SELECT feed_filter_id, MAX(time_inserted) as last_email FROM feed_report group by feed_filter_id) 
							as fre on fre.feed_filter_id = ff.id
							left join feed_newmatchcount on ff.id = feed_newmatchcount.feed_id
							where ff.id > 0 AND COALESCE(ff.deleted,0) = 0
							group by ff.client;";

            var feedList = Database.ListFetcher<Feed>(query, dr => new Feed
            {
                Id = dr.GetInt32(0),
                Client = dr.GetStringOrDefault(1),
                Market = dr.GetStringOrDefault(2),
                EmailSent = dr.GetDateOrNull(3).HasValue ? dr.GetDateOrNull(3).Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                LastTimestamp = dr.GetDateOrNull(4).HasValue ? dr.GetDateOrNull(4).Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                Domain = dr.GetStringOrDefault(5),
				NewMatchCount = dr.GetIntOrNull(6),
				ExpirationDate = dr.GetDateOrNull(7).HasValue ? dr.GetDateOrNull(7).Value.ToString("yyyy-MM-dd HH:mm:ss") : null
			});

            return feedList;
        }

        public static long CreateFeed(string client, bool includeMp3, DateTime? expirationDate/*, IEnumerable<FeedEmails> mailingList*/)
        {
            var feedId = Database.Insert(@"INSERT INTO feed_filters (client, include_mp3, timestamp, deleted ,expiration_date) 
                                        VALUES (@client, @includeMp3, @date, 0, @expiration_date);",
                                        "@client", client, "@includeMp3", includeMp3, "@date", null, "@expiration_date", expirationDate);

            //foreach (var feedEmail in mailingList)
            //{
            //    Database.Insert(@"INSERT INTO feed_emails (feed_filter_id, email, contact_id) VALUES (@feedId, @email,@contact_id);", "@feedId", feedId, "@email", feedEmail.email, "@contact_id",feedEmail.contact_id);
            //}

            return feedId;
        }

        public static long EditFeed(long feedFilterId, string client, bool includeMp3, DateTime? expirationDate/*, IEnumerable<FeedEmails> mailingList*/)
        {
            Database.ExecuteNonQuery(@"UPDATE feed_filters SET client = @client, include_mp3 = @includeMp3, expiration_date = @expiration_date WHERE id = @id",
                "@id", feedFilterId, "@client", client, "@includeMp3", includeMp3, "@expiration_date", expirationDate);


            /*Database.Delete(@"DELETE FROM feed_emails WHERE feed_filter_id = @id", "@id", feedFilterId);

            foreach (var feedEmail in mailingList)
            {
				Database.Insert(@"INSERT INTO feed_emails (feed_filter_id, email, contact_id) VALUES (@feedId, @email,@contact_id);", "@feedId", feedFilterId, "@email", feedEmail.email, "@contact_id", feedEmail.contact_id);
			}*/

            return feedFilterId;
        }

        public static void DeleteFeed(long feedId)
        {
			/*var ruleGroupsIds = Database.ListFetcher("SELECT id FROM rule_groups WHERE feed_filter_id = @id",
               dr => dr.GetInt32(0),
               "@id", feedId
           );

            foreach (var id in ruleGroupsIds)
                DeleteFeedFilterRuleGroup(id);

            Database.Delete(@"DELETE FROM feed_emails WHERE feed_filter_id = @id", "@id", feedId);
			Database.Delete(@"DELETE FROM contacts_feedfilter WHERE feed_filter_id = @id", "@id", feedId);
			Database.Delete(@"DELETE FROM feed_filters WHERE id = @id", "@id", feedId);*/
			Database.ExecuteNonQuery("Update feed_filters SET deleted = 1 WHERE id = @id", "@id", feedId);
		}

		public static void DeleteFeeds(IList<long> feedIds)
		{
			var ruleGroupsIds = Database.ListFetcher("SELECT id FROM rule_groups WHERE feed_filter_id " + Database.InClause(feedIds),
			   dr => dr.GetInt32(0)
		   );

			foreach (var id in ruleGroupsIds)
				DeleteFeedFilterRuleGroup(id);

			Database.Delete(@"DELETE FROM contacts_feedfilter WHERE feed_filter_id " + Database.InClause(feedIds));
			Database.Delete(@"DELETE FROM feed_emails WHERE feed_filter_id " + Database.InClause(feedIds));
			Database.Delete(@"DELETE FROM feed_filters WHERE id " + Database.InClause(feedIds));
		}



		public static FeedFilter GetFeedFilter(long feedFilterId, bool idsOnly = false)
		{
			var filters = GetFeedFilters(new List<long> { feedFilterId }, idsOnly);
			if (filters.Count > 0)
				return filters[0];
			return new FeedFilter();
		}


		public static List<FeedFilter> GetFeedFilters(IList<long> feedFilterIds, bool idsOnly = false)
        {
			if (!Database.Exists(@"SELECT 1 FROM feed_filters WHERE COALESCE(deleted,0) = 0 AND id " + Database.InClause(feedFilterIds)))
				return new List<FeedFilter>();

			//        var mailingList = Database.ListFetcher(@"SELECT con.id, feed_emails.email, feed_emails.contact_id, contacts.name,contacts.client_id, clients.name 
			//			FROM feed_emails LEFT OUTER JOIN contacts ON feed_emails.contact_id = contacts.contact_id
			//			LEFT OUTER JOIN clients ON contacts.client_id = clients.id
			//			WHERE feed_filter_id " + Database.InClause(feedFilterIds),
			//            dr => new FeedEmails {
			//	id = dr.GetInt32(0),
			//	email = dr.GetStringOrDefault(1),
			//	contact_id = dr.GetIntOrDefault(2),
			//	Contact = new Contact {
			//		name = dr.GetStringOrDefault(3),
			//		client_id = dr.GetIntOrDefault(4),
			//		Client = new Client {
			//			name = dr.GetStringOrDefault(5)
			//		}
			//	}
			//} 
			//        );

			var mailingList = Database.ListFetcher(@"SELECT contacts_feedfilter.feed_filter_id, contacts.contact_id, contacts.name, contacts.email, contacts.client_id, clients.name
									FROM contacts INNER JOIN contacts_feedfilter ON contacts.contact_id = contacts_feedfilter.contact_id
									LEFT OUTER JOIN clients ON contacts.client_id = clients.id
									WHERE contacts_feedfilter.feed_filter_id " + Database.InClause(feedFilterIds),
									dr => new FeedContact
									{
										FeedFilterId = dr.GetInt32(0),
										Contact = new Contact
										{
											contact_id = dr.GetInt32(1),
											name = dr.GetStringOrDefault(2),
											email = dr.GetStringOrDefault(3),
											client_id = dr.GetIntOrDefault(4),
											Client = new Client
											{
												name = dr.GetStringOrDefault(5)
											}
										}
									}
									);
            var sql = !idsOnly ?
			@"SELECT 								  
	              rg.id,
				  rg.feed_filter_id,
	              rg.exclude,
	              GROUP_CONCAT(DISTINCT mk.name ORDER BY mk.name SEPARATOR ', '),
	              GROUP_CONCAT(DISTINCT d.domain_name ORDER BY d.domain_name SEPARATOR ', '),
	              GROUP_CONCAT(DISTINCT a.company_name ORDER BY a.company_name SEPARATOR ', '), 
	              GROUP_CONCAT(DISTINCT b.brand_name ORDER BY b.brand_name SEPARATOR ', '),
	              GROUP_CONCAT(DISTINCT i.industry_name ORDER BY i.industry_name SEPARATOR ', '), 
	              GROUP_CONCAT(DISTINCT ac.category_name ORDER BY ac.category_name SEPARATOR ', ')
              FROM rule_groups as rg
              LEFT JOIN rule_markets rm ON rg.id = rm.rule_group_id
              LEFT JOIN rule_categories rc ON rg.id = rc.rule_group_id
              LEFT JOIN rule_domains rd ON rd.rule_group_id = rg.id
              LEFT JOIN domains d ON rd.domain = d.domain
              LEFT JOIN rule_advertisers ra ON rg.id = ra.rule_group_id
              LEFT JOIN rule_brands rb ON rg.id = rb.rule_group_id
              LEFT JOIN rule_industries ri ON rg.id = ri.rule_group_id
              LEFT JOIN markets mk ON rm.market_id = mk.id
              LEFT JOIN brands b ON rb.brand_id = b.id 
              LEFT JOIN advertisers a ON ra.advertiser_id = a.id 
              LEFT JOIN industries i ON ri.industry_id = i.id
              LEFT JOIN ad_categories ac ON rc.ad_category_id = ac.id
              WHERE rg.feed_filter_id " + Database.InClause(feedFilterIds) +
		" group by rg.id" :
	  @"SELECT 								  
	        rg.id,
			rg.feed_filter_id,
	        rg.exclude,
	        GROUP_CONCAT(DISTINCT rm.market_id ORDER BY rm.market_id SEPARATOR ','),
	        GROUP_CONCAT(DISTINCT rd.domain ORDER BY rd.domain SEPARATOR ','),
	        GROUP_CONCAT(DISTINCT ra.advertiser_id ORDER BY ra.advertiser_id SEPARATOR ','), 
	        GROUP_CONCAT(DISTINCT rb.brand_id ORDER BY rb.brand_id SEPARATOR ','),
	        GROUP_CONCAT(DISTINCT ri.industry_id ORDER BY ri.industry_id SEPARATOR ','), 
	        GROUP_CONCAT(DISTINCT rc.ad_category_id ORDER BY rc.ad_category_id SEPARATOR ',')
        FROM rule_groups as rg
        LEFT JOIN rule_markets rm ON rg.id = rm.rule_group_id
        LEFT JOIN rule_categories rc ON rg.id = rc.rule_group_id
        LEFT JOIN rule_domains rd ON rd.rule_group_id = rg.id
        LEFT JOIN rule_advertisers ra ON rg.id = ra.rule_group_id
        LEFT JOIN rule_brands rb ON rg.id = rb.rule_group_id
        LEFT JOIN rule_industries ri ON rg.id = ri.rule_group_id
        WHERE rg.feed_filter_id " + Database.InClause(feedFilterIds) +
		" group by rg.id";

            var filterGroupRows = Database.ListFetcher(sql,
                dr => new FeedFilterRuleGroupRow
                {
                    Id = dr.GetInt64(0),
                    FeedFilterId = dr.GetInt64(1),
                    Exclude = dr.GetBoolOrDefault(2),
                    FeedFilterRulesMarket = dr.GetStringOrDefault(3),
                    FeedFilterRulesDomain = dr.GetStringOrDefault(4),
                    FeedFilterRulesAdvertiser = dr.GetStringOrDefault(5),
                    FeedFilterRulesBrand = dr.GetStringOrDefault(6),
                    FeedFilterRulesIndustry = dr.GetStringOrDefault(7),
                    FeedFilterRulesCategory = dr.GetStringOrDefault(8)
                }
            );

            var filterGroups = new List<FeedFilterRuleGroup>();
            foreach (var row in filterGroupRows)
            {
                var filterGroupExists = filterGroups.Any(fg => fg.Id == row.Id);
                if (!filterGroupExists)
                {
                    filterGroups.Add(new FeedFilterRuleGroup
                    {
                        Id = row.Id,
                        FeedFilterId = row.FeedFilterId,
                        Exclude = row.Exclude,
                        FeedFilterRulesMarkets = new List<FeedFilterRule>(),
                        FeedFilterRulesDomains = new List<FeedFilterRule>(),
                        FeedFilterRulesAdvertisers = new List<FeedFilterRule>(),
                        FeedFilterRulesBrands = new List<FeedFilterRule>(),
                        FeedFilterRulesIndustries = new List<FeedFilterRule>(),
                        FeedFilterRulesCategories = new List<FeedFilterRule>()
                    });
                }

                var filterGroup = filterGroups.SingleOrDefault(fg => fg.Id == row.Id);

                if (row.FeedFilterRulesMarket != string.Empty)
                    filterGroup.FeedFilterRulesMarkets.Add(new FeedFilterRule { DisplayName = row.FeedFilterRulesMarket });
                if (row.FeedFilterRulesDomain != string.Empty)
                    filterGroup.FeedFilterRulesDomains.Add(new FeedFilterRule { DisplayName = row.FeedFilterRulesDomain });
                if (row.FeedFilterRulesAdvertiser != string.Empty)
                    filterGroup.FeedFilterRulesAdvertisers.Add(new FeedFilterRule { DisplayName = row.FeedFilterRulesAdvertiser });
                if (row.FeedFilterRulesBrand != string.Empty)
                    filterGroup.FeedFilterRulesBrands.Add(new FeedFilterRule { DisplayName = row.FeedFilterRulesBrand });
                if (row.FeedFilterRulesIndustry != string.Empty)
                    filterGroup.FeedFilterRulesIndustries.Add(new FeedFilterRule { DisplayName = row.FeedFilterRulesIndustry });
                if (row.FeedFilterRulesCategory != string.Empty)
                    filterGroup.FeedFilterRulesCategories.Add(new FeedFilterRule { DisplayName = row.FeedFilterRulesCategory });
            }

            return Database.ListFetcher(@"SELECT id, client, include_mp3, timestamp, fr.last_email, expiration_date
				FROM feed_filters ff LEFT JOIN
				( SELECT feed_filter_id, MAX(time_inserted) as last_email FROM feed_report group by feed_filter_id) as fr on fr.feed_filter_id = ff.id 
				
				WHERE id " + Database.InClause(feedFilterIds) ,
                dr => new FeedFilter
                {
                    Id = dr.GetInt64(0),
                    ClientName = dr.GetStringOrDefault(1),
                    IncludeMp3 = dr.GetBoolOrDefault(2),
                    Timestamp = dr.GetDateOrNull(3),
                    MailingList = mailingList.Where(l=>l.FeedFilterId == dr.GetInt64(0)).Select(l=>l.Contact).ToList(),
                    FilterGroups = filterGroups.Where(fg=>fg.FeedFilterId == dr.GetInt64(0)).ToList(),
                    LastEmailSent = dr.GetDateOrNull(4),
					ExpirationDate = dr.GetDateOrNull(5)
                }
            );
        }

		

		public static int GetTimeZoneOffset(long? feedFilterId = null, FeedFilter filter = null)
		{
			int? result = null;

			//Try to find offset for domain
			var feedFilter = filter ?? (feedFilterId != null ? GetFeedFilter(feedFilterId.Value, true) : null);
			if(feedFilter != null)
			{
				if (feedFilter.FilterGroups.Any(g => g.FeedFilterRulesDomains.Count > 0))
				{
					var feedDomain = feedFilter.FilterGroups.SelectMany(g => g.FeedFilterRulesDomains).FirstOrDefault();
					var domain_name = feedDomain != null ? feedDomain.DisplayName : string.Empty;
					if (!string.IsNullOrEmpty(domain_name))
					{
						var domain = Domain.GetByFullName(domain_name);
						if (domain != null && domain.timezoneoffset != null)
							result = domain.timezoneoffset;
					}
				}
			}			
			if(result == null)
			{
				var settings = FeedSettings.Get();
				result = settings.TimeZoneOffset;
			}
			return result ?? 0;
		}

        public static FeedFilterRuleGroup GetFeedFilterRuleGroup(long filterGroupId)
        {
            if (!Database.Exists(@"SELECT 1 FROM rule_groups WHERE id = @id", "@id", filterGroupId))
                return new FeedFilterRuleGroup();


            var filterGroup = Database.ItemFetcher("SELECT id, feed_filter_id, exclude FROM rule_groups WHERE id = @filterGroupId",
                dr => new FeedFilterRuleGroup
                {
                    Id = dr.GetInt64(0),
                    FeedFilterId = dr.GetInt64(1),
                    Exclude = dr.GetBoolOrDefault(2)
                },
                "@filterGroupId", filterGroupId
            );

            //Markets
            filterGroup.FeedFilterRulesMarkets = Database.ListFetcher("SELECT rm.market_id, m.name FROM rule_markets rm JOIN markets m ON rm.market_id = m.id WHERE rm.rule_group_id = @filterGroupId",
                dr => new FeedFilterRule
                {
                    Id = dr.GetStringOrDefault(0),
                    DisplayName = dr.GetStringOrDefault(1)
                },
                "@filterGroupId", filterGroupId
            );

            //Domains
            filterGroup.FeedFilterRulesDomains = Database.ListFetcher("SELECT rd.domain, d.domain_name FROM rule_domains rd LEFT JOIN domains d ON rd.domain = d.domain WHERE rd.rule_group_id = @filterGroupId",
                dr => new FeedFilterRule
                {
                    Id = dr.GetStringOrDefault(0),
                    DisplayName = dr.GetStringOrDefault(1)
                },
                "@filterGroupId", filterGroupId
            );

            //Industries
            filterGroup.FeedFilterRulesIndustries = Database.ListFetcher("SELECT rd.industry_id, i.industry_name FROM rule_industries rd JOIN industries i ON rd.industry_id = i.id WHERE rd.rule_group_id = @filterGroupId",
                dr => new FeedFilterRule
                {
                    Id = dr.GetStringOrDefault(0),
                    DisplayName = dr.GetStringOrDefault(1)
                },
                "@filterGroupId", filterGroupId
            );

            //Categories
            filterGroup.FeedFilterRulesCategories = Database.ListFetcher("SELECT rc.ad_category_id, a.category_name FROM rule_categories rc JOIN ad_categories a ON rc.ad_category_id = a.id WHERE rc.rule_group_id = @filterGroupId",
                dr => new FeedFilterRule
                {
                    Id = dr.GetStringOrDefault(0),
                    DisplayName = dr.GetStringOrDefault(1)
                },
                "@filterGroupId", filterGroupId
            );

            //Advertisers
            filterGroup.FeedFilterRulesAdvertisers = Database.ListFetcher("SELECT ra.advertiser_id, a.company_name FROM rule_advertisers ra JOIN advertisers a ON ra.advertiser_id = a.id WHERE ra.rule_group_id = @filterGroupId",
                dr => new FeedFilterRule
                {
                    Id = dr.GetStringOrDefault(0),
                    DisplayName = dr.GetStringOrDefault(1)
                },
                "@filterGroupId", filterGroupId
            );

            //Brand
            filterGroup.FeedFilterRulesBrands = Database.ListFetcher("SELECT rb.brand_id, b.brand_name FROM rule_brands rb JOIN brands b ON rb.brand_id = b.id WHERE rb.rule_group_id = @filterGroupId",
                dr => new FeedFilterRule
                {
                    Id = dr.GetStringOrDefault(0),
                    DisplayName = dr.GetStringOrDefault(1)
                },
                "@filterGroupId", filterGroupId
            );

            return filterGroup;
        }

		public static List<FeedFilter> GetFeedFiltersForContact(int contact_id)
		{
			return Database.ListFetcher<FeedFilter>(@"SELECT id, client, include_mp3, timestamp 
					FROM feed_filters INNER JOIN contacts_feedfilter ON feed_filters.id = contacts_feedfilter.feed_filter_id 
					WHERE feed_filters.deleted = 0 AND contacts_feedfilter.contact_id = @id",
					dr => new FeedFilter
					{
						Id = dr.GetInt64(0),
						ClientName = dr.GetStringOrDefault(1),
						IncludeMp3 = dr.GetBoolOrDefault(2),
						Timestamp = dr.GetDateOrNull(3),
					},
					"@id", contact_id
					);
		}

		public static List<FeedFilter> GetFeedFiltersForClient(int client_id)
		{
			return Database.ListFetcher<FeedFilter>(@"SELECT id, client, include_mp3, timestamp 
					FROM feed_filters INNER JOIN clients_feedfilter ON feed_filters.id = clients_feedfilter.feed_filter_id 
					WHERE feed_filters.deleted = 0 AND clients_feedfilter.client_id = @id",
					dr => new FeedFilter
					{
						Id = dr.GetInt64(0),
						ClientName = dr.GetStringOrDefault(1),
						IncludeMp3 = dr.GetBoolOrDefault(2),
						Timestamp = dr.GetDateOrNull(3),
					},
					"@id", client_id
					);
		}

		public static void DeleteFeedFilterRuleGroup(long filterGroupId)
        {
            //Markets
            Database.Delete(@"DELETE FROM rule_markets WHERE rule_group_id = @id",
                "@id", filterGroupId);

            //Domains
            Database.Delete(@"DELETE FROM rule_domains WHERE rule_group_id = @id",
                "@id", filterGroupId);

            //Industries
            Database.Delete(@"DELETE FROM rule_industries WHERE rule_group_id = @id",
                "@id", filterGroupId);

            //Categories
            Database.Delete(@"DELETE FROM rule_categories WHERE rule_group_id = @id",
                "@id", filterGroupId);

            //Advertisers
            Database.Delete(@"DELETE FROM rule_advertisers WHERE rule_group_id = @id",
                "@id", filterGroupId);

            //Brands
            Database.Delete(@"DELETE FROM rule_brands WHERE rule_group_id = @id",
                "@id", filterGroupId);

            //Filter group / Rule group
            Database.Delete(@"DELETE FROM rule_groups WHERE id = @id",
                "@id", filterGroupId);
        }

        public static void SaveFeedFilterRuleGroup(FeedFilterRuleGroup filterGroup)
        {
            bool exists;

            exists = Database.Exists(@"SELECT 1 FROM rule_groups WHERE id = @id",
                "@id", filterGroup.Id);

            if (!exists)
            {
                filterGroup.Id = Database.Insert(@"
				INSERT INTO rule_groups (feed_filter_id, exclude)
				VALUES (@feed_filter_id, @exclude)",
                    "@feed_filter_id", filterGroup.FeedFilterId,
                    "@exclude", filterGroup.Exclude);

            }


            //Markets
            Database.Delete(@"DELETE FROM rule_markets WHERE rule_group_id = @id",
                "@id", filterGroup.Id);
            foreach (var item in filterGroup.FeedFilterRulesMarkets)
            {
                Database.Insert(@"INSERT INTO rule_markets (rule_group_id, market_id) VALUES (@rule_group_id, @id)", "@rule_group_id", filterGroup.Id, "@id", item.Id);
            }

            //Domains
            Database.Delete(@"DELETE FROM rule_domains WHERE rule_group_id = @id",
                "@id", filterGroup.Id);
            foreach (var item in filterGroup.FeedFilterRulesDomains)
            {
                Database.Insert(@"INSERT INTO rule_domains (rule_group_id, domain) VALUES (@rule_group_id, @id)", "@rule_group_id", filterGroup.Id, "@id", item.Id);
            }

            //Industries
            Database.Delete(@"DELETE FROM rule_industries WHERE rule_group_id = @id",
                "@id", filterGroup.Id);
            foreach (var item in filterGroup.FeedFilterRulesIndustries)
            {
                Database.Insert(@"INSERT INTO rule_industries (rule_group_id, industry_id) VALUES (@rule_group_id, @id)", "@rule_group_id", filterGroup.Id, "@id", item.Id);
            }

            //Categories
            Database.Delete(@"DELETE FROM rule_categories WHERE rule_group_id = @id",
                "@id", filterGroup.Id);
            foreach (var item in filterGroup.FeedFilterRulesCategories)
            {
                Database.Insert(@"INSERT INTO rule_categories (rule_group_id, ad_category_id) VALUES (@rule_group_id, @id)", "@rule_group_id", filterGroup.Id, "@id", item.Id);
            }

            //Advertisers
            Database.Delete(@"DELETE FROM rule_advertisers WHERE rule_group_id = @id",
                "@id", filterGroup.Id);
            foreach (var item in filterGroup.FeedFilterRulesAdvertisers)
            {
                Database.Insert(@"INSERT INTO rule_advertisers (rule_group_id, advertiser_id) VALUES (@rule_group_id, @id)", "@rule_group_id", filterGroup.Id, "@id", item.Id);
            }

            //Brands
            Database.Delete(@"DELETE FROM rule_brands WHERE rule_group_id = @id",
                "@id", filterGroup.Id);
            foreach (var item in filterGroup.FeedFilterRulesBrands)
            {
                Database.Insert(@"INSERT INTO rule_brands (rule_group_id, brand_id) VALUES (@rule_group_id, @id)", "@rule_group_id", filterGroup.Id, "@id", item.Id);
            }
        }

        public static List<AjaxDomain> GetDomains(string term)
        {
            return Database.ListFetcher(
                @"SELECT DISTINCT c.domain, d.domain_name FROM channels c LEFT JOIN domains d on c.domain = d.domain",
                dr => new AjaxDomain
                {
                    Domain = dr.GetStringOrDefault(0),
                    DisplayName = dr.GetStringOrDefault(1)
                }
            );
        }

        public static List<FeedResultItem> GetFeed(long feedFilterId, DateTime? cutOffDate, TaggerUser user = null, DateTime? dateTo = null)
        {
            int totalCount;
            var filter = new AjaxFeedFilter();
            var startDate = (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0)).AddDays(-7);
            return GetFeed(feedFilterId, cutOffDate.HasValue ? cutOffDate : startDate, 0, 0, "firstAiring", false, filter, user, out totalCount, dateTo);
        }

        public static List<FeedResultItem> GetFeed(long feedFilterId, DateTime? cutOffDate, int pageSize, int pageNum, string sortColumn, bool ascending, AjaxFeedFilter filter, TaggerUser user, out int totalCount, DateTime? dateTo = null)
        {
            List<FeedResultItem> list = new List<FeedResultItem>();
            totalCount = 0;

			var feedSettings = AppSettings.FeedSettings.Get();

            if (feedFilterId > 0 && !Database.Exists(@"SELECT 1 FROM rule_groups WHERE feed_filter_id = @id", "@id", feedFilterId))
                return list;

			int? contact_id = null;
			if(user != null && !user.IsGrantedClientFeeds())
			{
				var contact = Contact.GetForUser(user.Id);
				contact_id = contact != null ? (int?)contact.contact_id : null;
			}
			

			string additionalFilter = "";
            //if (!string.IsNullOrWhiteSpace(filter.advertiserBrand))
            //    additionalFilter += " AND (a.company_name LIKE '%" + filter.advertiserBrand + "%' OR b.brand_name LIKE '%" + filter.advertiserBrand + "%')";
            if (!string.IsNullOrWhiteSpace(filter.brand))
                additionalFilter += " AND b.brand_name LIKE '%" + filter.brand + "%'";
            if (!string.IsNullOrWhiteSpace(filter.regionMarket))
                additionalFilter += " AND d.domain_name LIKE '%" + filter.regionMarket + "%'";
            if (!string.IsNullOrWhiteSpace(filter.channel))
                additionalFilter += " AND c.station_name LIKE '%" + filter.channel + "%'";
            if (!string.IsNullOrWhiteSpace(filter.adTranscript))
                additionalFilter += " AND ifnull(t.full_text, s.title) LIKE '%" + filter.adTranscript + "%'";


			string channelsRule;
			var rulesFilterSql = GetRulesFilterSql(feedFilterId, ref cutOffDate, out channelsRule);
			var reportFilterSql = contact_id != null ?
				@" AND s.pksid IN (SELECT DISTINCT pksid FROM feed_results_email fre INNER JOIN feed_report fr ON fre.header_id = fr.id INNER JOIN feed_report_contact frc ON fr.id = frc.report_id
				WHERE fr.feed_filter_id = @feedFilterId AND contact_id = @contact_id  )" : "";

			List<RawFeedResultItem> results = GetGroupedResults2(feedFilterId, cutOffDate, feedSettings, additionalFilter, channelsRule, rulesFilterSql);
			            
            Dictionary<string, FeedResultItem> dict = new Dictionary<string, FeedResultItem>();

            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
				//r.SongExcluded = songsExcluded.ContainsKey(r.SongId);

                FeedResultItem item;
                if (dict.TryGetValue(r.MP3PKSID, out item))
                {
                    if (item.Channels.Count == feedSettings.FirstAiringChannels)
                        continue;


                    var chanIndex = item.Channels.FindIndex(x => x.Name == r.ChannelName);

                    if (chanIndex >= 0)
                    {
                        if (item.Channels[chanIndex].FirstAiringDateTime < r.ChannelFirstAiringDateTime)
                            item.Channels[chanIndex].FirstAiringDateTime = r.ChannelFirstAiringDateTime;
                    }
                    else
                        item.Channels.Add(new FeedResultItemChannel() { Id = r.ChannelId, Name = r.ChannelName, FirstAiringDateTime = r.ChannelFirstAiringDateTime });

                    //item.Channels.Sort((x, y) => x.FirstAiringDateTime.Value.CompareTo(y.FirstAiringDateTime.Value));

                    if (sortColumn == "firstAiring")
                        item.Channels = item.Channels.OrderByWithDirection(x => x.FirstAiringDateTime, !ascending).ToList();
                    else
                        item.Channels = item.Channels.OrderByDescending(x => x.FirstAiringDateTime).ToList();
                }
                else
                {
                    item = new FeedResultItem();
                    item.AdvertiserId = r.AdvertiserId;
                    item.AdvertiserName = r.AdvertiserName;
                    item.BrandId = r.BrandId;
                    item.BrandName = r.BrandName;
                    item.CategoryId = r.CategoryId;
                    item.CategoryName = r.CategoryName;
                    item.AdTranscriptId = r.AdTranscriptId;
                    item.AdTranscript = r.AdTranscript;
                    item.IndustryId = r.IndustryId;
                    item.IndustryName = r.IndustryName;
                    item.RegionsIds = r.RegionsIds;
                    item.MarketsIds = r.MarketsIds;
                    item.Regions = r.Regions;
                    item.Markets = r.Markets;
                    item.Channels.Add(new FeedResultItemChannel() { Id = r.ChannelId, Name = r.ChannelName, FirstAiringDateTime = r.ChannelFirstAiringDateTime });
                    item.Mp3Url = Song.GetMp3Url(r.MP3PKSID);
                    item.Duration = r.Duration;
                    item.Mp3Id = r.MP3PKSID;
                    item.MediaType = r.MediaType;
                    item.SongTitle = r.Title;
					item.SongExcluded = r.SongExcluded;
					item.SongId = r.SongId;
                    dict.Add(r.MP3PKSID, item);
                }
            }

            switch (sortColumn)
            {
                case "regionMarket":
                    list = dict.Select(x => x.Value).OrderByWithDirection(o => o.Regions, !ascending).ToList();
                    break;
                case "channel":
                    list = dict.Select(x => x.Value).OrderByWithDirection(o => o.Channels.FirstOrDefault().Name, !ascending).ToList();
                    break;
                case "firstAiring":
                    list = dict.Select(x => x.Value).OrderByWithDirection(o => o.Channels.FirstOrDefault().FirstAiringDateTime, !ascending).ToList();
                    break;
                case "brand":
                default:
                    list = dict.Select(x => x.Value).OrderByWithDirection(o => o.BrandName, !ascending).ToList();
                    break;

                    //    case "advertiserBrand":
                    //    default:
                    //        list = dict.Select(x => x.Value).OrderByWithDirection(o => o.AdvertiserName, !ascending).ToList();
                    //        break;
            }

            totalCount = list.Count();

            if (pageSize > 0)
                list = list.GetRange(pageNum * pageSize, list.Count < (pageNum + 1) * pageSize ? list.Count - (pageNum * pageSize) : pageSize);

            return list;
        }

		private static List<RawFeedResultItem> GetGroupedResults(long feedFilterId, DateTime? cutOffDate, FeedSettings feedSettings, string additionalFilter, string channelsRule, string rulesFilterSql)
		{
			return Database.ListFetcher(string.Format(@"
                                SELECT 
	                                   a.id, 
		                                a.company_name, 
	                                   b.id, 
		                                b.brand_name, 
                                       t.id, 
		                                ifnull(t.full_text, s.title),
                                       i.id, 
		                                i.industry_name, 
                                       ac.id, 
		                                ac.category_name,
	                                   c.id,
                                        c.station_name,
                                        MIN(m.match_occurred) AS min_match_occurred,
                                        GROUP_CONCAT(DISTINCT d.id ORDER BY d.domain_name SEPARATOR ', '), 
                                        GROUP_CONCAT(DISTINCT mk.id ORDER BY mk.name SEPARATOR ', '),
                                        GROUP_CONCAT(DISTINCT d.domain_name ORDER BY d.domain_name SEPARATOR ', '), 
                                        GROUP_CONCAT(DISTINCT mk.name ORDER BY mk.name SEPARATOR ', '),
                                       s.pksid, s.duration, c.media_type, s.title,IF(sfe.song_id IS NOT NULL,1,0),s.id
                                FROM matches m
                                LEFT JOIN songs s ON m.song_id = s.id
                                LEFT JOIN (SELECT c.id, c.domain,c.station_name,c.media_type  FROM channels c WHERE LEFT(c.station_name,1) <> '*'
								 {0} ) c ON m.channel_id = c.id
                                LEFT JOIN brands  b ON s.brand_id = b.id 
                                LEFT JOIN advertisers a ON b.advertiser_id = a.id 
                                LEFT JOIN industries i ON b.industry_id = i.id
                                LEFT JOIN ad_categories ac ON s.category_id = ac.id
                                LEFT JOIN song_transcript t ON t.song_id = s.id
                                LEFT JOIN market_channels mc ON c.id = mc.channel_id
                                LEFT JOIN markets mk ON mc.market_id = mk.id
                                LEFT JOIN domains d ON d.domain = c.domain
                                LEFT JOIN products p ON s.product_id = p.id
								LEFT JOIN song_feedexcluded sfe ON s.id = sfe.song_id
                                WHERE  s.created >= @cutOffDate
								  AND COALESCE(s.deleted,0) = 0
                                  AND (p.id ", channelsRule) + Database.InClause(feedSettings.FeedProducts.Select(p => p.Id)) + ")"
											 + rulesFilterSql
											+ additionalFilter + @"
                                GROUP BY a.id, a.company_name, b.id, b.brand_name, i.id, i.industry_name, ac.id, ac.category_name, c.id, c.station_name, s.id, s.pksid
                                ORDER BY min_match_occurred",
								dr => new RawFeedResultItem()
								{
									AdvertiserId = dr.GetStringOrDefault(0),
									AdvertiserName = dr.GetStringOrDefault(1),
									BrandId = dr.GetStringOrDefault(2),
									BrandName = dr.GetStringOrDefault(3),
									AdTranscriptId = dr.GetStringOrDefault(4),
									AdTranscript = dr.GetStringOrDefault(5),
									IndustryId = dr.GetStringOrDefault(6),
									IndustryName = dr.GetStringOrDefault(7),
									CategoryId = dr.GetStringOrDefault(8),
									CategoryName = dr.GetStringOrDefault(9),
									ChannelId = dr.GetStringOrDefault(10),
									ChannelName = dr.GetStringOrDefault(11),
									ChannelFirstAiringDateTime = dr.GetDateOrDefault(12),
									RegionsIds = dr.GetStringOrDefault(13),
									MarketsIds = dr.GetStringOrDefault(14),
									Regions = dr.GetStringOrDefault(15),
									Markets = dr.GetStringOrDefault(16),
									MP3PKSID = dr.GetStringOrDefault(17),
									Duration = dr.GetDecimalOrDefault(18),
									MediaType = dr.GetStringOrDefault(19),
									Title = dr.GetStringOrDefault(20),
									SongExcluded = dr.GetBoolOrNull(21),
									SongId = dr.GetGuidOrNull(22)
								},
								"@feedFilterId", feedFilterId,
								"@cutOffDate", cutOffDate

							).Where(r => !string.IsNullOrEmpty(r.ChannelId)).ToList();
		}

		private static List<RawFeedResultItem> GetGroupedResults2(long feedFilterId, DateTime? cutOffDate,
			FeedSettings feedSettings, string additionalFilter, string channelsRule, string rulesFilterSql)
		{
			var rawResults = Database.ListFetcher(string.Format(@"
                                SELECT 
	                                   a.id, 		                                
	                                   b.id, 		                                
                                       i.id, 		                                
                                       ac.id, 		                                
	                                   c.id,
										m.match_start,
										m.match_end,
                                        m.match_occurred,                                        
                                       s.pksid,
									   s.duration,
									   c.match_threshold,
									   s.id,
										d.id,
										mk.id,
										IF(sfe.song_id IS NOT NULL,1,0),
										s.title, c.media_type
                                FROM matches m
                                LEFT JOIN songs s ON m.song_id = s.id
                                LEFT JOIN (SELECT c.id, c.domain, c.match_threshold, c.media_type  FROM channels c WHERE LEFT(c.station_name,1) <> '*'
								 {0} ) c ON m.channel_id = c.id
                                LEFT JOIN brands  b ON s.brand_id = b.id 
                                LEFT JOIN advertisers a ON b.advertiser_id = a.id 
                                LEFT JOIN industries i ON b.industry_id = i.id
                                LEFT JOIN ad_categories ac ON s.category_id = ac.id                                
                                LEFT JOIN market_channels mc ON c.id = mc.channel_id
                                LEFT JOIN markets mk ON mc.market_id = mk.id
                                LEFT JOIN domains d ON d.domain = c.domain
                                LEFT JOIN products p ON s.product_id = p.id
								LEFT JOIN song_feedexcluded sfe ON s.id = sfe.song_id
                                WHERE  s.created >= @cutOffDate
								  AND COALESCE(s.deleted,0) = 0
                                  AND (p.id ", channelsRule) + Database.InClause(feedSettings.FeedProducts.Select(p => p.Id)) + ")"
											 + rulesFilterSql
											+ additionalFilter ,
								dr => new RawFeedUngroupedResult
								{
									AdvertiserId = dr.GetStringOrDefault(0),									
									BrandId = dr.GetStringOrDefault(1),									
									IndustryId = dr.GetStringOrDefault(2),									
									CategoryId = dr.GetStringOrDefault(3),									
									ChannelId = dr.GetStringOrDefault(4),
									MatchStart = dr.GetDoubleOrNull(5),
									MatchEnd = dr.GetDoubleOrNull(6),
									MatchOccured = dr.GetDateOrNull(7),
									MP3PKSID = dr.GetStringOrDefault(8),
									Duration = dr.GetDecimalOrDefault(9),
									MatchThreshold = dr.GetDecimalOrNull(10),
									SongId = dr.GetGuidOrNull(11),
									DomainId = dr.GetIntOrNull(12),
									MarketId = dr.GetIntOrNull(13),
									SongExcluded = dr.GetBoolOrNull(14),
									SongTitle = dr.GetStringOrDefault(15),
									MediaType = dr.GetStringOrDefault(16)
								},
								"@feedFilterId", feedFilterId,
								"@cutOffDate", cutOffDate

							).Where(r => !string.IsNullOrEmpty(r.ChannelId) && r.Valid).ToList();
			//Group the values
			var grouped = rawResults.GroupBy(r=> new {r.AdvertiserId, r.BrandId, r.IndustryId, r.CategoryId, r.ChannelId, r.SongId, r.MediaType}).
				Select(g=> new RawFeedResultItem
				{
					AdvertiserId = g.Key.AdvertiserId,
					BrandId = g.Key.BrandId,
					IndustryId = g.Key.IndustryId,
					CategoryId = g.Key.CategoryId,
					ChannelId = g.Key.ChannelId,
					SongId = g.Key.SongId,
					ChannelFirstAiringDateTime = g.Min(r=>r.MatchOccured),
					MarketIdsList = g.Select(r=>r.MarketId).Distinct().ToList(),
					DomainIdsList = g.Select(r=>r.DomainId).Distinct().ToList(),
					MP3PKSID = g.First().MP3PKSID,
					Duration = g.First().Duration,
					SongExcluded = g.First().SongExcluded,
					Title = g.First().SongTitle,
					MediaType = g.Key.MediaType
				}).OrderBy(g=>g.ChannelFirstAiringDateTime).ToList();

			//Get related tables data
			var advertisersIds = grouped.Select(g=>g.AdvertiserId).Distinct().Select(Guid.Parse).ToList();
			var advertisers = advertisersIds.Count > 0 ? Advertiser.GetAll(advertisersIds).ToDictionary(a=>a.Id.ToString()) : new Dictionary<string, Advertiser>();
			var brandIds = grouped.Select(g=>g.BrandId).Distinct().Select(Guid.Parse).ToList();
			var brands = brandIds.Count > 0 ? Brand.GetAll(brandIds).ToDictionary(a=>a.Id.ToString()) : new Dictionary<string, Brand>();
			var industryIds = grouped.Select(g=>g.IndustryId).Distinct().Select(Guid.Parse).ToList();
			var industries = industryIds.Count > 0 ? TaggerIndustry.GetAll(industryIds).ToDictionary(a=>a.Id.ToString()) : new Dictionary<string, TaggerIndustry>();
			var categoryIds = grouped.Select(g=>g.CategoryId).Distinct().Select(Guid.Parse).ToList();
			var categories = categoryIds.Count > 0 ? Category.GetAll(categoryIds).ToDictionary(a=>a.Id.ToString()) : new Dictionary<string, Category>();
			var channelsIds = grouped.Select(g=>g.ChannelId).Distinct().Select(Guid.Parse).ToList();
			var channels = channelsIds.Count > 0 ? Channel.Get(channelsIds).ToDictionary(a=>a.Id.ToString()) : new Dictionary<string, Channel>();
			var songIds = grouped.Select(g=>g.SongId.Value).Distinct().ToList();
			var transcripts = songIds.Count > 0 ? Transcript.GetBySongIds(songIds).ToList().ToDictionary(a=>(Guid?) a.SongId) : new Dictionary<Guid?, Transcript>();
			var marketIds = grouped.SelectMany(g=>g.MarketIdsList).Distinct().ToList();
			var markets = marketIds.Count > 0 ? Market.GetMarketByIds(marketIds).ToList() : new List<Market>();
			var domainIds = grouped.SelectMany(g=>g.DomainIdsList).Distinct().ToList();
			var domains = domainIds.Count > 0 ? Domain.GetByIds(domainIds).ToList() : new List<Domain>();

			foreach(var g in grouped)
			{
				if(g.AdvertiserId != null)
					g.AdvertiserName = advertisers[g.AdvertiserId].Name;
				if(g.BrandId != null)
					g.BrandName = brands[g.BrandId].Name;
				var transcript =  transcripts.ContainsKey(g.SongId) ? transcripts[g.SongId] : null;
				g.AdTranscript = transcript != null && !string.IsNullOrEmpty(transcript.FullText) ? transcript.FullText : g.Title;
				g.AdTranscriptId = transcript != null ? transcript.Id.ToString() : string.Empty;
				g.IndustryName = industries[g.IndustryId].Name;
				g.CategoryName = categories[g.CategoryId].Name;
				g.ChannelName = channels[g.ChannelId].Name;
				var rowDomains = domains.Where(d=>g.DomainIdsList.Contains(d.id)).ToList();
				g.Regions = string.Join(",",rowDomains.Select(d=>d.domain_name));
				var rowMarkets = markets.Where(d=>g.MarketIdsList.Contains(d.Id)).ToList();
				g.Markets = string.Join(",",rowMarkets.Select(d=>d.Name));
			}
			return grouped;
		}

		private static string GetRulesFilterSql(long feedFilterId, ref DateTime? cutOffDate, out string channelsRule)
		{
			var rulesFilter = new List<string>();
			var feedFilter = GetFeedFilter(feedFilterId, true);
			channelsRule = string.Empty;

			if (cutOffDate != null)
			{
				//cutoffdate is in localtime but it needs to be compared to songcreated which is in server date
				var timeZoneOffset = GetTimeZoneOffset(feedFilterId, feedFilter);
				cutOffDate = cutOffDate.Value.AddHours(timeZoneOffset);
			}

			foreach (var g in feedFilter.FilterGroups)
			{

				/*if (g.FeedFilterRulesDomains.Count > 0)
					rulesFilter.Add(BuildClause("c.domain ", g.FeedFilterRulesDomains.SelectMany(r => r.DisplayName.Split(',')).ToList(), g.Exclude));*/
				if(g.FeedFilterRulesDomains.Count > 0)
					channelsRule = " AND " + BuildClause("c.domain ", g.FeedFilterRulesDomains.SelectMany(r => r.DisplayName.Split(',')).ToList(), g.Exclude);
				if (g.FeedFilterRulesAdvertisers.Count > 0)
					rulesFilter.Add(BuildClause("a.id ", g.FeedFilterRulesAdvertisers.SelectMany(r => r.DisplayName.Split(',')).ToList(), g.Exclude));
				if (g.FeedFilterRulesBrands.Count > 0)
					rulesFilter.Add(BuildClause("b.id ", g.FeedFilterRulesBrands.SelectMany(r => r.DisplayName.Split(',')).ToList(), g.Exclude));
				if (g.FeedFilterRulesCategories.Count > 0)
					rulesFilter.Add(BuildClause("ac.id ", g.FeedFilterRulesCategories.SelectMany(r => r.DisplayName.Split(',')).ToList(), g.Exclude));
				if (g.FeedFilterRulesIndustries.Count > 0)
					rulesFilter.Add(BuildClause("i.id ", g.FeedFilterRulesIndustries.SelectMany(r => r.DisplayName.Split(',')).ToList(), g.Exclude));
				if (g.FeedFilterRulesMarkets.Count > 0)
					rulesFilter.Add(BuildClause("mk.id ", g.FeedFilterRulesMarkets.SelectMany(r => r.DisplayName.Split(',')).ToList(), g.Exclude));
			}

			var rulesFilterSql = string.Empty;
			if (rulesFilter.Count > 0)
				rulesFilterSql = " AND " + string.Join(" AND ", rulesFilter);
			return rulesFilterSql;
		}

		public static int? GetCountForFeed(long feedFilterId, DateTime? cutOffDate = null)
		{
			if (cutOffDate == null)
			{
				//get date from email reports
				var lastReportItem = GetLastFeedReportItemInfo(feedFilterId);
				if (lastReportItem != null)
				{
					cutOffDate = lastReportItem.TimeInserted;					
				}

				if (cutOffDate == null)
				{
					cutOffDate = DateTime.Today.AddDays(-7);
				}
			}
			string channelsRule;
			var rulesFilterSql = GetRulesFilterSql(feedFilterId, ref cutOffDate, out channelsRule);
			if(!string.IsNullOrEmpty(rulesFilterSql) || feedFilterId == 0)
			{
				var feedSettings = AppSettings.FeedSettings.Get();
				var sql = string.Format(@"SELECT m.song_id, c.id as channel_id, m.match_start, m.match_end, c.match_threshold, s.duration
								FROM matches m
								LEFT JOIN songs s ON m.song_id = s.id
								LEFT JOIN (SELECT c.id, c.domain,c.match_threshold FROM channels c WHERE LEFT(c.station_name,1) <> '*'
								 {0} ) c ON m.channel_id = c.id
								LEFT JOIN brands b ON s.brand_id = b.id
								LEFT JOIN advertisers a ON b.advertiser_id = a.id
								LEFT JOIN industries i ON b.industry_id = i.id
								LEFT JOIN ad_categories ac ON s.category_id = ac.id
								LEFT JOIN song_transcript t ON t.song_id = s.id
								LEFT JOIN market_channels mc ON c.id = mc.channel_id
								LEFT JOIN markets mk ON mc.market_id = mk.id
								LEFT JOIN domains d ON d.domain = c.domain
								LEFT JOIN products p ON s.product_id = p.id
								WHERE 
								  s.created > @cutOffDate								  
								  AND COALESCE(s.deleted,0) = 0
								  AND(p.id ",channelsRule) + Database.InClause(feedSettings.FeedProducts.Select(p => p.Id)) + ")"
									+ rulesFilterSql;
				var items = Database.ListFetcher<FeedMatchCount>(sql, dr => new FeedMatchCount {
					song_id = dr.GetString(0),
					channel_id = dr.GetNullableString(1),
					MatchStart = dr.GetDoubleOrNull(2),
					MatchEnd = dr.GetDoubleOrNull(3),
					MatchThreshold = dr.GetDecimalOrNull(4),
					Duration = dr.GetDecimalOrDefault(5)					
					}, "@cutOffDate", cutOffDate);
				return items.Where(i=>i.channel_id != null && i.Valid).Select(i=>i.song_id).Distinct().Count();
			}
			return 0;
		}

        private static string EncloseStrings(IList<string> inputs)
        {
            return string.Join(",", inputs.Select(s => string.Format("'{0}'", s)));
        }

        private static string EncloseStrings(string input)
        {
            return EncloseStrings(input.Split(','));
        }

        private static string BuildClause(string fieldName, IList<string> inputs, bool negate = false)
        {
            return fieldName + (inputs.Count > 1 ? (negate ? " NOT " : "") + Database.InClause(inputs) :
                                                   (negate ? "<>" : "=") + string.Format("'{0}'", inputs[0]));
        }

        public static List<FeedResultItem> GetFeedForReportId(string reportId, int pageSize, int pageNum, string sortColumn, bool ascending, AjaxFeedFilter filter, out int totalCount)
        {
            List<FeedResultItem> list = new List<FeedResultItem>();
            totalCount = 0;

            if (!Database.Exists(@"SELECT 1 FROM feed_report INNER JOIN feed_filters ON feed_report.feed_filter_id = feed_filters.id WHERE feed_filters.deleted = 0 AND report_id = @id", "@id", reportId))
                return list;

            string additionalFilter = "";
            //if (!string.IsNullOrWhiteSpace(filter.advertiserBrand))
            //    additionalFilter += " AND (a.company_name LIKE '%" + filter.advertiserBrand + "%' OR b.brand_name LIKE '%" + filter.advertiserBrand + "%')";
            if (!string.IsNullOrWhiteSpace(filter.brand))
                additionalFilter += " AND (a.company_name LIKE '%" + filter.brand + "%' OR b.brand_name LIKE '%" + filter.brand + "%')";
            if (!string.IsNullOrWhiteSpace(filter.adTranscript))
                additionalFilter += " AND ifnull(st.full_text, s.title) LIKE '%" + filter.adTranscript + "%'";

            list = Database.ListFetcher(@"
                                                    SELECT fre.id, #0
	                                                       fr.feed_filter_id, #1
                                                           fre.client_id, #2
                                                           fr.feed_timestamp, #3
                                                           fre.advertiser_id, #4
                                                           a.company_name, #5
                                                           fre.brand_id, #6
                                                           b.brand_name, #7
                                                           fre.ad_transcript_id, #8
                                                           ifnull(st.full_text, s.title), #9
                                                           fre.region_ids, #10
                                                           fre.market_ids, #11
                                                           fre.industry_id, #12
                                                           i.industry_name, #13
                                                           fre.ad_category_id, #14
                                                           ac.category_name, #15
                                                           fre.channel_id_1, #16
                                                           (SELECT station_name FROM channels ch WHERE ch.id = fre.channel_id_1) channel_name_1, #17
                                                           first_airing_1, #18
                                                           fre.channel_id_2, #19
                                                           /*(SELECT station_name FROM channels ch WHERE ch.id = fre.channel_id_2)*/ NULL AS channel_name_2, #20
                                                           first_airing_2, #21
                                                           fre.channel_id_3, #22
                                                           /*(SELECT station_name FROM channels ch WHERE ch.id = fre.channel_id_3)*/ NULL AS channel_name_3, #23
                                                           first_airing_3, #24
                                                           fre.include_mp3, #25
                                                           fre.pksid, #26
                                                           s.duration, #27
														   s.title,IF(sfe.song_id IS NOT NULL,1,0),s.id
                                                    FROM feed_report fr
													INNER JOIN feed_results_email fre ON fr.id = fre.header_id
                                                    LEFT JOIN advertisers a ON a.id = fre.advertiser_id
                                                    LEFT JOIN brands b ON b.id = fre.brand_id
                                                    LEFT JOIN song_transcript st ON st.id = fre.ad_transcript_id
                                                    LEFT JOIN industries i ON i.id = fre.industry_id
                                                    LEFT JOIN ad_categories ac ON ac.id = ad_category_id
                                                    LEFT JOIN songs s ON s.pksid = fre.pksid
													LEFT JOIN song_feedexcluded sfe ON s.id = sfe.song_id
                                                    WHERE fr.report_id = @reportId
                                  " + additionalFilter + @"
                                ORDER BY first_airing_1",
                    dr => new FeedResultItem()
                    {
                        AdvertiserId = dr.GetStringOrDefault(4),
                        AdvertiserName = dr.GetStringOrDefault(5),
                        BrandId = dr.GetStringOrDefault(6),
                        BrandName = dr.GetStringOrDefault(7),
                        AdTranscriptId = dr.GetStringOrDefault(8),
                        AdTranscript = dr.GetStringOrDefault(9),
                        IndustryId = dr.GetStringOrDefault(12),
                        IndustryName = dr.GetStringOrDefault(13),
                        CategoryId = dr.GetStringOrDefault(14),
                        CategoryName = dr.GetStringOrDefault(15),
                        RegionsIds = dr.GetStringOrDefault(10),
                        MarketsIds = dr.GetStringOrDefault(11),
                        Regions = GetRegionsForIds(dr.GetStringOrDefault(10)),
                        Markets = GetMarketsForIds(dr.GetStringOrDefault(11)),
                        Channels = new List<FeedResultItemChannel>() {
                            new FeedResultItemChannel() { Id = dr.GetStringOrDefault(16), Name = dr.GetStringOrDefault(17), FirstAiringDateTime = dr.GetDateOrDefault(18) }/*,
                            new FeedResultItemChannel() { Id = dr.GetStringOrDefault(19), Name = dr.GetStringOrDefault(20), FirstAiringDateTime = dr.GetDateOrDefault(21) },
                            new FeedResultItemChannel() { Id = dr.GetStringOrDefault(22), Name = dr.GetStringOrDefault(23), FirstAiringDateTime = dr.GetDateOrDefault(24) }*/
                        }.Where(x => !string.IsNullOrEmpty(x.Id)).ToList(),
                        Mp3Url = Song.GetMp3Url(dr.GetStringOrDefault(26)),
                        Duration = dr.GetDecimal(27),
                        Mp3Id = dr.GetStringOrDefault(26),
						SongTitle = dr.GetStringOrDefault(28),
						FeedFilterId = dr.GetInt32(1),
						SongExcluded = dr.GetBoolOrNull(29),
						SongId = dr.GetGuidOrNull(30)
                    },
                    "@reportId", reportId
                );


            if (!string.IsNullOrWhiteSpace(filter.regionMarket))
                list = list.Where(x => /*x.Markets.ToLower().Contains(filter.regionMarket.ToLower()) ||*/ x.Regions.ToLower().Contains(filter.regionMarket.ToLower())).ToList();

            if (!string.IsNullOrWhiteSpace(filter.channel))
                list = list.Where(x => x.Channels.Any(y => y.Name.ToLower().Contains(filter.channel.ToLower()))).ToList();


            switch (sortColumn)
            {

                case "regionMarket":
                    list = list.OrderByWithDirection(o => o.Regions, !ascending).ToList();
                    break;
                case "channel":
                    list = list.OrderByWithDirection(o => o.Channels.FirstOrDefault().Name, !ascending).ToList();
                    break;
                case "firstAiring":
                    list = list.OrderByWithDirection(o => o.Channels.FirstOrDefault().FirstAiringDateTime, !ascending).ToList();
                    break;
                case "brand":
                default:
                    list = list.OrderByWithDirection(o => o.BrandName, !ascending).ToList();
                    break;
                    //case "advertiserBrand":
                    //default:
                    //    list = list.OrderByWithDirection(o => o.AdvertiserName, !ascending).ToList();
                    //    break;
            }

            totalCount = list.Count();

			

            if (pageSize > 0)
                list = list.GetRange(pageNum * pageSize, list.Count < (pageNum + 1) * pageSize ? list.Count - (pageNum * pageSize) : pageSize);

            return list;
        }

        private static string GetRegionsForIds(string regionIds)
        {
            var results = new List<string>();

            var regionIdsList = regionIds.Split(',').Select(x => x.Trim()).ToList();

            foreach (var id in regionIdsList)
            {
                results.Add(Database.ItemFetcher("SELECT domain_name FROM domains WHERE id = @id", dr => dr.GetString(0), "@id", id));
            }

            return string.Join(", ", results);
        }

        private static string GetMarketsForIds(string marketIds)
        {
            var results = new List<string>();

            var marketIdsList = marketIds.Split(',').Select(x => x.Trim()).ToList();

            foreach (var id in marketIdsList)
            {
                results.Add(Database.ItemFetcher("SELECT name FROM markets WHERE id = @id", dr => dr.GetString(0), "@id", id));
            }

            return string.Join(", ", results);
        }

        public static List<string> GetEmails(long feedFilterId)
        {
            return Database.ListFetcher("SELECT c.email FROM contacts c INNER JOIN contacts_feedfilter cf ON c.contact_id = cf.contact_id  WHERE cf.feed_filter_id = @id",
                dr => dr.GetStringOrDefault(0),
                "@id", feedFilterId
            );
        }

		public static List<Contact> GetContacts(long feedFilterId)
		{
			return Database.ListFetcher("SELECT c.contact_id, c.name, c.email FROM contacts c INNER JOIN contacts_feedfilter cf ON c.contact_id = cf.contact_id  WHERE cf.feed_filter_id = @id",
				dr => new Contact {
					contact_id = dr.GetInt32(0),
					name = dr.GetStringOrDefault(1),
					email = dr.GetStringOrDefault(2),
				},
				"@id", feedFilterId
			);
		}

		public static void SetFeedFilterTimestamp(long feedFilterId, DateTime clientDate)
        {
            Database.ExecuteNonQuery(@"UPDATE feed_filters SET timestamp = @clientDate WHERE id = @id", "@id", feedFilterId, "@clientDate", clientDate);
        }

        public static void SaveEmailFeedResults(FeedReport report)
        {
			using (var conn = Database.Get())
			{
				MySqlTransaction tr = null;
				try
				{
					tr = conn.BeginTransaction();

					var header_Id = Database.Insert(conn, tr, @"INSERT INTO feed_report(feed_filter_id, time_inserted, report_id, includemp3, feed_timestamp)
											VALUES(@feed_filter_id, @time_inserted, @report_id, @includemp3, @feed_timestamp)", 
												"@feed_filter_id", report.FeedFilterId,
												"@time_inserted", report.TimeInserted,
												"@report_id", report.reportId,
												"@includemp3", report.IncludeMp3,
												"@feed_timestamp", report.FeedTimeStamp
												);

					foreach (var item in report.Items)
					{
						Database.Insert(conn, tr, @"insert into feed_results_email(header_id, advertiser_id, brand_id, ad_transcript_id, region_ids, market_ids, industry_id, 
		                            ad_category_id, channel_id_1, first_airing_1, channel_id_2, first_airing_2, channel_id_3, first_airing_3, pksid, sent_in_email) 
                                    values(@header_id, @advertiser_id, @brand_id, @ad_transcript_id, @region_ids, @market_ids, @industry_id,
                                    @ad_category_id, @channel_id_1, @first_airing_1, @channel_id_2, @first_airing_2, @channel_id_3, @first_airing_3,@pksid, @sent_in_email)",

										"@header_id", header_Id,
										"@advertiser_id", item.AdvertiserId,
										"@brand_id", item.BrandId,
										"@ad_transcript_id", item.AdTranscriptId,
										"@region_ids", item.RegionsIds,
										"@market_ids", item.MarketsIds,
										"@industry_id", item.IndustryId,
										"@ad_category_id", item.AdCategoryId,
										"@channel_id_1", item.Channel1Id,
										"@first_airing_1", item.FirstAiring1,
										"@channel_id_2", item.Channel2Id,
										"@first_airing_2", item.FirstAiring2,
										"@channel_id_3", item.Channel3Id,
										"@first_airing_3", item.FirstAiring3,
										"@pksid", item.PKSID,
										"@sent_in_email", item.SentInEmail
										);
					}

					foreach(var c in report.Contacts)
					{
						Database.Insert(conn, tr, "INSERT INTO feed_report_contact(report_id, contact_id) VALUES(@report_id, @contact_id)",
										"@report_id", header_Id, "@contact_id", c.contact_id);
					}

					tr.Commit();
				}
				catch
				{
					if (tr != null)
						tr.Rollback();
					throw;
				}
				
			}
				

            
        }

        public static void GenerateResults(HttpContext context, string baseUrl, long feedFilterId, DateTime? cutOffDate, AjaxFeedFilter filter)
        {
            IWorkbook workbook = new XSSFWorkbook();

			var feedFilter = GetFeedFilter(feedFilterId);

			var illegal = Path.GetInvalidFileNameChars();

			var fileName = feedFilter.ClientName;
			foreach (var c in illegal)
			{
				fileName = fileName.Replace(c, '_');
			}

            ICellStyle hlink_style = workbook.CreateCellStyle();
			hlink_style.VerticalAlignment = VerticalAlignment.Top;
            IFont hlink_font = workbook.CreateFont();
            hlink_font.Underline = FontUnderlineType.Single;
            hlink_font.Color = IndexedColors.Blue.Index;
            hlink_style.SetFont(hlink_font);

            ISheet sheet = workbook.CreateSheet(fileName);

            var includeMp3 = Database.Exists(@"SELECT 1 FROM feed_filters WHERE id = @id && include_mp3 = 1", "@id", feedFilterId);

            int totalCount;
            var feed = GetFeed(feedFilterId, cutOffDate, 0, 0, "advertiser", true, filter, null, out totalCount);

            int row = 0;
            sheet
                .CreateRow(row)
                .CreateCell(0)
                .SetCellValue(fileName);
            ++row;

            string[] headers = { "Advertiser", "Brand", "Ad Transcript", "Country / Market", "Industry", "Category", "Channels", "First Airing","Play" };

			var headerStyle = (XSSFCellStyle)workbook.CreateCellStyle();
			var font = workbook.CreateFont();
			font.FontHeightInPoints = 11;
			font.FontName = "Calibri";
			font.Boldweight = (short) FontBoldWeight.Bold;
			headerStyle.SetFont(font);

			var sheetHeadersRow = sheet.CreateRow(row);
            for (var i = 0; i < headers.Count(); i++)
            {
                sheetHeadersRow.CreateCell(i).SetCellValue(headers[i]);
				sheetHeadersRow.Cells[i].CellStyle = headerStyle;
            }

			var transcriptCellStyle = (XSSFCellStyle)workbook.CreateCellStyle();
			transcriptCellStyle.WrapText = true;
			transcriptCellStyle.VerticalAlignment = VerticalAlignment.Top;

			var cellStyle = (XSSFCellStyle)workbook.CreateCellStyle();
			cellStyle.VerticalAlignment = VerticalAlignment.Top;

			sheet.SetColumnWidth(2, 60 * 256);
            

            foreach (var result in feed.Where(f=>f.SongExcluded != true))
            {
                ++row;
                var sheetRow = sheet.CreateRow(row);
                sheetRow.CreateCell(0).SetCellValue(result.AdvertiserName);
                sheetRow.CreateCell(1).SetCellValue(result.BrandName);
				var transcriptCell = sheetRow.CreateCell(2);
				transcriptCell.SetCellValue(new XSSFRichTextString(result.AdTranscript));
				transcriptCell.CellStyle = transcriptCellStyle;
				
                sheetRow.CreateCell(3).SetCellValue(result.Regions + " " + result.Markets);
                sheetRow.CreateCell(4).SetCellValue(result.IndustryName);
                sheetRow.CreateCell(5).SetCellValue(result.CategoryName);
                sheetRow.CreateCell(6).SetCellValue(string.Join("\r\n", result.Channels.Select(c => c.Name)));
                sheetRow.CreateCell(7).SetCellValue(string.Join("\r\n", result.Channels.Select(c => c.FirstAiringDateTime)));

                //if (includeMp3)
                {
                    var playCell = sheetRow.CreateCell(8);
                    playCell.SetCellValue("Play");
                    var url = baseUrl + "/#/feed-playback/" + result.Mp3Id;
                    XSSFHyperlink link = new XSSFHyperlink(HyperlinkType.Url);
                    link.Address = (url);
                    playCell.Hyperlink = (link);
                    playCell.CellStyle = (hlink_style);
                }

				for (int i = 0; i < sheetRow.Cells.Count; i++)
				{
					if (i != 2 && i!= sheetRow.Cells.Count -1)
						sheetRow.Cells[i].CellStyle = cellStyle;
				}
            }


            var totalColumns = headers.Length + 1;

            for (int n = 0; n < totalColumns; n++)
            {
				if(n != 2)
					sheet.AutoSizeColumn(n);
            }

            MemoryStream ms = new MemoryStream();
            workbook.Write(ms);

            var content = ms.ToArray();

            context.Response.Clear();
            context.Response.ClearHeaders();
            context.Response.ContentType = "application/application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            context.Response.AddHeader("content-disposition", String.Format("attachment; filename={0}.xlsx",fileName));
            context.Response.AddHeader("content-length", content.LongLength.ToString());
            context.Response.OutputStream.Write(content, 0, content.Length);
            context.Response.Flush();
            context.Response.End();
        }

        public static FeedResultSongDetails GetFeedResultSongDetails(string pksid, string reportId, string feedFilterId)
        {
            string query = "";

            if (reportId == "0" || string.IsNullOrEmpty(reportId))
            {

                query = @"
                            SELECT s.pksid, s.duration, a.company_name, b.brand_name, ac.category_name, 
						    st.full_text, i.industry_name, s.title, s.created
                            FROM songs s
                            LEFT JOIN brands b ON b.id = s.brand_id
                            LEFT JOIN ad_categories ac ON ac.id = s.category_id
                            LEFT JOIN advertisers a ON a.id = b.advertiser_id
                            LEFT JOIN song_transcript st ON st.song_id = s.id
                            LEFT JOIN industries i ON i.id = ac.industry_id
                            WHERE s.pksid = @pksid";

                var feedItem = Database.ItemFetcher(query,
                    dr => new FeedResultSongDetails()
                    {
                        Mp3Url = Song.GetMp3Url(dr.GetStringOrDefault(0)),
                        VideoUrl = Song.GetVideoUrl(dr.GetStringOrDefault(0)),
                        Duration = dr.GetDecimal(1),
                        Advertiser = dr.GetStringOrDefault(2),
                        Brand = dr.GetStringOrDefault(3),
                        Category = dr.GetStringOrDefault(4),
                        FullTranscript = dr.GetStringOrDefault(5),
                        Industry = dr.GetStringOrDefault(6),
                        Title = dr.GetStringOrDefault(7),
                        Created = dr.GetDateOrDefault(8)
                    },
                    "@pksid", pksid
                );

                return feedItem;

            }
            else
            {
                query = @"
                            SELECT fre.id, #0
	                                fre.feed_filter_id, #1
                                    fre.client_id, #2
                                    fre.timestamp, #3
                                    fre.advertiser_id, #4
                                    a.company_name, #5
                                    fre.brand_id, #6
                                    b.brand_name, #7
                                    fre.ad_transcript_id, #8
                                    ifnull(st.full_text, s.title), #9
                                    fre.region_ids, #10
                                    fre.market_ids, #11
                                    fre.industry_id, #12
                                    i.industry_name, #13
                                    fre.ad_category_id, #14
                                    ac.category_name, #15
                                    fre.channel_id_1, #16
                                    (SELECT station_name FROM channels ch WHERE ch.id = fre.channel_id_1) channel_name_1, #17
                                    first_airing_1, #18
                                    fre.channel_id_2, #19
                                    (SELECT station_name FROM channels ch WHERE ch.id = fre.channel_id_2) channel_name_2, #20
                                    first_airing_2, #21
                                    fre.channel_id_3, #22
                                    (SELECT station_name FROM channels ch WHERE ch.id = fre.channel_id_3) channel_name_3, #23
                                    first_airing_3, #24
                                    fre.include_mp3, #25
                                    fre.pksid, #26
                                    s.duration, #27
                                    s.created, #28
                                    s.title #29
                            FROM feed_results_email fre
                            LEFT JOIN advertisers a ON a.id = fre.advertiser_id
                            LEFT JOIN brands b ON b.id = fre.brand_id
                            LEFT JOIN song_transcript st ON st.id = fre.ad_transcript_id
                            LEFT JOIN industries i ON i.id = fre.industry_id
                            LEFT JOIN ad_categories ac ON ac.id = ad_category_id
                            LEFT JOIN songs s ON s.pksid = fre.pksid
                            WHERE report_id = @reportId
                            AND fre.pksid = @pksid
        ORDER BY first_airing_1";

                var feedItem = Database.ItemFetcher(query,
                    dr => new FeedResultItem()
                    {
                        AdvertiserId = dr.GetStringOrDefault(4),
                        AdvertiserName = dr.GetStringOrDefault(5),
                        BrandId = dr.GetStringOrDefault(6),
                        BrandName = dr.GetStringOrDefault(7),
                        AdTranscriptId = dr.GetStringOrDefault(8),
                        AdTranscript = dr.GetStringOrDefault(9),
                        IndustryId = dr.GetStringOrDefault(12),
                        IndustryName = dr.GetStringOrDefault(13),
                        CategoryId = dr.GetStringOrDefault(14),
                        CategoryName = dr.GetStringOrDefault(15),
                        RegionsIds = dr.GetStringOrDefault(10),
                        MarketsIds = dr.GetStringOrDefault(11),
                        Regions = GetRegionsForIds(dr.GetStringOrDefault(10)),
                        Markets = GetMarketsForIds(dr.GetStringOrDefault(11)),
                        Channels = new List<FeedResultItemChannel>() {
                            new FeedResultItemChannel() { Id = dr.GetStringOrDefault(16), Name = dr.GetStringOrDefault(17), FirstAiringDateTime = dr.GetDateOrDefault(18) },
                            new FeedResultItemChannel() { Id = dr.GetStringOrDefault(19), Name = dr.GetStringOrDefault(20), FirstAiringDateTime = dr.GetDateOrDefault(21) },
                            new FeedResultItemChannel() { Id = dr.GetStringOrDefault(22), Name = dr.GetStringOrDefault(23), FirstAiringDateTime = dr.GetDateOrDefault(24) }
                        }.Where(x => !string.IsNullOrEmpty(x.Id)).ToList(),
                        Mp3Url = Song.GetMp3Url(dr.GetStringOrDefault(26)),
                        Duration = dr.GetDecimal(27),
                        Mp3Id = dr.GetStringOrDefault(26),
                        SongCreated = dr.GetDateOrDefault(28),
                        SongTitle = dr.GetStringOrDefault(29)
                    },
                    "@reportId", reportId,
                    "@pksid", pksid
                );

                return new FeedResultSongDetails()
                {
                    Mp3Url = feedItem.Mp3Url,
                    VideoUrl = Song.GetVideoUrl(pksid),
                    Duration = feedItem.Duration,
                    Advertiser = feedItem.AdvertiserName,
                    Brand = feedItem.BrandName,
                    Category = feedItem.CategoryName,
                    FullTranscript = feedItem.AdTranscript,
                    Industry = feedItem.IndustryName,
                    Market = feedItem.Markets,
                    Region = feedItem.Regions,
                    Title = feedItem.SongTitle,
                    Created = feedItem.SongCreated,
                    Channels = feedItem.Channels
                };
            }
        }

		public static List<FeedReport> GetFeedReports(long feedFilterId)
		{
			return Database.ListFetcher(@"SELECT fr.id, fr.time_inserted, fr.report_id, COUNT(*) FROM feed_report fr INNER JOIN feed_results_email fre ON fr.id = fre.header_id
				WHERE fr.feed_filter_id = @feedFilterId GROUP BY fr.id,fr.time_inserted, fr.report_id ",
				dr => new FeedReport
				{
					Id = dr.GetInt32(0),
					TimeInserted = dr.GetDateTime(1),
					reportId = dr.GetString(2),
					ItemCount = dr.GetIntOrDefault(3)
				},
				"@feedFilterId", feedFilterId
				);
		}

		public static FeedReport GetLastFeedReportItemInfo(long feedFilterId)
		{
			return Database.ItemFetcher(@"SELECT fr.time_inserted, fr.report_id, COUNT(*) 
						FROM feed_report fr INNER JOIN feed_results_email fre ON fr.id = fre.header_id
						WHERE fr.feed_filter_id = @feedFilterId 
 					    GROUP BY fr.id
						ORDER BY fr.time_inserted DESC LIMIT 1",
				dr => new FeedReport
				{
					TimeInserted = dr.GetDateTime(0),
					reportId = dr.GetString(1),
					ItemCount = dr.GetIntOrDefault(2)
				},
				"@feedFilterId", feedFilterId
				);
		}

		public static void UpdateSongFeedStatus(string userId, DateTime timeStamp, IList<FeedResultItem> songs = null)
		{
			
			if(songs != null)
			{
				Log.Info(string.Format("Updating song status. Songs: {0}", string.Join(",", songs.Select(s => s.SongId))));
				Database.ExecuteNonQuery("DELETE FROM song_feedexcluded WHERE song_id " + Database.InClause(songs.Select(s => s.SongId.ToString())));
				var ids = songs.Where(s => s.SongExcluded == true).Select(s => s.SongId.ToString()).ToList();
				if(ids.Count > 0)
					Database.ExecuteNonQuery("INSERT INTO song_feedexcluded (song_id, user_id, `timestamp`) SELECT id,@user_id, @timestamp FROM songs WHERE id " + Database.InClause(ids),
						"@user_id", userId, "@timestamp", timeStamp);
			}
		}

		public class FeedContact
		{
			public int FeedFilterId { get; set; }
			public Contact Contact { get; set; }
		}

		public static void UpdateNewMatchCount(Feed feed)
		{
			var timestamp = feed.LastTimestamp != null ? (DateTime?)DateTime.Parse(feed.LastTimestamp) : null;
			if (timestamp != null)
			{
				if (timestamp < DateTime.Now.AddDays(-30))
					timestamp = DateTime.Now.AddDays(-30);
			}
			try
			{
				var newMatchCount = GetCountForFeed(feed.Id, timestamp);
				if(Database.Exists("SELECT feed_id FROM feed_newmatchcount WHERE feed_id = @id", "@id", feed.Id))
					Database.ExecuteNonQuery("UPDATE feed_newmatchcount SET new_matches = @num WHERE feed_id = @id", "@num", newMatchCount, "@id", feed.Id);
				else
					Database.ExecuteNonQuery("INSERT INTO feed_newmatchcount (feed_id, new_matches) VALUES(@id, @num)", "@id", feed.Id, "@num", newMatchCount);
			}
			catch(Exception ex)
			{
				Log.Error(ex);
			}
			
		}
	}

	public class FeedNewMatchCountService
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		Thread _queueingThread;
		bool _shouldStop;
		public void Start()
		{
			_queueingThread = new Thread(FeedNewMatchCountServiceThreadProc) { Name = "Feed count Service" };
			_shouldStop = false;
			_queueingThread.Start();
		}
		public void Stop()
		{
			_shouldStop = true;
			_queueingThread.Join();
		}
		void FeedNewMatchCountServiceThreadProc()
		{
			while (!_shouldStop)
			{
				
				var feeds = Feeds.GetFeeds();
				foreach(var feed in feeds)
				{
					try
					{
						Feeds.UpdateNewMatchCount(feed);
					}
					catch (Exception ex)
					{
						Log.Info(string.Format("update new matches count failed for feed id: {0}, Error: {1}",feed.Id,  ex.ToString()));
					}
						
				}
				Thread.Sleep(TimeSpan.FromMinutes(3));
			}
		}
	}

	public class FeedMatchCount
	{
		public string song_id;
		public string channel_id;
		public double? MatchStart {get;set;}
		public double? MatchEnd {get;set;}
		public decimal? Duration { get; set; }
		public decimal? MatchThreshold {get;set;}
		public bool Valid
		{
			get
			{
				if(MatchStart == null || MatchEnd == null)
					return false;
				return (MatchEnd.Value - MatchStart.Value) > Convert.ToDouble(Duration * MatchThreshold);
			}
		}
	}
}
