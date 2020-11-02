using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using System.Linq.Expressions;
using System.Linq;


namespace AdK.Tagger.Model
{
    public class Monitor
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        //#####################################################################
        public static DataTable GetDataTable(MySqlCommand command)
        {
            Log.Info(command.CommandText);
            DataTable res = new DataTable();
            using (var dr = command.ExecuteReader())
            {
                res.Load(dr);
            }
            return res;
        }

        //#####################################################################
        public static DataTable GetRecordingsSummary(int recordingType)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	count(c2.id) AS Total_Channels,
	count(CASE WHEN mrs < date_sub(now(), interval @intervalMin minute) THEN 1 ELSE null END) AS Dead,
    count(CASE WHEN mrs IS NULL THEN 1 ELSE null END) AS No_Recordings
FROM channels c2
LEFT JOIN (
	SELECT
		c.id,
		rci.machine_name, 
		max(rci.recording_start) AS mrs,
		round(rci.duration/1000) AS sec
	FROM channels c
	JOIN recordings_checked_in rci ON c.id=rci.channel_id AND rci.id>((SELECT max(id) FROM recordings_checked_in) - (SELECT count(id) FROM channels) * 12 * 24 * 7)
	WHERE rci.recording_type=@recordingType
	GROUP BY c.id
	) res
ON res.id=c2.id
WHERE station_name NOT LIKE '*%'
";
                int intervalMin = recordingType == 0 ? 20 : 15;
                command.Parameters.AddWithValue("@recordingType", recordingType);
                command.Parameters.AddWithValue("@intervalMin", intervalMin);
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static string GetRciLookupLimit()
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT coalesce(max(recording_start),'FOREVER')
FROM recordings_checked_in
WHERE id = ((SELECT max(id) FROM recordings_checked_in) - (SELECT count(id) FROM channels) *400*7)
";
                return (string)command.ExecuteScalar();
            }
        }
        //#####################################################################
        public static DataTable GetRecordingStatusAllChannels(int recordingType)
        {
            string clientNow = "";
            string filterChannels = "";
            switch (ConfigurationManager.AppSettings["Monitor.Client"])
            {
                case "PK":
                    clientNow = "now()";
                    filterChannels = "station_name NOT LIKE '*%' AND media_type='Radio'";
                    break;
                case "MIS":
                    clientNow = @"date_sub(now(), interval (
                                        SELECT CASE WHEN (domain = 'jm' OR domain = 'ky') THEN 7 ELSE 6 END
                                          FROM channels
                                          WHERE id = c2.id
			                            ) hour)";
                    filterChannels = "station_name NOT LIKE '*%' AND media_sub_type LIKE 'str%'";
                    break;
                case "CY":
                    clientNow = "date_add(now(),interval 1 hour)";
                    filterChannels = "station_name NOT LIKE '*%' AND media_type='Radio'";
                    break;
                case "ROI":
                    clientNow = "now()";
                    filterChannels = "station_name NOT LIKE '*%' AND media_type='Radio'";

                    break;
            }

            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	c2.domain AS Domain, 
	c2.station_name AS Station_Name, 
	c2.external_id AS External_ID, 
	res.machine_name AS Machine_Name, 
	timediff(now(), date_add(mrs, INTERVAL sec SECOND)) AS Waiting_For,
    CASE WHEN mrs IS NULL THEN 2 ELSE 
		CASE WHEN mrs < date_sub(" + clientNow + @", interval @intervalMin minute) THEN 1 ELSE 3 END
		END AS h_status,
    c2.id AS h_channel_id,
    mc.status AS h_hold_status,
    mc.hold_until AS h_hold_until
FROM channels c2
LEFT JOIN (
	SELECT
		c.id,
		rci.machine_name, 
		max(rci.recording_start) AS mrs,
		round(rci.duration/1000) AS sec
	FROM channels c
	JOIN recordings_checked_in rci ON c.id=rci.channel_id AND rci.id>((SELECT max(id) FROM recordings_checked_in) - (SELECT count(id) FROM channels) * 400 * 7)
	WHERE rci.recording_type=@recordingType
	GROUP BY c.id
	) res
