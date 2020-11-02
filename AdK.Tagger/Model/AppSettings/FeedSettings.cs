using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model.AppSettings
{
    public class FeedSettings
    {
        private const string MODULE = "Feeds";

        public string FeedMailSubject { get; set; }
        public string FeedMailBody { get; set; }
		public List<Product> FeedProducts { get; set; }
		public int TimeZoneOffset { get; set; }
		public string AdFeedEmptyMessage { get; set; }
		public string FeedMailBcc { get; set; }
		public int FirstAiringChannels { get; set; }


		public static FeedSettings Get()
        {
			return new FeedSettings
			{
				FeedMailSubject = Settings.Get(MODULE, "FeedMailSubject", "New Ads"),
				FeedMailBody = Settings.Get(MODULE, "FeedMailBody"),
				FeedProducts = GetProductsFromIds(Settings.Get(MODULE, "FeedProducts")),
				TimeZoneOffset = Convert.ToInt32(Settings.Get(MODULE, "TimeZoneOffset", 0)),
				AdFeedEmptyMessage = Settings.Get(MODULE, "AdFeedEmptyMessage", ""),
				FeedMailBcc = Settings.Get(MODULE, "FeedMailBcc",""),
				FirstAiringChannels = Convert.ToInt32(Settings.Get(MODULE, "FirstAiringChannels"))
			};
        }

		private static List<Product> GetProductsFromIds(string setting)
		{
			var result = new List<Product>();
			if(!string.IsNullOrEmpty(setting))
			{
				var ids = setting.Split(',');
				result = Product.GetByIds(ids);
			}
			return result;
		}

		public static void Save(FeedSettings settings)
        {
            Settings.Set(MODULE, "FeedMailSubject", settings.FeedMailSubject);
            Settings.Set(MODULE, "FeedMailBody", settings.FeedMailBody);
			Settings.Set(MODULE, "FeedProducts", string.Join(",", settings.FeedProducts.Select(p => p.Id.ToString())));
			Settings.Set(MODULE, "AdFeedEmptyMessage", settings.AdFeedEmptyMessage );
			Settings.Set(MODULE, "FeedMailBcc", settings.FeedMailBcc);
			Settings.Set(MODULE, "FirstAiringChannels", settings.FirstAiringChannels);
		}
    }
}
