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
    public class Clutter : ReportBase
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public List<ClutterRow> ChannelRows;
        public List<Model.DayPart> DayParts { get; set; }

        public class ClutterRow
        {
            public ClutterRow()
            {
                DayPartValues = new List<ClutterDayPartValue>();
            }

            public Guid ChanelId { get; set; }
            public string ChannelName { get; set; }
            public List<ClutterDayPartValue> DayPartValues { get; set; }
        }


        public class ClutterDayPartValue
        {
            public DayPart DayPart { get; set; }
            public double AverageBreak { get; set; }
            public double PercentageAboveAdBreak { get; set; }

        }


        public Clutter(string userId, Guid focusChannelId, IncludeSet include, DayPartType dayPartEnum, int adBreakDurationInSeconds, DateTime date) :
            base(userId, focusChannelId, include)
        {
            ChannelRows = new List<ClutterRow>();
            DayParts = new List<DayPart>();

            if (dayPartEnum == DayPartType.MyDayParts)
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
                        });
                    }
                }

                //Group matches in blocks
                var channelBlocks = ChannelAdBlockGenerator.Instance.GroupByChannelBlocks(dbRows);

                //ADD day average collumn
                var dayAverageDayPart = new DayPart { Name = "Day Average" };

                foreach (var cb in channelBlocks)
                {
                    var adBlocks = cb.Value.Where(c => c.EndDate.Date == date.Date);
                    var channelRow = new ClutterRow
                    {
                        ChanelId = cb.Key,
                        ChannelName = Channels.First(c => c.Id == cb.Key).Name,
                    };

                    double dayDurationSum = 0;
                    double dayAdBlocksCount = 0;
                    double dayAdBlocksAboveAdBreakCount = 0;

                    foreach (var dayPart in DayParts)
                    {
                        var dayPartAdBlocks = adBlocks.Where(c => dayPart.Hours.Any(h => h.Hour == c.EndDate.Hour));

                        double dayPartDurationSum = dayPartAdBlocks.Sum(x => (x.EndDate - x.StartDate).TotalSeconds);
                        double dayPartAdBlocksCount = dayPartAdBlocks.Count();
                        double dayPartAdBlocksAboveAdBreakCount = dayPartAdBlocks.Count(x => (x.EndDate - x.StartDate).TotalSeconds >= adBreakDurationInSeconds);

                        channelRow.DayPartValues.Add(
                            new ClutterDayPartValue
                            {
                                DayPart = dayPart,
                                AverageBreak = dayPartAdBlocksCount > 0 ? dayPartDurationSum / dayPartAdBlocksCount : 0,
                                PercentageAboveAdBreak = dayPartAdBlocksCount > 0 ? (dayPartAdBlocksAboveAdBreakCount / dayPartAdBlocksCount) * 100 : 0
                            }
                        );

                        dayDurationSum += dayPartDurationSum;
                        dayAdBlocksCount += dayPartAdBlocksCount;
                        dayAdBlocksAboveAdBreakCount += dayPartAdBlocksAboveAdBreakCount;

                    }

                    //Add day average for channel
                    channelRow.DayPartValues.Add(
                        new ClutterDayPartValue()
                        {
                            DayPart = dayAverageDayPart,
                            AverageBreak = dayAdBlocksCount > 0 ? dayDurationSum / dayAdBlocksCount : 0,
                            PercentageAboveAdBreak = dayAdBlocksCount > 0 ? (dayAdBlocksAboveAdBreakCount / dayAdBlocksCount) * 100 : 0
                        }
                    );

                    ChannelRows.Add(channelRow);
                }

                DayParts.Add(dayAverageDayPart);
            }

        }

    }
}