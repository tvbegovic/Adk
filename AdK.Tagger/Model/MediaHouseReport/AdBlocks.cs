using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using AdK.Tagger.Model.Reporting;
using DatabaseCommon;
using MySql.Data.MySqlClient;

namespace AdK.Tagger.Model.MediaHouseReport
{
    #region Additional classes
    public class AdBlockChannelRow
    {
        public AdBlockChannelRow()
        {
            DayRows = new List<AdBlockChannelRowDay>();
        }
        public Guid ChannelId { get; set; }
        public string ChannelName { get; set; }
        public List<AdBlockChannelRowDay> DayRows { get; set; }
    }

    public class AdBlockChannelRowDay
    {
        public AdBlockChannelRowDay()
        {
            Totals = new AdBlockDayRowTotals() { BlockCount = 0, Duration = 0, AdCount = 0 };
            AdBlocks = new List<AdBlockDayRowAdBlock>();
            AdBlockDayRowDayPartSummaries = new List<AdBlockDayRowDayPartSummary>();
        }
        public DateTime Date { get; set; }
        public string DateString { get { return string.Format("{0}.{1}.{2}", Date.Day, Date.Month, Date.Year); } }
        public AdBlockDayRowTotals Totals { get; set; }
        public List<AdBlockDayRowDayPartSummary> AdBlockDayRowDayPartSummaries { get; set; }
        public List<AdBlockDayRowAdBlock> AdBlocks { get; set; }
    }

    public class AdBlockDayRowTotals
    {
        public int BlockCount { get; set; }
        public double Duration { get; set; }
        public string DurationString
        {
            get
            {
                if (Duration <= 0) return "";

                var t = TimeSpan.FromSeconds(Duration);
                return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
            }
        }
        public int AdCount { get; set; }
    }

    public class AdBlockDayRowDayPartSummary
    {
        public AdBlockDayRowDayPartSummary()
        {
            AdCount = 0;
            BlockCount = 0;
            Duration = 0;
        }
        public DayPart DayPart { get; set; }
        public int AdCount { get; set; }
        public int BlockCount { get; set; }
        public double Duration { get; set; }
        public string DurationString
        {
            get
            {
                if (Duration <= 0) return "";

                var t = TimeSpan.FromSeconds(Duration);
                return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
            }
        }
    }

    public class AdBlockDayRowAdBlock
    {
        public AdBlockDayRowAdBlock()
        {
            AdCount = 0;
            AdBlockDayParts = new List<AdBlockDayPart>();
        }
        //ID
        public int AdCount { get; set; }
        public List<AdBlockDayPart> AdBlockDayParts { get; set; }

    }

    public class AdBlockDayPart
    {
        public AdBlockDayPart()
        {
            AdBlockAds = new List<AdBlockAd>();
        }
        public DayPart DayPart { get; set; }
        public DateTime Start { get; set; }
        public string StartTimeString { get { return AdCount <= 0 ? "" : Start.ToString("HH:mm:ss"); } }
        public DateTime End { get; set; }
        public string EndTimeString { get { return AdCount <= 0 ? "" : End.ToString("HH:mm:ss"); } }

        public int AdCount { get; set; }
        public List<AdBlockAd> AdBlockAds { get; set; }
    }

    public class AdBlockAd
    {
        public DateTime Start { get; set; }
        public string StartTimeString { get { return Duration <= 0 ? "" : Start.ToString("HH:mm:ss"); } }

        public double Duration { get; set; }
        public string DurationString
        {
            get
            {
                if (Duration <= 0) return "";

                var t = TimeSpan.FromSeconds(Duration);
                return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
            }
        }

        public Guid SpotId { get; set; }
        public string SpotName { get; set; }
        public Guid BrandId { get; set; }
        public string BrandName { get; set; }
        public string PksId { get; set; }

        public string Mp3Url { get { return Song.GetMp3Url(PksId); } }
    }
    #endregion

    public class AdBlocks : ReportBase
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private DayPartType _dayPart { get; set; }
        public List<DayPart> DayParts { get; set; }
        public Dictionary<int, bool> DayPartVisibility { get; set; }
        public List<AdBlockChannelRow> AdBlockTable { get; set; }

