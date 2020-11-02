using DatabaseCommon;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
    public class ChannelsWithCoverage
    {
        public Guid Id { get; set; }
        public string StationName { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public decimal Days90 { get; set; }
        public decimal Days90Duration { get; set; }
        public string Days90Color { get; set; }
        public decimal Days30 { get; set; }
        public decimal Days30Duration { get; set; }
        public string Days30Color { get; set; }
        public decimal Days7 { get; set; }
        public decimal Days7Duration { get; set; }
        public string Days7Color { get; set; }
        public decimal Yesterday { get; set; }
        public decimal YesterdayDuration { get; set; }
        public string YesterdayColor { get; set; }

        public ChannelsWithCoverage()
        {

        }

        public static List<ChannelsWithCoverage> Get()
        {
            using (var conn = Database.Get())
            {
                var cmd = conn.CreateCommand();

                cmd.CommandText = string.Format(@"
select 
	c.id, c.station_name, c.country, c.city, 
    case when c90.coverage > 100 then 100 else c90.coverage end as days_90,
    case when c90.coverage > 100 then 0 else c90.coverage_duration end as days_90_duration,
    case when c90.coverage <= (select value from settings where module = 'Channels' and `key` = 'MinUpperLimit') then 'red'
		 when c90.coverage > (select value from settings where module = 'Channels' and `key` = 'MinUpperLimit') and c90.coverage < (select value from settings where module = 'Channels' and `key` = 'MaxLowerLimit') then 'yellow' 
         when c90.coverage >= (select value from settings where module = 'Channels' and `key` = 'MaxLowerLimit') then 'green' 
	 end as days_90_color,
	case when c30.coverage > 100 then 100 else c30.coverage end as days_30,
    case when c30.coverage > 100 then 0 else c30.coverage_duration end as days_30_duration,
    case when c30.coverage <= (select value from settings where module = 'Channels' and `key` = 'MinUpperLimit') then 'red'
		 when c30.coverage > (select value from settings where module = 'Channels' and `key` = 'MinUpperLimit') and c30.coverage < (select value from settings where module = 'Channels' and `key` = 'MaxLowerLimit') then 'yellow' 
         when c30.coverage >= (select value from settings where module = 'Channels' and `key` = 'MaxLowerLimit') then 'green' 
	 end as days_30_color,
	case when c7.coverage > 100 then 100 else c7.coverage end as days_7,
	case when c7.coverage > 100 then 0 else c7.coverage_duration end as days_7_duration,
    case when c7.coverage <= (select value from settings where module = 'Channels' and `key` = 'MinUpperLimit') then 'red'
		 when c7.coverage > (select value from settings where module = 'Channels' and `key` = 'MinUpperLimit') and c7.coverage < (select value from settings where module = 'Channels' and `key` = 'MaxLowerLimit') then 'yellow' 
         when c7.coverage >= (select value from settings where module = 'Channels' and `key` = 'MaxLowerLimit') then 'green' 
	 end as days_7_color,
	case when c1.coverage > 100 then 100 else c1.coverage end as yesteday,
    case when c1.coverage > 100 then 0 else c1.coverage_duration end as yesteday_duration,
    case when c1.coverage <= (select value from settings where module = 'Channels' and `key` = 'MinUpperLimit') then 'red'
		 when c1.coverage > (select value from settings where module = 'Channels' and `key` = 'MinUpperLimit') and c1.coverage < (select value from settings where module = 'Channels' and `key` = 'MaxLowerLimit') then 'yellow' 
         when c1.coverage >= (select value from settings where module = 'Channels' and `key` = 'MaxLowerLimit') then 'green' 
	 end as yesterday_color
from channels c 
	join
		(select channel_id, round(sum(total_duration), 2) as coverage, (100 - round(sum(total_duration), 2)) * 86400 /100 coverage_duration from channel_day_coverage 
		where channel_id is not null and cover_date = date_sub(current_date, interval 1 day)
		group by channel_id) c1
	on c.id = c1.channel_id
	join
		(select channel_id, round(sum(total_duration)/7, 2) as coverage, (100 - round(sum(total_duration)/7, 2)) * 86400*7 /100 coverage_duration from channel_day_coverage 
		where channel_id is not null and cover_date >= date_sub(current_date, interval 7 day) and cover_date < current_date
		group by channel_id) c7
	on c.id = c7.channel_id
	join
		(select channel_id, round(sum(total_duration)/30, 2) as coverage, (100 - round(sum(total_duration)/30, 2)) * 86400*30 /100 coverage_duration from channel_day_coverage 
		where channel_id is not null and cover_date >= date_sub(current_date, interval 30 day) and cover_date < current_date
		group by channel_id) c30
	on c.id = c30.channel_id
	join
		(select channel_id, round(sum(total_duration)/90, 2) as coverage, (100 - round(sum(total_duration)/90, 2)) * 86400*90 /100 coverage_duration from channel_day_coverage 
		where channel_id is not null and cover_date >= date_sub(current_date, interval 90 day) and cover_date < current_date
		group by channel_id) c90
	on c.id = c90.channel_id
	where station_name not like '* %'
    order by station_name
                                             ");

                var dbRows = new List<ChannelsWithCoverage>();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        dbRows.Add(new ChannelsWithCoverage
                        {
                            Id = dr.GetGuid(0),
                            StationName = dr.GetString(1),
                            Country = dr.GetStringOrDefault(2),
                            City = dr.GetStringOrDefault(3),
                            Days90 = dr.GetDecimal(4),
                            Days90Duration = dr.GetDecimal(5),
                            Days90Color = dr.GetString(6),
                            Days30 = dr.GetDecimal(7),
                            Days30Duration = dr.GetDecimal(8),
                            Days30Color = dr.GetString(9),
                            Days7 = dr.GetDecimal(10),
                            Days7Duration = dr.GetDecimal(11),
                            Days7Color = dr.GetString(12),
                            Yesterday = dr.GetDecimal(13),
                            YesterdayDuration = dr.GetDecimal(14),
                            YesterdayColor = dr.GetString(15),
                        });
                    }
                }
                return dbRows;
            }
        }
    }
}