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
    public class DayPartActivityByDaypart
    {
        public DayPartActivityByDaypart(int daypartId, string daypartName)
        {
            DaypartId = daypartId;
            key = daypartName;
            values = new List<ChannelValue>();
        }

        public int DaypartId { get; set; }

        /// <summary>
        /// key property is needed by nvD3 library to draw chart (*reason for bad naming convention)
        /// </summary>
        public string key { get; set; }

        /// <summary>
        /// values property is needed by nvD3 library to draw chart (*reason for bad naming convention)
        /// </summary>
        public List<ChannelValue> values { get; set; }


    }

    public class ChannelValue
    {
        public ChannelValue(Guid channelId, string channelName, decimal value)
        {
            ChannelId = channelId;
            ChannelName = channelName;
            Value = value;
            Count = 0;
            Duration = 0;
            Spend = 0;
        }
        public ChannelValue(Guid channelId, string channelName, decimal count, decimal duration, decimal spend)
        {
            ChannelId = channelId;
            ChannelName = channelName;
            Value = 0;
            Count = count;
            Duration = duration;
            Spend = spend;
        }

        public Guid ChannelId { get; set; }
        public string ChannelName { get; set; }
        public decimal Value { get; set; }
        public decimal Count { get; set; }
        public decimal Duration { get; set; }
        public decimal Spend { get; set; }
    }

    public class ActivityByDaypart : ReportBase
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public List<DayPartActivityByDaypart> ChartData { get; set; }
        public List<DayPartActivityByDaypart> PercentageChartData { get; set; }

        public decimal MaxTotalValue { get; set; }
        public decimal MaxPercentageValue { get; set; }
        public decimal TotalChartValue { get; set; }

        public ActivityByDaypart(string userId, Guid focusChannelId, IncludeSet include, GroupingValue value, PeriodInfo period, DayOfWeekRange dayOfWeekRange, DayPartType dayPart, bool viewData)
            : base(userId, focusChannelId, include, period, value)
        {
            ChartData = new List<DayPartActivityByDaypart>();
            PercentageChartData = new List<DayPartActivityByDaypart>();

            MaxPercentageValue = 0;

            _loadData(userId, dayOfWeekRange, dayPart, viewData);
        }

        private void _loadData(string userId, DayOfWeekRange dayOfWeekRange, DayPartType dayPartSet, bool viewData)
        {
            using (var conn = Database.Get())
            {
                var cmd = conn.CreateCommand();

                var value = viewData ? "SUM(IFNULL(play_count, 0)) AS totalCount, SUM(IFNULL(duration, 0)) AS totalDuration, SUM(IFNULL(earns, 0)) AS totalSpend, play_hour" : "SUM(IFNULL(" + _valueColumn() + ", 0)) AS total";
                var orderBy = viewData ? "SUM(play_count), SUM(duration), SUM(earns)" : "SUM(" + _valueColumn() + ")";
                cmd.CommandText = string.Format(@"
									SELECT C.channel_id, CH.external_id, {0}
											-- , DP.daypart_id, DP.daypart_name 
												
									FROM report_base_cache C
									LEFT JOIN channels CH ON C.channel_id = CH.id
									-- INNER JOIN(SELECT
									--               dp.`id` as daypart_id, dp.`name` as daypart_name, dph.`day`, dph.`hour`
									--               FROM day_part dp
									--               JOIN day_part_set dps ON dp.day_part_set_id = dps.id
									--                  JOIN day_part_hour dph ON dp.id = dph.day_part_id
									--           WHERE user_id = @user_id
									-- ) AS DP ON(DAYOFWEEK(C.play_date) - 1, C.play_hour) = (DP.`day`, DP.`hour`)
									WHERE
										channel_id {1}
										AND WEEKDAY(C.play_date) IN ({2})
										AND C.play_date >= @start AND C.play_date < @end
									GROUP BY  {4} C.channel_id, CH.external_id
												-- , DP.daypart_id, DP.daypart_name
									ORDER BY {4} C.channel_id, 
												-- DP.daypart_id,
												{3} DESC

									"
                                        , value
                                        , Database.InClause(_ChannelIds)
                                        , DayOfWeekRangeToMySqlRange(dayOfWeekRange)
                                        , orderBy
                                        , viewData && dayPartSet == DayPartType.AllDay ? "play_hour," : string.Empty
                                    );

                cmd.Parameters.AddWithValue("@start", _Period.CurrentStart.Date);
                cmd.Parameters.AddWithValue("@end", _Period.CurrentEnd.Date);

                if (dayPartSet == DayPartType.MyDayParts)
                {
                    cmd.CommandText = cmd.CommandText.Replace("--", "");
                    cmd.Parameters.AddWithValue("@user_id", userId);
                }

                Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    SET @user_id:='{2}'; 
                    {3}
                ", _Period.CurrentStart.Date, _Period.CurrentEnd.Date, userId, cmd.CommandText));

                var allChannelsTotalValues = new Dictionary<Guid, ChannelValue>();
                var daypartChannelValues = new Dictionary<int, List<ChannelValue>>();

                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        if (!viewData)
                        {
                            Guid channelId = dr.GetGuid(0);
                            var channelName = dr.GetString(1);
                            var total = dr.GetDecimal(2);
                            int daypartId = -1;
                            string daypartName = "All day";
                            if (dayPartSet == DayPartType.MyDayParts)
                            {
                                daypartId = dr.GetInt32(3);
                                daypartName = dr.GetString(4);
                            }

                            if (allChannelsTotalValues.ContainsKey(channelId))
                            {
                                allChannelsTotalValues[channelId].Value += total;
                            }
                            else
                            {
                                allChannelsTotalValues[channelId] = new ChannelValue(channelId, channelName, total);
                            }

                            var daypartActivity = ChartData.FirstOrDefault(c => c.DaypartId == daypartId);
                            if (daypartActivity == null)
                            {
                                daypartActivity = new DayPartActivityByDaypart(daypartId, daypartName);
                                ChartData.Add(daypartActivity);
                                daypartChannelValues.Add(daypartId, new List<ChannelValue>());
                            }

                            daypartChannelValues[daypartId].Add(new ChannelValue(channelId, channelName, total));


                            TotalChartValue += total;
                        }
                        else
                        {
                            Guid channelId = dr.GetGuid(0);
                            var channelName = dr.GetString(1);
                            var count = dr.GetDecimal(2);
                            var duration = dr.GetDecimal(3);
                            var spend = dr.GetDecimal(4);
                            var playHour = dr.GetInt32(5);
                            int daypartId = -1;
                            string daypartName = "All day";
                            if (dayPartSet == DayPartType.MyDayParts)
                            {
                                daypartId = dr.GetInt32(6);
                                daypartName = dr.GetString(7);
                            }
                            else
                            {
                                daypartId = dr.GetInt32(5);
                                daypartName = daypartId.ToString();
                            }
                            if (allChannelsTotalValues.ContainsKey(channelId))
                            {
                                allChannelsTotalValues[channelId].Count += count;
                                allChannelsTotalValues[channelId].Duration += duration;
                                allChannelsTotalValues[channelId].Spend += spend;
                            }
                            else
                            {
                                allChannelsTotalValues[channelId] = new ChannelValue(channelId, channelName, count, duration, spend);
                            }
                            var daypartActivity = ChartData.FirstOrDefault(c => c.DaypartId == daypartId);
                            if (daypartActivity == null)
                            {
                                daypartActivity = new DayPartActivityByDaypart(daypartId, daypartName);
                                ChartData.Add(daypartActivity);
                                daypartChannelValues.Add(daypartId, new List<ChannelValue>());
                            }
                            daypartChannelValues[daypartId].Add(new ChannelValue(channelId, channelName, count, duration, spend));
                        }

                    }
                }



                TotalChartValue = TotalChartValue == 0 ? 1 : TotalChartValue;

                MaxTotalValue = allChannelsTotalValues.Any() ? allChannelsTotalValues.Max(b => b.Value.Value) : 0;


                var orderedAllChannelsTotalValues = allChannelsTotalValues.Where(c => c.Value.Value > 0 || c.Value.Count > 0 || c.Value.Duration > 0 || c.Value.Spend > 0).ToList().OrderByDescending(b => b.Value.Value).ThenByDescending(b => b.Value.Count).ThenByDescending(b => b.Value.Duration).ThenByDescending(b => b.Value.Spend);


                foreach (var daypartActivity in ChartData)
                {
                    var percentageChannelData = new DayPartActivityByDaypart(daypartActivity.DaypartId, daypartActivity.key);

                    foreach (var channel in orderedAllChannelsTotalValues)
                    {
                        var channelValue = daypartChannelValues[daypartActivity.DaypartId].FirstOrDefault(b => b.ChannelId == channel.Key);
                        if (channelValue == null)
                        {
                            channelValue = new ChannelValue(channel.Key, channel.Value.ChannelName, 0);
                        }

                        daypartActivity.values.Add(channelValue);

                        decimal percentage = (channelValue.Value / TotalChartValue) * 100;

                        MaxPercentageValue = percentage > MaxPercentageValue ? percentage : MaxPercentageValue;

                        percentageChannelData.values.Add(new ChannelValue(channelValue.ChannelId, channelValue.ChannelName, percentage));
                    }

                    PercentageChartData.Add(percentageChannelData);

                }



            }
        }

        private string DayOfWeekRangeToMySqlRange(DayOfWeekRange dayOfWeekRange)
        {
            switch (dayOfWeekRange)
            {
                case DayOfWeekRange.All:
                    return "0,1,2,3,4,5,6";
                case DayOfWeekRange.M_F:
                    return "0,1,2,3,4";
                case DayOfWeekRange.Weekends:
                    return "5,6";
                case DayOfWeekRange.Monday:
                    return "0";
                case DayOfWeekRange.Tuesday:
                    return "1";
                case DayOfWeekRange.Wednesday:
                    return "2";
                case DayOfWeekRange.Thursday:
                    return "3";
                case DayOfWeekRange.Friday:
                    return "4";
                case DayOfWeekRange.Saturday:
                    return "5";
                case DayOfWeekRange.Sunday:
                    return "6";
                default:
                    return "0,1,2,3,4,5,6";
            }
        }

    }
}