        public AdBlocks(string userId, Guid focusChannelId, IncludeSet include, DateTime date, ChannelOrDate groupBy, DayPartType dayPart) : base(userId, focusChannelId, include)
        {
            _dayPart = dayPart;
            AdBlockTable = new List<AdBlockChannelRow>();
            DayPartVisibility = new Dictionary<int, bool>();

            DayParts = new List<DayPart>();

            if (_dayPart == DayPartType.MyDayParts)
            {
                DayPartSet userDayPartSet = DayPartSet.GetForUser(_UserId).FirstOrDefault();
                if (userDayPartSet == null || !userDayPartSet.Parts.Any())
                {
                    return;
                }

                DayParts = userDayPartSet.Parts;
            }
            else
            {
                DayParts = DayPart.GetAllDayParts();
            }


            using (var conn = Database.Get())
            {
                GetData(conn, include, date, groupBy);
            }


        }

        private void GetData(MySqlConnection conn, IncludeSet include, DateTime date, ChannelOrDate groupBy)
        {

            var cmd = conn.CreateCommand();

            cmd.CommandText = string.Format(@"
                                                SELECT m.channel_id, Min(m.match_start), MAX(m.match_end), m.match_occurred, m.song_id, s.brand_id, b.brand_name, s.title, s.id, s.pksid
                                                FROM matches m
                                                JOIN songs AS s ON m.song_id = s.id
                                                JOIN accounts a ON a.user_id = s.user_id
                                                JOIN brands AS b ON b.id = s.brand_id
                                                join ( select ch.id channel_id, ch.match_threshold from channels ch 
                                                       where ch.id {0}
                                                     ) x1 on x1.channel_id = m.channel_id
                                                where 
                                                m.channel_id {0}
                                                and (DATE(m.match_occurred) = DATE(@date))
                                                and s.duration > 0 AND m.match_end - m.match_start >= s.duration * x1.match_threshold AND s.deleted = 0 AND
                                                      s.suppress_chart = 0 AND a.suppress_chart = 0 
                                                GROUP BY m.channel_id, m.song_id, m.match_occurred, s.brand_id, b.brand_name, s.title, s.id, s.pksid
                                                ORDER BY m.match_occurred
                                             ", Database.InClause(_ChannelIds));

            cmd.Parameters.AddWithValue("@date", date);

            Log.Info(string.Format(@"
                    SET @date:='{0:yyyy-MM-dd}'; 
                    {1}
                ", date, cmd.CommandText));

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
                        BrandId = dr.GetString(5),
                        BrandName = dr.GetString(6),
                        Title = dr.GetString(7),
                        SongId = dr.GetGuid(8),
                        PksId = dr.GetStringOrDefault(9)
                    });
                }
            }

            //Group matches in blocks
            var channelBlocks = ChannelAdBlockGenerator.Instance.GroupByChannelBlocks(dbRows);