ON res.id=c2.id
LEFT JOIN monitor_channels mc ON mc.channel_id=c2.id
WHERE " + filterChannels + @"
ORDER BY h_status, Waiting_For
";
                int intervalMin = recordingType == 0 ? 20 : 15;
                command.Parameters.AddWithValue("@intervalMin", intervalMin);
                command.Parameters.AddWithValue("@recordingType", recordingType);
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable GetChannelById(string channel)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = "SELECT station_name, domain FROM channels WHERE id=@channelId";
                command.Parameters.AddWithValue("@channelId", channel);
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable GetCaptureChannel(string channel)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @" 
SELECT
	Recording_Start,
    duration/1000 as Duration,
    date_add(time_landed, interval 1 hour) as File_Saved
FROM recordings_checked_in
WHERE id > ((SELECT max(id) FROM recordings_checked_in) - (SELECT count(id) FROM channels) * 400 * 7)
AND channel_id=@channelId
AND recording_type=4
ORDER BY id DESC
LIMIT 300
";
                command.Parameters.AddWithValue("@channelId", channel);
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable GetCaptureHolesChannel(string channel)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @" 
SELECT
	date_format(prevEnd, '%Y-%m-%d %H:%i:%s') AS Gap_Begins,
    date_format(curStart, '%Y-%m-%d %H:%i:%s') as Gap_Ends,
    time_format(timediff(curStart, prevEnd), '%H:%i:%s') AS Missing
