using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;
using AdK.Tagger.Model.AppSettings;
using System.Net.Mail;
using System.Net;


namespace AdK.Tagger.Model.MailTemplates
{
    public class FeedMail
    {
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		public static void Send(long feedFilterId, string userId, DateTime? timeStampTo = null)
        {
            Uri url = HttpContext.Current.Request.Url;
            //TODO
            string baseHrefUrl = string.Format("{0}://{1}{2}/login.html?href=",
                 url.Scheme,
                 url.Authority,
                 HttpRuntime.AppDomainAppVirtualPath == "/" ? "" : HttpRuntime.AppDomainAppVirtualPath);

			string basePublicHrefUrl = string.Format("{0}://{1}{2}/public.html#/",
				 url.Scheme,
				 url.Authority,
				 HttpRuntime.AppDomainAppVirtualPath == "/" ? "" : HttpRuntime.AppDomainAppVirtualPath);

			var reportId = Guid.NewGuid().ToString();

			Log.Info("FeedMail.Send getting feed filter " + feedFilterId.ToString());
            var feedFilter = Feeds.GetFeedFilter(feedFilterId);
			Log.Info("FeedMail.Send obtained feed filter");

			Log.Info("FeedMail.Send getting time zone offset");
			var timeZoneOffset = Feeds.GetTimeZoneOffset(filter: feedFilter);
			Log.Info("FeedMail.Send obtained time zone offset");

			Log.Info("FeedMail.Send getting feed settings");
			var settings = FeedSettings.Get();

            var subject = settings.FeedMailSubject;

			var mindate = DateTime.Now.AddHours(-1 * timeZoneOffset).AddDays(-30);
			var timestamp = feedFilter.Timestamp >= mindate ? feedFilter.Timestamp : mindate;

			Log.Info(string.Format("FeedMail.Send getting feed data for feedFilterId={0} with timestamp {1}", feedFilterId, timestamp));
			
            var feed = Feeds.GetFeed(feedFilterId, timestamp, dateTo: timeStampTo).Where(x => x.SongExcluded != true);

            var userDateFormat = UserSettings.GetUserDateFormat(userId);

            userDateFormat = userDateFormat.Equals("shortDate") ? "d": userDateFormat;

            var feedListForTemplate =
                feed.Select(x => new object[] {
                    //"playbackPageUrl", feedFilter.IncludeMp3 ? "cid:" + x.Mp3Id + "@adkontrol.com" : baseHrefUrl + "feed-playback/" + x.Mp3Id + "/" + reportId,
					"playbackPageUrl", basePublicHrefUrl + "playback/" + x.Mp3Id + (feedFilter.IncludeMp3 ? "/false" : ""),
					"advertiserName", x.AdvertiserName,
                    "brandName", x.BrandName,
                    "advertiserAndBrandName", x.AdvertiserName.ToLower().Equals(x.BrandName.ToLower()) ? x.AdvertiserName : x.AdvertiserName + " / " + x.BrandName,
                    "media", x.MediaType,
                    "title", x.SongTitle,
                    "duration", DateTime.Today.AddSeconds(Convert.ToDouble(x.Duration??0)).ToString("mm:ss"),
                    "market", x.Markets,
                    //"channelsAndAirings", string.Join(", ", 
                    //                            x.Channels.Select(y => y.Name + " " + 
                    //                                    string.Format(
                    //                                        "{0:"+userDateFormat+" HH:mm:ss}", 
                    //                                        y.FirstAiringDateTime))),
                    "FirstAiringsList", x.Channels.Select(y=> new object[] { "channelName", y.Name, "firstAiringTime",  string.Format("{0:"+userDateFormat+" HH:mm:ss}", y.FirstAiringDateTime)}).ToArray(),
                    "channels", string.Join(", ", x.Channels.Select(z=>z.Name).ToArray()),
                    "adTranscript", x.AdTranscript
                } ).ToArray();


			Log.Info("FeedMail.Send processing template");
			var body = Mailer.ProcessTemplate(settings.FeedMailBody,
                                              "FeedList", feedListForTemplate,
                                              "feedPageUrl", basePublicHrefUrl + "add-feed/" + reportId);

            var contacts = Feeds.GetContacts(feedFilterId);

			Log.Info("FeedMail.Send getting emails of contacts");
						

			List<Attachment> attachments = null;
			if(feedFilter.IncludeMp3)
			{
				var client = new WebClient();
				attachments = new List<Attachment>();
				foreach (var res in feed)
				{
					if(!string.IsNullOrEmpty(res.Mp3Url))
					{
						Log.Info("FeedMail.Send getting attachment: " + res.Mp3Url);
						var stream = client.OpenRead(res.Mp3Url);
						var att = new Attachment(stream, res.SongTitle + ".mp3");
						att.ContentType = new System.Net.Mime.ContentType("audio/mp3");
						att.ContentId = res.Mp3Id + "@adkontrol.com";
						attachments.Add(att);
					}
				}
			}

            foreach (var c in contacts.Where(co=>!string.IsNullOrEmpty(co.email)))
            {
                Mailer.Send(c.email, subject, body, isHtml: true,attachments: attachments, bcc: settings.FeedMailBcc);
            }

			var report = new FeedReport {
				FeedFilterId = feedFilter.Id,
				reportId = reportId,
				TimeInserted = DateTime.Now.AddHours(-1 * timeZoneOffset),
				IncludeMp3 = feedFilter.IncludeMp3,
				FeedTimeStamp = feedFilter.Timestamp,
				Items = feed.Select(x => new FeedResultEmailItem()
				{
					AdvertiserId = x.AdvertiserId,
					BrandId = x.BrandId,
					IndustryId = x.IndustryId,
					AdCategoryId = x.CategoryId,
					AdTranscriptId = x.AdTranscriptId,
					RegionsIds = x.RegionsIds,
					MarketsIds = x.MarketsIds,
					Channel1Id = x.Channels.Count > 0 ? x.Channels[0].Id : null,
					FirstAiring1 = x.Channels.Count > 0 ? x.Channels[0].FirstAiringDateTime : null,
					Channel2Id = x.Channels.Count > 1 ? x.Channels[1].Id : null,
					FirstAiring2 = x.Channels.Count > 1 ? x.Channels[1].FirstAiringDateTime : null,
					Channel3Id = x.Channels.Count > 2 ? x.Channels[2].Id : null,
					FirstAiring3 = x.Channels.Count > 2 ? x.Channels[2].FirstAiringDateTime : null,
					PKSID = x.Mp3Id,
					SentInEmail = true
				}).ToList(),
				Contacts = contacts
			};

            Feeds.SaveEmailFeedResults(report);
						
			

        }
    }
}