            if (groupBy == ChannelOrDate.Channel)
            {
                GroupByChannel(channelBlocks);
            }
            else
            {
                GroupByDate(channelBlocks);
            }

        }

        private void GroupByChannel(Dictionary<Guid, List<ChannelBlock>> channelBlocks)
        {
            foreach (var channelBlock in channelBlocks)
            {
                var row = new AdBlockChannelRow();

                row.ChannelId = channelBlock.Key;
                row.ChannelName = Channels.First(c => c.Id == row.ChannelId).Name;


                var channelDaysList = channelBlock.Value.Select(d => d.EndDate.Date).Distinct().OrderBy(d => d.Date).ToList();

                foreach (var day in channelDaysList)
                {
                    var channelDay = new AdBlockChannelRowDay() { Date = day };


                    var dayRowDayPartSummary = new Dictionary<DayPart, AdBlockDayRowDayPartSummary>();

                    var maxAdBlocksCount = 0;

                    var dictDayPartAdBlockDayPart = new Dictionary<DayPart, Queue<AdBlockDayPart>>();


                    var adBlocks = channelBlock.Value.Where(d => d.EndDate.Date == day).ToList();

                    foreach (var adBlock in adBlocks)
                    {
                        foreach (var dayPart in DayParts)
                        {
                            var adBlockDayPart = new AdBlockDayPart() { DayPart = dayPart, AdCount = 0 };

                            AdBlockDayRowDayPartSummary tmpDayRowDayPartSummary;
                            if (!dayRowDayPartSummary.TryGetValue(dayPart, out tmpDayRowDayPartSummary))
                            {
                                tmpDayRowDayPartSummary = new AdBlockDayRowDayPartSummary()
                                {
                                    DayPart = dayPart,
                                    AdCount = 0,
                                    BlockCount = 0,
                                    Duration = 0
                                };
                                dayRowDayPartSummary.Add(dayPart, tmpDayRowDayPartSummary);
                            }

                            if (dayPart.Hours.Any(h => h.Hour == adBlock.EndDate.Hour))
                            {
                                adBlockDayPart.Start = adBlock.StartDate;
                                adBlockDayPart.End = adBlock.EndDate;
                                adBlockDayPart.AdCount = adBlock.Items.Count();
                                adBlockDayPart.AdBlockAds = adBlock.Items.Select(a => new AdBlockAd() { Start = a.MatchOccured, Duration = a.DurationInSeconds, BrandName = a.BrandName, SpotId = a.SongId, SpotName = a.Title, PksId = a.PksId }).ToList();


                                tmpDayRowDayPartSummary.AdCount += adBlockDayPart.AdCount;
                                tmpDayRowDayPartSummary.BlockCount += 1;
                                tmpDayRowDayPartSummary.Duration += (adBlockDayPart.End - adBlockDayPart.Start).TotalSeconds;

                                channelDay.Totals.AdCount += adBlockDayPart.AdCount;
                                channelDay.Totals.BlockCount++;
                                channelDay.Totals.Duration += (adBlockDayPart.End - adBlockDayPart.Start).TotalSeconds;


                                Queue<AdBlockDayPart> QueueAdBlockDayPart;
                                if (!dictDayPartAdBlockDayPart.TryGetValue(adBlockDayPart.DayPart, out QueueAdBlockDayPart))
                                {
                                    QueueAdBlockDayPart = new Queue<AdBlockDayPart>();
                                    dictDayPartAdBlockDayPart.Add(adBlockDayPart.DayPart, QueueAdBlockDayPart);
                                }

                                if (adBlockDayPart.AdCount > 0)
                                    QueueAdBlockDayPart.Enqueue(adBlockDayPart);

                                maxAdBlocksCount = QueueAdBlockDayPart.Count > maxAdBlocksCount ? QueueAdBlockDayPart.Count : maxAdBlocksCount;
                            }
                        }
                    }



                    channelDay.AdBlockDayRowDayPartSummaries = dayRowDayPartSummary.Select(s => new AdBlockDayRowDayPartSummary() { DayPart = s.Key, AdCount = s.Value.AdCount, BlockCount = s.Value.BlockCount, Duration = s.Value.Duration }).ToList();


                    for (int i = 0; i < maxAdBlocksCount; i++)
                    {
                        var adBlockRow = new AdBlockDayRowAdBlock();
                        foreach (var dayPart in DayParts)
                        {
                            Queue<AdBlockDayPart> QueueAdBlockDayPart;
                            if (!dictDayPartAdBlockDayPart.TryGetValue(dayPart, out QueueAdBlockDayPart))
                            {
                                QueueAdBlockDayPart = new Queue<AdBlockDayPart>();
                            }

                            if (QueueAdBlockDayPart.Count > 0)
                            {
                                var adBlockDayPart = QueueAdBlockDayPart.Dequeue();

                                adBlockRow.AdBlockDayParts.Add(adBlockDayPart);

                                adBlockRow.AdCount += adBlockDayPart.AdCount;
                            }
                            else
                            {
                                adBlockRow.AdBlockDayParts.Add(new AdBlockDayPart() { DayPart = dayPart, AdCount = 0 });
                            }

                        }

                        channelDay.AdBlocks.Add(adBlockRow);
                    }



                    row.DayRows.Add(channelDay);
                }
                AdBlockTable.Add(row);
            }
        }

        private void GroupByDate(Dictionary<Guid, List<ChannelBlock>> channelBlocks)
        {
            //foreach (var channelBlock in channelBlocks)
            //{
            //    var row = new AdBlockChannelRow();

            //    row.ChannelId = channelBlock.Key;
            //    row.ChannelName = Channels.First(c => c.Id == row.ChannelId).Name;

            //    var channelDaysList = channelBlock.Value.Select(d => d.EndDate.Date).Distinct().OrderBy(d => d.Date).ToList();

            //    foreach (var day in channelDaysList)
            //    {
            //        var channelDay = new AdBlockChannelRowDay() { Date = day };


            //        var dayRowDayPartSummary = new Dictionary<DayPart, AdBlockDayRowDayPartSummary>();

            //        var adBlocks = channelBlock.Value.Where(d => d.EndDate.Date == day).ToList();

            //        foreach (var adBlock in adBlocks)
            //        {
            //            var adBlockRow = new AdBlockDayRowAdBlock();

            //            foreach (var dayPart in DayParts)
            //            {
            //                var adBlockDayPart = new AdBlockDayPart() { DayPart = dayPart, AdCount = 0 };

            //                AdBlockDayRowDayPartSummary tmpDayRowDayPartSummary;
            //                if (!dayRowDayPartSummary.TryGetValue(dayPart, out tmpDayRowDayPartSummary))
            //                {
            //                    tmpDayRowDayPartSummary = new AdBlockDayRowDayPartSummary()
            //                    {
            //                        DayPart = dayPart,
            //                        AdCount = 0,
            //                        BlockCount = 0,
            //                        Duration = 0
            //                    };
            //                    dayRowDayPartSummary.Add(dayPart, tmpDayRowDayPartSummary);
            //                }

            //                if (dayPart.Hours.Any(h => h.Hour == adBlock.EndDate.Hour))
            //                {
            //                    adBlockDayPart.Start = adBlock.StartDate;
            //                    adBlockDayPart.End = adBlock.EndDate;
            //                    adBlockDayPart.AdCount = adBlock.Items.Count();
            //                    adBlockDayPart.AdBlockAds = adBlock.Items.Select(a => new AdBlockAd() { Start = a.MatchOccured, Duration = a.DurationInSeconds, BrandName = a.BrandName, SpotName = a.Title }).ToList();

            //                    adBlockRow.AdCount += adBlockDayPart.AdCount;


            //                    tmpDayRowDayPartSummary.AdCount += adBlockDayPart.AdCount;
            //                    tmpDayRowDayPartSummary.BlockCount += 1;
            //                    tmpDayRowDayPartSummary.Duration += (adBlockDayPart.End - adBlockDayPart.Start).TotalSeconds;


            //                    //bool tmpDayPartVisible;
            //                    //DayPartVisibility.TryGetValue(dayPart.Id, out tmpDayPartVisible);
            //                    //DayPartVisibility[dayPart.Id] = tmpDayPartVisible || adBlockDayPart.AdCount > 0;

            //                    channelDay.Totals.AdCount += adBlockDayPart.AdCount;
            //                    channelDay.Totals.BlockCount++;
            //                    channelDay.Totals.Duration += (adBlockDayPart.End - adBlockDayPart.Start).TotalSeconds;
            //                }





            //                adBlockRow.AdBlockDayParts.Add(adBlockDayPart);
            //            }

            //            channelDay.AdBlocks.Add(adBlockRow);
            //        }

            //        channelDay.AdBlockDayRowDayPartSummaries = dayRowDayPartSummary.Select(s => new AdBlockDayRowDayPartSummary() { DayPart = s.Key, AdCount = s.Value.AdCount, BlockCount = s.Value.BlockCount, Duration = s.Value.Duration }).ToList();

            //        row.DayRows.Add(channelDay);
            //    }
            //    AdBlockTable.Add(row);
            //}
        }

    }


}