FROM(
    SELECT
        rci.id,
        rci.channel_id,
        @start AS curStart,
        @dur AS curDuration,
        @start:= rci.recording_start AS prevStart,
        @dur:= rci.duration / 1000 AS prevDuration,
        date_add(rci.recording_start, interval(rci.duration / 1000) second) AS prevEnd
    FROM
        (SELECT @start:= null, @dur:= null) AS init,
        recordings_checked_in as rci
    WHERE rci.id > ((SELECT max(id) FROM recordings_checked_in) - (SELECT count(id) FROM channels) * 400 * 7)
    AND rci.channel_id = @channelId
    AND rci.recording_type = 4
    ORDER BY rci.id DESC
) AS rci
HAVING missing > '00:00:1'
";
                command.Parameters.AddWithValue("@channelId", channel);
                return GetDataTable(command);
            }
        }

        //#####################################################################
        public static DataTable GetDuplicateSummaryLate()
        {
            var duplicateStatusLate = new Dpoc.DuplicateStatusLate();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	COUNT(1) AS Running_Late, 
    coalesce(max(timediff(now(),export_date_time)),0) AS Now_Running
FROM (
	SELECT 
		export_date_time, 
		date_sub(date_matched, INTERVAL 5 HOUR) matched_date_time 
	FROM 
		(SELECT id, pksid, export_date_time
			FROM harvested_clips 
			ORDER BY id DESC 
			LIMIT 100
		) hc 
	LEFT JOIN match_sample_matched msm on hc.pksid = msm.pksid 
	ORDER BY hc.id DESC
	) stat
WHERE matched_date_time IS NULL
AND timediff(now(),export_date_time) > '00:05:00'
";
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable GetDuplicateSummaryFinished()
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
    count(CASE WHEN delay>'00:05:00' THEN 1 END) AS Finished_Late, 
    max(delay) AS Max_Delay
FROM (
    SELECT 
		timediff(date_sub(date_matched, INTERVAL 5 HOUR), export_date_time) AS delay 
	FROM 
		(SELECT id, pksid, export_date_time 
			FROM harvested_clips 
			ORDER BY id DESC 
			LIMIT 100
		) hc 
	LEFT JOIN match_sample_matched msm on hc.pksid = msm.pksid 
    ) stat
";
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable GetDuplicatesDetails()
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	hc.pksid AS PKSID, 
    tagger AS Harvester,
    export_date_time AS Export_Time,
    date_sub(date_matched, interval 5 hour) AS Matched_Time, 
    timediff(date_sub(date_matched, interval 5 hour), export_date_time) AS Delay
FROM 
	(SELECT * 
	    FROM harvested_clips 
        ORDER BY id DESC 
        LIMIT 100
    ) hc 
    LEFT JOIN match_sample_matched msm ON hc.pksid = msm.pksid 
    ORDER BY hc.id desc
";
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable GetHarvestingSummary(int limit)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	date_format(date(export_date_time),'%Y-%m-%d') AS Date,
	count(id) AS Clips,
    count(DISTINCT station_name) AS Channels,
    count(DISTINCT tagger) AS Harvesters,
	max(timediff(export_date_time,clip_date_time)) AS Max_Delay
FROM harvested_clips
GROUP BY Date
ORDER BY Date DESC
LIMIT @limit
";
                command.Parameters.AddWithValue("@limit", limit);
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable getDuplicateDetectionReport()
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	date_format(export_date, '%Y-%m-%d') AS Export_Date,
    delay15 * 15 AS Delay,
    clips AS Clips
FROM (
	SELECT
		date(hc.export_date_time) AS export_date,
		time_to_sec(timediff(date_sub(date_matched, interval 5 hour),export_date_time)) DIV 15 AS delay15,
		count(1) AS clips
	FROM harvested_clips hc
	JOIN match_sample_matched msm ON msm.pksid=hc.pksid
	WHERE date(hc.export_date_time) < date(now())
		AND date(hc.export_date_time) > date(date_sub(now(),INTERVAL 31 DAY))
	GROUP BY export_date, delay15
	HAVING delay15 < 40
		AND delay15 >= 0

	UNION

	SELECT
		date(hc.export_date_time) AS export_date,
		40 AS delay15,
		count(1) AS clips
	FROM harvested_clips hc
	JOIN match_sample_matched msm ON msm.pksid=hc.pksid
	WHERE date(hc.export_date_time) < date(now())
		AND date(hc.export_date_time) > date(date_sub(now(),INTERVAL 31 DAY))
		AND time_to_sec(timediff(date_sub(date_matched, interval 5 hour),export_date_time)) >= 600
	GROUP BY export_date
    
    UNION
    
	SELECT
		date(hc.export_date_time) AS export_date,
		-1 AS delay15,
        count(1) AS clips
    FROM harvested_clips hc
    JOIN match_sample_matched msm ON msm.pksid = hc.pksid
    WHERE date(hc.export_date_time) < date(now())
        AND date(hc.export_date_time) > date(date_sub(now(), INTERVAL 31 DAY))
        AND time_to_sec(timediff(date_sub(date_matched, interval 5 hour), export_date_time)) <= 0
    GROUP BY export_date
    
) stats
ORDER BY Export_Date DESC, Delay ASC
";


                DataTable table = GetDataTable(command);

                DataTable res = new DataTable();
                var rowColumn = table.Columns[0].ColumnName; //always 1st column
                var pivotColumn = table.Columns[1].ColumnName; //always 2nd column
                res.Columns.Add(new DataColumn(rowColumn));


                DataTable uniqueColumns = table.DefaultView.ToTable(true, pivotColumn);
                foreach(DataRow row in uniqueColumns.Rows)
                {
                    res.Columns.Add(new DataColumn(row[0].ToString()));
                }

                DataTable uniqueRows = table.DefaultView.ToTable(true, rowColumn);
                foreach (DataRow row in uniqueRows.Rows)
                {
                    DataRow dr = res.NewRow();
                    dr[0] = row[0].ToString();
                    res.Rows.Add(dr);
                }

                foreach (DataRow srcRow in table.Rows)
                {
                    foreach (DataRow targetRow in res.Rows)
                    {
                        if (targetRow[0].ToString() == srcRow[0].ToString())
                        {
                            targetRow[srcRow[pivotColumn].ToString()] = srcRow[2]; //always 3rd column
                        }
                    }
                }

                return res;
            }
        }
        //#####################################################################
        public class HarvestStatChannelDateHarvester
        {
            public string delay;
            public string harvester;
            public string clips;
        }
        public class HarvestStatChannelDate
        {
            public string date;
            public List<HarvestStatChannelDateHarvester> harvesters;
        }
        public class HarvestStatChannel
        {
            public string channel;
            public List<HarvestStatChannelDate> dateStats;

            public HarvestStatChannel(string c, List<string> dates)
            {
                channel = c;
                dateStats = new List<HarvestStatChannelDate>();
                foreach(string d in dates)
                {
                    dateStats.Add(new HarvestStatChannelDate {
                        date = d,
                        harvesters = new List<HarvestStatChannelDateHarvester>()
                    });
                }
            }

        }
        public class HarvestStatus
        {
            public List<string> dates;
            public List<HarvestStatChannel> channelStats;
            public HarvestStatChannelDate getChannelDateStats(string channel, string date)
            {
                foreach (HarvestStatChannel c in channelStats)
                {
                    if(c.channel == channel)
                    {
                        foreach (HarvestStatChannelDate d in c.dateStats)
                        {
                            if (d.date == date)
                            {
                                return d;
                            }
                        }
                    }
                }
                return null;
            }
        }
        public static string GetHarvestingStatus()
        {
            HarvestStatus stats = new HarvestStatus {
                dates = new List<string>(),
                channelStats = new List<HarvestStatChannel>()
            };
            using (var db = Database.Get())
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = @"
SELECT harvest_date
FROM (
	SELECT 
		DISTINCT harvest_date
	FROM user_events
	ORDER BY harvest_date DESC
	LIMIT 7
) tmp
ORDER BY harvest_date ASC
";
                DataTable res = GetDataTable(cmd);

                string dateFilter = "";
                foreach (DataRow row in res.Rows)
                {
                    string date = DateTime.Parse(row["harvest_date"].ToString()).ToString("yyyy-MM-dd");
                    dateFilter = dateFilter + "'" + date + "',";
                    stats.dates.Add(date);
                }
                dateFilter = dateFilter.Remove(dateFilter.Length - 1);

                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	ue.harvest_date AS airtime_date,
    c.station_name AS channel,
    c.domain AS domain,
	ue.user_name AS harvester,
	datediff(ue.event_time,ue.harvest_date) AS delay,
    count(hc.clip_date_time) AS clips
FROM user_events ue
JOIN channels c on c.id=ue.channel_id
LEFT JOIN harvested_clips hc
	ON date(ue.harvest_date)=date(hc.clip_date_time)
    AND c.id=hc.channel_id
    AND ue.user_name=hc.tagger
WHERE event_type=5
AND harvest_date in (" + dateFilter + @")
GROUP BY airtime_date, c.id, harvester, delay
";
                res = GetDataTable(command);

                List<string> channels = new List<string>();

                foreach (DataRow row in res.Rows)
                {
                    string channel = row["channel"].ToString() + " (" + row["domain"].ToString() + ")";
                    if (!channels.Contains(channel))
                    {
                        channels.Add(channel);
                        stats.channelStats.Add(new HarvestStatChannel(channel, stats.dates));
                    }
                    string airtime_date = DateTime.Parse(row["airtime_date"].ToString()).ToString("yyyy-MM-dd");
                    stats.getChannelDateStats(channel, airtime_date).harvesters.Add(new HarvestStatChannelDateHarvester {
                        harvester = row["harvester"].ToString(),
                        delay = row["delay"].ToString(),
                        clips = row["clips"].ToString()
                    });
                }

                System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                return serializer.Serialize(stats);
            }
        }
        //#####################################################################
        public static DataTable GetPromotionSummary()
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
    sum(Not_Promoted) AS Not_Promoted,
	count(Station_Name) As Channels,
    date_format(min(Oldest),'%Y-%m-%d') AS Oldest
