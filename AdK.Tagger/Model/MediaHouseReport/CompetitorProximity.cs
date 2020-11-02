using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using DatabaseCommon;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace AdK.Tagger.Model.MediaHouseReport
{
    #region Additional Classes
    public class CompetitorProximityBlock
    {
        public CompetitorProximityBlock()
        {
            Ads = new List<CompetitorProximityBlockAd>();
        }
        public string ChannelName { get; set; }
        public DateTime Start { get; set; }

        public string HeaderString { get { return string.Format("{0}, {1}, {2}", ChannelName, Start.ToString("dd.MM.yyyy"), Start.ToString("HH:mm:ss")); } }

        public List<CompetitorProximityBlockAd> Ads { get; set; }
    }

    public class CompetitorProximityBlockAd
    {
        public DateTime Start { get; set; }
        public string StartString { get { return Start.ToString("HH:mm:ss"); } }
        public string Advertiser { get; set; }
        public string Brand { get; set; }
        public string SpotTitle { get; set; }
        public double Duration { get; set; }
        public string DurationString
        {
            get
            {
                if (Duration <= 0) return "";

                var t = TimeSpan.FromSeconds(Duration);
                return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            }
        }
        public string Industry { get; set; }
        public string Category { get; set; }
        public bool IsFocusBrand { get; set; }
        public bool IsCompetitorCategory { get; set; }
    }

    #endregion


    public class CompetitorProximity : ReportBase
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public List<CompetitorProximityBlock> AdBlocks { get; set; }
        public CompetitorProximity(string userId, Guid focusChannelId, IncludeSet include, string focusBrandId, DateTime date) :
            base(userId, focusChannelId, include)
        {

            AdBlocks = new List<CompetitorProximityBlock>();

            using (var conn = Database.Get())
            {
                _getAdBlocks(focusChannelId, date, focusBrandId, conn);

            }

        }

        private void _getAdBlocks(Guid focusChannelId, DateTime date, string focusBrandId, MySql.Data.MySqlClient.MySqlConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = string.Format(@"
                                                SELECT 
                                                    m.channel_id,
                                                    MIN(m.match_start),
                                                    MAX(m.match_end),
                                                    m.match_occurred,
                                                    m.song_id,
                                                    ifnull(s.performer, '') AS advertiser_name,
                                                    ifnull(s.brand_id, '') as brand_id,
                                                    ifnull(s.brand, '') AS brand_name,
                                                    ifnull(s.title, '') AS spot_title,
                                                    ifnull(s.album, '') AS industry_name,
                                                    ifnull(s.role, '') AS category_name
                                                FROM
                                                    matches m
                                                        JOIN
                                                    songs AS s ON m.song_id = s.id
                                                        JOIN
                                                    accounts a ON a.user_id = s.user_id
                                                        JOIN
                                                    (SELECT 
                                                        ch.id channel_id, ch.match_threshold
                                                    FROM
                                                        channels ch
                                                    WHERE
                                                        ch.id = '{0}') x1 ON x1.channel_id = m.channel_id
                                                WHERE
                                                    (DATE(m.match_occurred) = DATE(@date))
                                                        AND s.duration > 0
                                                        AND m.match_end - m.match_start >= s.duration * x1.match_threshold
                                                        AND s.deleted = 0
                                                        AND s.suppress_chart = 0
                                                        AND a.suppress_chart = 0
                                                GROUP BY m.channel_id , m.match_occurred , m.song_id , s.performer , s.brand_id , s.brand , s.title , s.album , s.role

                                             ", focusChannelId);

            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@yesterday", date.AddDays(-1));

            Log.Info(string.Format(@"
                    SET @date:='{0:yyyy-MM-dd}'; 
                    SET @yesterday:='{1:yyyy-MM-dd}'; 
                    {2}
                ", date, date.AddDays(-1), cmd.CommandText));

            var dbRows = new List<RawDbMatchItem>();
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    dbRows.Add(new RawDbMatchItem
                    {
                        ChannelId = dr.GetGuid(0),
                        MatchStart = dr.GetDouble(1),
                        MatchEnd = dr.GetDouble(2),
                        MatchOccured = dr.GetDateTime(3),
                        //SongId = dr.GetGuid(4),
                        Advertiser = dr.GetString(5),
                        BrandId = dr.GetString(6),
                        BrandName = dr.GetString(7),
                        Title = dr.GetString(8),
                        Industry = dr.GetString(9),
                        Category = dr.GetString(10)
                    });
                }
            }


            if (dbRows.Count == 0)
                return;

            //Group matches in blocks
            var channelBlocks = ChannelAdBlockGenerator.Instance.GroupByChannelBlocks(dbRows);

            var channelName = Channels.First(c => c.Id == focusChannelId).Name;

            var ChannelAdBlocks = channelBlocks[focusChannelId];

            foreach (var adBlock in ChannelAdBlocks)
            {
                var brandCategories = new HashSet<string>();

                var block = new CompetitorProximityBlock()
                {
                    ChannelName = channelName,
                    Start = adBlock.Items.Min(a => a.MatchOccured)
                };

                var hasFocusBrandAd = false;

                //TEMP
                //var categoryCounter = new Dictionary<string, int>();

                foreach (var item in adBlock.Items)
                {
                    var ad = new CompetitorProximityBlockAd()
                    {
                        Start = item.MatchOccured,
                        Advertiser = item.Advertiser,
                        Brand = item.BrandName,
                        SpotTitle = item.Title,
                        Duration = item.DurationInSeconds,
                        Industry = item.Industry,
                        Category = item.Category,
                        IsFocusBrand = false,
                        IsCompetitorCategory = false
                    };

                    //TEMP
                    //int tempCount;
                    //if (categoryCounter.TryGetValue(item.Category, out tempCount))
                    //{
                    //    categoryCounter[item.Category] = tempCount + 1;
                    //}
                    //else
                    //{
                    //    categoryCounter[item.Category] = 1;
                    //}


                    if (item.BrandId.ToString() == focusBrandId)
                    {
                        brandCategories.Add(item.Category);
                        ad.IsFocusBrand = true;
                        hasFocusBrandAd = true;
                    }


                    block.Ads.Add(ad);
                }

                if (!hasFocusBrandAd || block.Ads.Count < 3)
                    continue;


                var competitorCategoryCounter = 0;
                foreach (var ad in block.Ads)
                {

                    ad.IsCompetitorCategory = brandCategories.Contains(ad.Category) && !ad.IsFocusBrand;
                    if (ad.IsCompetitorCategory)
                        competitorCategoryCounter++;
                }

                if (competitorCategoryCounter < 2)
                    continue;

                //TEMP
                //if (categoryCounter.Max(c => c.Value) < 3)
                //    continue;

                AdBlocks.Add(block);
            }

        }

    }

}