FROM
	(SELECT 
		station_name AS Station_Name,
		count(*) AS Not_Promoted,
		min(export_date_time) AS Oldest
	FROM harvested_clips
	WHERE promotor IS NULL
	AND song_id IS NULL
	GROUP BY station_name
	) stats
";
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable GetPromotionDetails()
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	station_name AS Station_Name,
    count(*) AS Not_Promoted,
    date_format(min(export_date_time), '%Y-%m-%d') AS Oldest
FROM harvested_clips
WHERE promotor IS NULL
AND song_id IS NULL
GROUP BY station_name
ORDER BY Oldest ASC, Not_Promoted DESC
";
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable GetPromotionQueue()
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
    station_name AS Channel, 
    clip_date_time AS Time_Aired, 
    tagger AS Harvester, 
    export_date_time AS Time_Harvested
FROM harvested_clips
WHERE promotor IS NULL
AND song_id IS NULL
AND export_date_time > '2000-01-01'
ORDER BY export_date_time ASC
";
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static void UpdateRecordingStatus(string userId, string channelId, decimal status, decimal hold, string reason)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
INSERT INTO monitor_statuses 
(channel_id, user_id, timestamp, status, hold_until, hold_reason) 
VALUES 
(@channelId ,@userId,  now(), @status, date_add(now(), interval @holdUntil hour), @holdReason)";
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@channelId", channelId);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@holdUntil", hold);
                command.Parameters.AddWithValue("@holdReason", reason);
                command.ExecuteNonQuery();

                var cmd2 = db.CreateCommand();
                cmd2.CommandText = @"
INSERT INTO monitor_channels 
(channel_id, status, hold_until) 
VALUES 
(@channelId, @status, date_add(now(), interval @holdUntil hour))
ON DUPLICATE KEY UPDATE
    status = @status, 
    hold_until = date_add(now(), interval @holdUntil hour)
";
                cmd2.Parameters.AddWithValue("@channelId", channelId);
                cmd2.Parameters.AddWithValue("@status", status);
                cmd2.Parameters.AddWithValue("@holdUntil", hold);
                cmd2.ExecuteNonQuery();
            }
        }
        //#####################################################################
        public static DataTable GetChannelStatus(string channelId)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT status, hold_until
FROM monitor_channels
WHERE channel_id=@channelId
";
                command.Parameters.AddWithValue("@channelId", channelId);
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable GetCoverageForChannels(string channel_tag)
        {
            string tag_filter = channel_tag == "" ? "" : "AND ct.tag='" + channel_tag + "'";
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
    date_format(str_to_date(CONCAT(cvrg.week,' Monday'), '%X%V %W'), '%Y-%m-%d') AS Week,
	cvrg.Coverage,
    cvrg.Missing,
    nmch.numchannels AS Channels,
    cvrg.week AS h_week
FROM (
	SELECT 
		YEARWEEK(cover_date) AS week,
		format(sum(CASE WHEN total_duration<100 THEN total_duration ELSE 100 END) / count(1),2) AS Coverage,
		format(100 - sum(CASE WHEN total_duration<100 THEN total_duration ELSE 100 END) / count(1),2) AS Missing
	FROM channel_day_coverage cdc
	JOIN channels c ON c.id=cdc.channel_id
		AND c.station_name NOT LIKE '*%'
	LEFT JOIN channel_tags ct ON ct.channel_id=c.id
	JOIN (
		SELECT 
			channel_id, 
			min(cover_date) as live_from
		FROM channel_day_coverage
		GROUP BY channel_id
	) live ON live.channel_id=cdc.channel_id
	WHERE cdc.cover_date > live_from
	AND cdc.cover_date < curdate()"
    + tag_filter + @"
	GROUP BY week
) cvrg
JOIN (
	SELECT 
		week, 
        count(channel) AS numchannels
	FROM (
		SELECT 
			c.id AS channel,
			yearweek(cover_date) AS week
		FROM channel_day_coverage cdc
		JOIN channels c ON c.id=cdc.channel_id
			AND c.station_name NOT LIKE '*%'
		LEFT JOIN channel_tags ct ON ct.channel_id=c.id
		JOIN (
			SELECT 
				channel_id, 
				min(cover_date) as live_from
			FROM channel_day_coverage
			GROUP BY channel_id
		) live ON live.channel_id=cdc.channel_id
		WHERE cdc.cover_date > live_from
		AND cdc.cover_date < curdate()
	    " + tag_filter + @"
        GROUP BY c.id, week
	) res
	GROUP BY week
) nmch ON nmch.week=cvrg.week
ORDER BY Week DESC
LIMIT 12
";
                return GetDataTable(command);
            }
        }
        //#####################################################################
        public static DataTable GetCoverageByChannel(string week, string channel_tag)
        {
            string tag_filter = channel_tag == "" ? "" : "AND ct.tag='" + channel_tag + "'";
            Log.Info(channel_tag);
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	domain AS Domain,
    station_name AS Channel,
    format(coverage,2) AS Coverage,
    format(missing, 2) AS Missing,
    id AS h_channel_id
FROM (
	SELECT 
        c.id,
		c.domain,
		c.station_name,
		sum(CASE WHEN total_duration<100 THEN total_duration ELSE 100 END) / count(1) AS coverage,
		100 - sum(CASE WHEN total_duration<100 THEN total_duration ELSE 100 END) / count(1) AS missing
	FROM channel_day_coverage cdc
	JOIN channels c ON c.id=cdc.channel_id
		AND c.station_name NOT LIKE '*%'
	LEFT JOIN channel_tags ct ON ct.channel_id=c.id
	JOIN (
		SELECT 
			channel_id, 
			min(cover_date) as live_from
		FROM channel_day_coverage
		GROUP BY channel_id
	) live ON live.channel_id=cdc.channel_id
	WHERE cdc.cover_date > live_from
	AND cdc.cover_date < curdate()
	AND yearweek(cover_date)=@week
	" + tag_filter + @"
    GROUP BY c.id
	ORDER BY Missing DESC
) cvrg
";
                command.Parameters.AddWithValue("@week", week);
                return GetDataTable(command);
            }
        }

        //#####################################################################
        public static string WhatClient()
        {
            return ConfigurationManager.AppSettings["Monitor.Client"];
        }
        //#####################################################################
        public static string GetMondayOfWeek(string week)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT date_format(str_to_date(CONCAT(@week,' Monday'), '%X%V %W'), '%Y-%m-%d')";
                command.Parameters.AddWithValue("@week", week);
                return (string)command.ExecuteScalar();
            }
        }


    } // THE END
}