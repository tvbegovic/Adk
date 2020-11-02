using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace AdK.Tagger.Model
{
    public class Dpoc
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        //exclude for HR: Radio S (3360cd56-ef6e-4ce1-a9c4-7efbe7387c6a, 018a0499-5ca9-4fc0-b4eb-f4828862449c)
        public static string IPSOS = "'48c0eece-3595-4e0f-928e-3d0d88a61f46','400b03d9-1677-44cf-ad16-7041ea5f381b','6afcab28-59f9-4ad0-85a5-e64f88cb273c','13b3a21f-b7d0-4192-9876-41d595a91316','9ff8f0be-78cb-4403-a117-2df6278a8156','4ada1c35-44a0-4900-8feb-5cef0cef7a08','5b3e8834-b5c4-4e78-bb62-b1839bcf9a36','4b8b8c56-9922-4b34-9fcf-c74e896ab404','b43e67c7-384f-479d-962c-5b9a48409a62','eac57397-8d6d-4d3f-9fe5-6c9b7ae18368','ef072e2e-396c-4e45-ba36-7a5966da7458','0710559c-b8e3-4196-b92d-68cf4cc50dd3','e3c06d89-f503-449b-82b0-db492f49dcbe','3e1e6659-4031-4536-908f-764bbf6a2a37','470a87b3-e01c-4348-9258-a1f983f45fef','7d580467-1abc-47ff-b902-f269b2791f03','e8ac9901-4e0d-4177-ab60-ea15adb69b98','cdfb3401-3a2d-4483-a497-5e16d06dcba2','79a01175-9f8e-4274-ad0f-405cba382fcb','bc5ad2c1-9d45-4d10-99be-78fee5b381d3','fba6cf32-e2c5-4abb-a823-a3c525ad0464','429b2c47-e72a-44df-8564-d0fdba9b96c7','f678c931-19a1-4251-ac18-a5f5c5d177ac','d23e734d-1f09-4094-bcc2-4af4edff738c','a7e7a0b0-bac7-4d71-91c4-5e30ee8eea3c','45877b1c-1835-4c31-81cc-fbfbd8f5a129','68910662-146d-4344-a924-e97222244d95','f3f443f2-b7e9-4d60-a425-13e3312f68fe','c8e6936f-c2cc-4084-a923-074b3fecc613','26d646b3-41a9-492a-872d-5e76e5885355','bd9233c2-9a73-491a-a270-b659869c6d0b','aed07e26-f9d2-447c-847f-adf656e2850f','322779c6-cbf3-4537-a741-4cd353805b0d','e6998f2b-7a9e-4b7a-b888-bd79bee68751','f297613b-1e7b-425d-9126-6dbf91b21d99','5b66aaf2-ebed-4821-892f-c4732a8457e6','2c8fde5d-3542-459a-8772-bfe3ad0069ae','1669ed72-fcb9-405f-887a-cd945de641d3','514e16b4-9ac1-4fdb-8573-abf6f89c391a','f50af634-e38a-40dc-b849-3926b8ba32f3','a59b5eb6-8346-4ffb-9cb3-f0d673b42724','70f499b4-1e8e-4693-84ca-ab979513fbc7'";
        public static string hrBaza = "adkontrol_hrvatska";
        public static string hrRecordingsLimit = "40000";
        public static string hrLocalTime = "now()";
        public static string hrChannelFilter = Dpoc.IPSOS;
        public static string pkChannelFilter = "SELECT id FROM adkontrol_hrvatska.channels WHERE station_name NOT LIKE '*%'";

        public static string misBaza = "adkontrol_carribean";
        public static string misRecordingsLimit = "10000";
        public static string misLocalTime = "date_sub(now(), INTERVAL (CASE WHEN c.domain='jm' THEN 7 ELSE 6 END) HOUR)";
        public static string misChannelFilter = "SELECT id FROM adkontrol_carribean.channels WHERE id NOT IN ('d39707a5-3af8-4601-b087-002387a7bbd8','b19a61da-35bf-47d2-8a84-5d90e4e71add','1b8c1d7b-5906-4f07-a82a-79c88731f6c3','40163175-e90d-47f2-82fc-a118ef7edae3','23d9ea06-5df3-4b57-a06c-1a8c9d466577','3494597b-36fd-48c3-8470-6cbaea5f0b24','91006491-de41-4400-bfb5-6951a79ab73b') AND station_name NOT LIKE '*%'";
        //exclude for MIS: 'd39707a5-3af8-4601-b087-002387a7bbd8','b19a61da-35bf-47d2-8a84-5d90e4e71add','1b8c1d7b-5906-4f07-a82a-79c88731f6c3','40163175-e90d-47f2-82fc-a118ef7edae3','23d9ea06-5df3-4b57-a06c-1a8c9d466577','3494597b-36fd-48c3-8470-6cbaea5f0b24','91006491-de41-4400-bfb5-6951a79ab73b'

        public static string cyBaza = "adkontrol_cyprus";
        public static string cyRecordingsLimit = "10000";
        public static string cyLocalTime = "date_add(now(), INTERVAL 1 HOUR)";
        public static string cyChannelFilter = "SELECT id FROM adkontrol_cyprus.channels WHERE station_name NOT LIKE '*%'";

        public static string roiBaza = "adkontrol_roiafrica";
        public static string roiRecordingsLimit = "10000";
        public static string roiLocalTime = "now()";
        public static string roiChannelFilter = "SELECT id FROM adkontrol_roiafrica.channels WHERE station_name NOT LIKE '*%'";

        #region Harvesting
        public class HarvestingDateStats
        {
            public string harvestingDate;
            public decimal clips;
            public decimal taggers;
            public decimal channels;
            public decimal maxDelay;
        }
        public class HarvestingStat
        {
            public string datum;
            public string tagger;
            public string channel;
            public decimal spotsCount;
        }

        //#####################################################################
        public static List<Dpoc.HarvestingStat> GetHarvestingStats()
        {
            var harvestingStats = new List<Dpoc.HarvestingStat>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT DATE(export_date_time) AS datum, tagger, station_name AS channel, count(id) AS spotsCount
FROM adkontrol_hrvatska.harvested_clips
GROUP BY datum, tagger, channel
ORDER BY datum DESC
LIMIT 100;";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        harvestingStats.Add(new HarvestingStat
                        {
                            datum = dr.GetString(0).Split(' ')[0],
                            tagger = dr.GetString(1),
                            channel = dr.GetString(2),
                            spotsCount = dr.GetDecimal(3)
                        });
                }
                return harvestingStats;
            }
        }
        //#####################################################################
        public static List<Dpoc.HarvestingStat> GetHarvestingStatsMis()
        {
            var harvestingStats = new List<Dpoc.HarvestingStat>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT DATE(export_date_time) AS datum, tagger, station_name AS channel, count(id) AS spotsCount
FROM adkontrol_carribean.harvested_clips
GROUP BY datum, tagger, channel
ORDER BY datum DESC
LIMIT 100;";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        harvestingStats.Add(new HarvestingStat
                        {
                            datum = dr.GetString(0).Split(' ')[0],
                            tagger = dr.GetString(1),
                            channel = dr.GetString(2),
                            spotsCount = dr.GetDecimal(3)
                        });
                }
                return harvestingStats;
            }
        }
        //#####################################################################
        public static List<Dpoc.HarvestingDateStats> GetHarvestingStatus(string whichWeb)
        {
            string baza = "";
            switch (whichWeb)
            {
                case "hr":
                    baza = "adkontrol_hrvatska";
                    break;
                case "mis":
                    baza = Dpoc.misBaza;
                    break;
            }

            var harvestingStatus = new List<Dpoc.HarvestingDateStats>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	date(export_date_time) AS harvestDate,
	count(id) AS clips,
    count(DISTINCT tagger) AS taggers,
    count(DISTINCT station_name) AS channels,
	max(timestampdiff(HOUR,clip_date_time,export_date_time)) AS maxDelay
FROM " + baza + @".harvested_clips
GROUP BY harvestDate
ORDER BY harvestDate DESC
LIMIT 2
";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        harvestingStatus.Add(new Dpoc.HarvestingDateStats
                        {
                            harvestingDate = dr.GetString(0).Split(' ')[0],
                            clips = dr.GetDecimal(1),
                            taggers = dr.GetDecimal(2),
                            channels = dr.GetDecimal(3),
                            maxDelay = dr.GetDecimal(4)
                        });
                }
                return harvestingStatus;
            }
        }
        #endregion
        #region Promotion
        public class PromotionDate
        {
            public string datum;
            public decimal spotsCount;
        }

        public class NotPromotedClip
        {
            public string channel;
            public string timeAired;
            public string harvester;
            public string timeHarvested;
        }
        //#####################################################################
        public static List<Dpoc.PromotionDate> GetPromotionQueue()
        {
            var promotionQueue = new List<Dpoc.PromotionDate>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT DATE(export_date_time) AS datum, count(id) as nisu_promovirani
FROM harvested_clips
WHERE id > 16288
AND promotor IS NULL
AND export_date_time > '2000-01-01'
GROUP BY datum
ORDER BY datum DESC;";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        promotionQueue.Add(new Dpoc.PromotionDate
                        {
                            datum = dr.GetString(0).Split(' ')[0],
                            spotsCount = dr.GetDecimal(1)
                        });
                }
                return promotionQueue;
            }
        }
        //#####################################################################
        public static List<Dpoc.NotPromotedClip> GetNotPromotedClips()
        {
            var promotionQueue = new List<Dpoc.NotPromotedClip>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT station_name, clip_date_time, tagger, export_date_time
FROM harvested_clips
WHERE promotor IS NULL
AND song_id IS NULL
AND export_date_time > '2000-01-01'
ORDER BY export_date_time DESC;";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        promotionQueue.Add(new Dpoc.NotPromotedClip
                        {
                            channel = dr.GetString(0),
                            timeAired = dr.GetDateTime(1).ToString(),
                            harvester = dr.GetString(2),
                            timeHarvested = dr.GetDateTime(3).ToString()
                        });
                }
                return promotionQueue;
            }
        }
        #endregion
        #region CONTROL
        public class ChannelCoverage
        {
            public string channel;
            public string country;
            public decimal coverage;
            public decimal missing;
        }

        //#####################################################################
        public static List<Dpoc.ChannelCoverage> GetCoverageWorst(decimal numdays)
        {
            var coverageWorst = new List<Dpoc.ChannelCoverage>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	c.station_name, 
    d.domain_name, 
    sum(case when total_duration > 100 then 100 else total_duration end)/@numdays AS coverage,
    (100 - sum(case when total_duration > 100 then 100 else total_duration end)/@numdays)/100 * 86400*@numdays AS missing
FROM channel_day_coverage AS cdc
JOIN channels AS c ON c.id=cdc.channel_id
JOIN domains AS d ON c.domain=d.domain
WHERE cdc.cover_date >= date_sub(current_date, interval @numdays day)
AND cdc.cover_date < current_date
AND c.id IN (" + Dpoc.IPSOS + @")
GROUP BY c.station_name
ORDER BY coverage ASC
LIMIT 10
";
                command.Parameters.AddWithValue("@numdays", numdays);
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        coverageWorst.Add(new Dpoc.ChannelCoverage
                        {
                            channel = dr.GetString(0),
                            country = dr.GetString(1),
                            coverage = dr.GetDecimal(2),
                            missing = dr.GetDecimal(3)
                        });
                }
                return coverageWorst;
            }
        }
        //#####################################################################
        public static decimal GetMissingTotal(decimal numdays)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 100 - sum(case when total_duration > 100 then 100 else total_duration end)/count(1) AS missing
FROM channel_day_coverage AS cdc
JOIN channels AS c ON c.id=cdc.channel_id
WHERE cdc.cover_date >= date_sub(current_date, interval @numdays day)
AND cdc.cover_date < current_date
AND c.id IN (" + Dpoc.IPSOS + @")
";
                command.Parameters.AddWithValue("@numdays", numdays);
                return (decimal)command.ExecuteScalar();
            }
        }
        //#####################################################################
        public static string GetCountBadChannels(decimal numdays)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT count(1) 
FROM (
    SELECT 
        sum(case when total_duration > 100 then 100 else total_duration end)/count(1) AS coverage
    FROM channel_day_coverage
    WHERE cover_date >= date_sub(current_date, interval @numdays day)
    AND cover_date < current_date
    AND channel_id IN (" + Dpoc.IPSOS + @")
    GROUP BY channel_id
    HAVING coverage < 99
    ) TMP_TBL
";
                command.Parameters.AddWithValue("@numdays", numdays);
                return command.ExecuteScalar().ToString();
            }
        }
        #endregion
        #region Hashing
        public class HashingStatus
        {
            public decimal total;
            public decimal dead;
            public decimal worst;
            public decimal bad;
            public decimal oscilating;
        }
        public class ChannelStatus
        {
            public string channel;
            public string domain;
            public decimal recordings;
            public decimal duration;
            public decimal missing;
            public string lastRecording;
            public string externalId;
            public string mediaSubType;
            public string id;
            public decimal unhealthy;
        }
        public class ChannelDetails
        {
            public string start;
            public decimal duration;
            public string fileSaved;
            public string delay;
        }
        public class ChannelData
        {
            public string name;
            public string domain;
        }

        //#####################################################################
        public static List<Dpoc.ChannelStatus> GetHashingDetails(string whichWeb)
        {
            string baza = "", recordingsLimit = "", localTime = "", channelFilter = "";
            switch (whichWeb)
            {
                case "hr":
                    baza = Dpoc.hrBaza;
                    recordingsLimit = Dpoc.hrRecordingsLimit;
                    localTime = Dpoc.hrLocalTime;
                    channelFilter = Dpoc.hrChannelFilter;
                    break;
                case "mis":
                    baza = Dpoc.misBaza;
                    recordingsLimit = Dpoc.misRecordingsLimit;
                    localTime = Dpoc.misLocalTime;
                    channelFilter = Dpoc.misChannelFilter;
                    break;
            }
            var recordingStatus = new List<Dpoc.ChannelStatus>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	stat.channel,
    stat.domain,
    stat.recordings,
    stat.duration,
    stat.missing,    
    max(coalesce(rci2.recording_start,0)) as last_recording,
    stat.external_id,
    stat.media_sub_type
FROM
	(SELECT 
		c.id as cid,
		c.station_name AS channel, 
		c.domain AS domain,
		count(rci.duration) AS recordings,
		round(coalesce(sum(rci.duration),0)/1000) AS duration,
		3600-round(coalesce(sum(rci.duration),0)/1000) AS missing,
        c.external_id as external_id,
        c.media_sub_type as media_sub_type
	FROM " + baza + @".channels AS c
	LEFT JOIN " + baza + @".recordings_checked_in AS rci
		ON rci.id > (SELECT max(id) FROM " + baza + @".recordings_checked_in) - " + recordingsLimit + @"
		AND c.id = rci.channel_id
		AND rci.recording_type = 0
		AND rci.recording_start > date_sub(date_sub(" + localTime + @", INTERVAL 20 minute), INTERVAL 1 HOUR)
		AND rci.recording_start < date_sub(" + localTime + @", INTERVAL 20 minute)
	WHERE c.id IN (" + channelFilter + @")
	GROUP BY c.station_name) AS stat
LEFT JOIN 
	(SELECT id,channel_id,recording_start 
		FROM " + baza + @".recordings_checked_in
        WHERE id > (SELECT max(id) FROM " + baza + @".recordings_checked_in) - (" + recordingsLimit + @"*2*24)
        AND recording_type = 0
		AND channel_id IN (" + channelFilter + @")
		ORDER BY id DESC
	) AS rci2
	ON rci2.channel_id = stat.cid
GROUP BY stat.cid
ORDER BY duration ASC
";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        recordingStatus.Add(new Dpoc.ChannelStatus
                        {
                            channel = dr.GetString(0),
                            domain = dr.GetString(1),
                            recordings = dr.GetDecimal(2),
                            duration = dr.GetDecimal(3),
                            missing = dr.GetDecimal(4),
                            lastRecording = dr.GetString(5),
                            externalId = dr.IsDBNull(6) ? "" : dr.GetString(6),
                            mediaSubType = dr.IsDBNull(7) ? "" : dr.GetString(7)
                        });
                }
                return recordingStatus;
            }
        }
        //#####################################################################
        // TODO: channels sleeping over night: 69e914bb-3d5d-451b-9341-3408df6a5451, aebad7d8-debc-4840-a0be-eecf4c609479, bd55b27c-a02e-48fc-ba0d-153524397f7b, 7679f8fa-17b3-4bef-9ff0-c966b2dc1cf6,0fec4180-0963-4f8f-b101-b0c5f579009e, a01f842f-e9ed-4622-a861-6035bd1601fc,9185ce1a-3a2c-4b74-8928-51b8de3240c4,fa330edd-29da-4887-ae98-69e39b29af7c,09e44526-1578-4dba-bc3e-4cb529a3b156,9cea41f3-29ce-416c-9316-705e0391b60e,b44b098d-3365-44d1-a260-ec90299f2886,ac8cde23-7354-44c2-a276-10a4d6ce003b,e9e76e9c-b159-4f37-bf12-35aab001838a,fac94401-d569-486e-af05-9e235ecf0d24,0029188c-d4cc-4853-a998-b0bd3b8b6581,c52bf120-e674-4c25-af52-027d6c207d94,68209147-de6d-40d0-a4e8-e528b4b53b4f,a2554d2a-bb94-4169-81d1-2b82aace8a45,1091e810-0352-4092-9294-df7be2868553,959416c7-b2a3-414d-ba32-af320e79384b,a4ea3f46-44da-4846-9490-696d5ee6add7,616628aa-32a9-4499-a778-890c29070b66,4c323f2a-92e6-48b9-8fc1-f625576f9196
        public static Dpoc.HashingStatus GetHashingStatus(string whichWeb)
        {
            string baza="", recordingsLimit="", localTime="", channelFilter="";
            switch (whichWeb)
            {
                case "hr":
                    baza = Dpoc.hrBaza;
                    recordingsLimit = Dpoc.hrRecordingsLimit;
                    localTime = Dpoc.hrLocalTime;
                    channelFilter = Dpoc.hrChannelFilter;
                    break;
                case "mis":
                    baza = Dpoc.misBaza;
                    recordingsLimit = Dpoc.misRecordingsLimit;
                    localTime = Dpoc.misLocalTime;
                    channelFilter = Dpoc.misChannelFilter;
                    break;
            }

            var hashingStatus = new HashingStatus();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	max(CASE WHEN missing<1 THEN missing END) worst,
    count(CASE WHEN missing>0.005 AND missing<1 THEN 1 END) bad,
    count(CASE WHEN recordings<>12 AND recordings<>0 THEN 1 END) oscilating,
	count(CASE WHEN missing=1 THEN 1 END) dead,
    count(channel) total
FROM (
SELECT 
	c.station_name AS channel, 
    c.domain AS domain,
    count(rci.duration) AS recordings,
    (3600-round(coalesce(sum(rci.duration),0)/1000))/3600 AS missing
FROM " + baza + @".channels AS c
LEFT JOIN " + baza + @".recordings_checked_in AS rci
    ON rci.id > (SELECT max(id) FROM " + baza + @".recordings_checked_in)-" + recordingsLimit + @"
    AND c.id = rci.channel_id
    AND rci.recording_start > date_sub(date_sub(" + localTime + @", INTERVAL 20 minute), INTERVAL 1 HOUR)
    AND rci.recording_start < date_sub(" + localTime + @", INTERVAL 20 minute)
    AND rci.recording_type = 0
WHERE c.id IN (" + channelFilter + @")
GROUP BY c.station_name) stat
";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        hashingStatus.worst = dr.GetDecimal(0) * 100;
                        hashingStatus.bad = dr.GetDecimal(1);
                        hashingStatus.oscilating = dr.GetDecimal(2);
                        hashingStatus.dead = dr.GetDecimal(3);
                        hashingStatus.total = dr.GetDecimal(4);
                    };
                }
                return hashingStatus;
            }
        }
        #endregion
        #region Capture
        public class HoleDetails
        {
            public string starts;
            public string ends;
            public string missing;
        }
        public class DeadChannel
        {
            public string domain;
            public string channel;
            public string externalId;
            public string server;
            public string silence;
            public string id;
            public decimal action;
            public string actionTimestamp;
        }

        //#####################################################################
        public static Dpoc.HashingStatus GetCaptureStatus(string whichWeb, string delayMin)
        {
            string baza = "", recordingsLimit = "", localTime = "", channelFilter = "";
            switch (whichWeb)
            {
                case "hr":
                    baza = Dpoc.hrBaza;
                    recordingsLimit = Dpoc.hrRecordingsLimit;
                    localTime = Dpoc.hrLocalTime;
                    channelFilter = Dpoc.hrChannelFilter;
                    break;
                case "mis":
                    baza = Dpoc.misBaza;
                    recordingsLimit = Dpoc.misRecordingsLimit;
                    localTime = Dpoc.misLocalTime;
                    channelFilter = Dpoc.misChannelFilter;
                    break;
            }

            var hashingStatus = new HashingStatus();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	max(CASE WHEN missing<1 THEN missing END) worst,
    count(CASE WHEN missing>0.005 AND missing<1 THEN 1 END) bad,
    count(CASE WHEN recordings>12 THEN 1 END) oscilating,
	count(CASE WHEN missing=1 THEN 1 END) dead,
    count(channel) total
FROM (
SELECT 
	c.station_name AS channel, 
    c.domain AS domain,
    count(rci.duration) AS recordings,
    (3600-round(coalesce(sum(rci.duration),0)/1000))/3600 AS missing
FROM " + baza + @".channels AS c
LEFT JOIN " + baza + @".recordings_checked_in AS rci
    ON rci.id > (SELECT max(id) FROM " + baza + @".recordings_checked_in)-" + recordingsLimit + @"
    AND c.id = rci.channel_id
    AND rci.recording_start > date_sub(date_sub(" + localTime + @", INTERVAL " + delayMin + @" minute), INTERVAL 1 HOUR)
    AND rci.recording_start < date_sub(" + localTime + @", INTERVAL " + delayMin + @" minute)
    AND rci.recording_type = 4
WHERE c.id IN (" + channelFilter + @")
GROUP BY c.station_name) stat
";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        hashingStatus.worst = dr.GetDecimal(0) * 100;
                        hashingStatus.bad = dr.GetDecimal(1);
                        hashingStatus.oscilating = dr.GetDecimal(2);
                        hashingStatus.dead = dr.GetDecimal(3);
                        hashingStatus.total = dr.GetDecimal(4);
                    };
                }
                return hashingStatus;
            }
        }
        //#####################################################################
        public static decimal unhealthy(decimal recordings, decimal duration)
        {
            decimal recMissing = 12 - recordings;
            decimal minMissing = (3600 - duration) / 60;
            decimal res = 0;
            res += (recMissing == 12) ? 10000 : 0;
            res += recMissing > 0 ? recMissing * 10 : Math.Abs(recMissing) * 50;
            res += minMissing > 0 ? minMissing * 5 : Math.Abs(minMissing);
            return res;
        }
        //#####################################################################
        public static List<Dpoc.ChannelStatus> GetCaptureDetails(string whichWeb)
        {
            string baza = "", recordingsLimit = "", localTime = "", channelFilter = "";
            switch (whichWeb)
            {
                case "hr":
                    baza = Dpoc.hrBaza;
                    recordingsLimit = Dpoc.hrRecordingsLimit;
                    localTime = Dpoc.hrLocalTime;
                    channelFilter = Dpoc.pkChannelFilter;
                    break;
                case "mis":
                    baza = Dpoc.misBaza;
                    recordingsLimit = Dpoc.misRecordingsLimit;
                    localTime = Dpoc.misLocalTime;
                    channelFilter = "SELECT id FROM adkontrol_carribean.channels WHERE station_name NOT LIKE '*%'";
                    break;
                case "cy":
                    baza = Dpoc.cyBaza;
                    recordingsLimit = Dpoc.cyRecordingsLimit;
                    localTime = Dpoc.cyLocalTime;
                    channelFilter = Dpoc.cyChannelFilter;
                    break;
                case "roi":
                    baza = Dpoc.roiBaza;
                    recordingsLimit = Dpoc.roiRecordingsLimit;
                    localTime = Dpoc.roiLocalTime;
                    channelFilter = Dpoc.roiChannelFilter;
                    break;
            }
            var recordingStatus = new List<Dpoc.ChannelStatus>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	stat.channel,
    stat.domain,
    stat.recordings,
    stat.duration,
    stat.missing,    
    max(coalesce(rci2.recording_start,0)) as last_recording,
    stat.external_id,
    stat.media_sub_type,
    stat.cid
FROM
	(SELECT 
		c.id as cid,
		c.station_name AS channel, 
		c.domain AS domain,
		count(rci.duration) AS recordings,
		round(coalesce(sum(rci.duration),0)/1000) AS duration,
		3600-round(coalesce(sum(rci.duration),0)/1000) AS missing,
        c.external_id as external_id,
        c.media_sub_type as media_sub_type
	FROM " + baza + @".channels AS c
	JOIN " + baza + @".recordings_checked_in AS rci
		ON rci.id > (SELECT max(id) FROM " + baza + @".recordings_checked_in) - " + recordingsLimit + @"
		AND c.id = rci.channel_id
		AND rci.recording_type = 4
		AND rci.recording_start > date_sub(date_sub(" + localTime + @", INTERVAL 5 minute), INTERVAL 1 HOUR)
		AND rci.recording_start < date_sub(" + localTime + @", INTERVAL 5 minute)
	WHERE c.id IN (" + channelFilter + @")
	GROUP BY c.station_name) AS stat
LEFT JOIN 
	(SELECT id,channel_id,recording_start 
		FROM " + baza + @".recordings_checked_in
        WHERE id > (SELECT max(id) FROM " + baza + @".recordings_checked_in) - (" + recordingsLimit + @"*2*24)
        AND recording_type = 4
		ORDER BY id DESC
	) AS rci2
	ON rci2.channel_id = stat.cid
GROUP BY stat.cid
";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        recordingStatus.Add(new Dpoc.ChannelStatus
                        {
                            channel = dr.GetString(0),
                            domain = dr.GetString(1),
                            recordings = dr.GetDecimal(2),
                            duration = dr.GetDecimal(3),
                            missing = dr.GetDecimal(4),
                            lastRecording = dr.GetString(5),
                            externalId = dr.IsDBNull(6) ? "" : dr.GetString(6),
                            mediaSubType = dr.IsDBNull(7) ? "" : dr.GetString(7),
                            id = dr.GetString(8),
                            unhealthy = Dpoc.unhealthy(dr.GetDecimal(2), dr.GetDecimal(3))
                        });
                }
                return recordingStatus;
            }
        }
        //#####################################################################
        public static List<Dpoc.ChannelDetails> GetCaptureChannel(string channel, string whichWeb)
        {
            string dbName = "", idLimit = "";
            switch (whichWeb)
            {
                case "pk":
                    dbName = Dpoc.hrBaza;
                    idLimit = Dpoc.hrRecordingsLimit;
                    break;
                case "mis":
                    dbName = Dpoc.misBaza;
                    idLimit = Dpoc.misRecordingsLimit;
                    break;
                case "cy":
                    dbName = Dpoc.cyBaza;
                    idLimit = Dpoc.cyRecordingsLimit;
                    break;
                case "roi":
                    dbName = Dpoc.roiBaza;
                    idLimit = Dpoc.roiRecordingsLimit;
                    break;
            }
            idLimit = (decimal.Parse(idLimit) * 5).ToString();
            var channelDetails = new List<Dpoc.ChannelDetails>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @" 
SELECT
	recording_start,
    duration/1000 as duration_sec,
    date_add(time_landed, interval 1 hour) as file_done
FROM " + dbName + @".recordings_checked_in
WHERE id > (SELECT max(id) FROM " + dbName + @".recordings_checked_in) - " + idLimit + @"
AND channel_id=@channelId
AND recording_type=4
ORDER BY id DESC
";
                command.Parameters.AddWithValue("@channelId", channel);
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        channelDetails.Add(new Dpoc.ChannelDetails
                        {
                            start = dr.GetString(0),
                            duration = dr.GetDecimal(1),
                            fileSaved = dr.GetString(2)
                        });
                }
                return channelDetails;
            }
        }
        //#####################################################################
        public static List<Dpoc.HoleDetails> GetCaptureHolesChannel(string channel, string whichWeb)
        {
            string dbName = "", idLimit = "";
            switch (whichWeb)
            {
                case "pk":
                    dbName = Dpoc.hrBaza;
                    idLimit = "40000000";
                    break;
                case "mis":
                    dbName = Dpoc.misBaza;
                    idLimit = "20000000";
                    break;
                case "cy":
                    dbName = Dpoc.cyBaza;
                    idLimit = "0";
                    break;
                case "roi":
                    dbName = Dpoc.roiBaza;
                    idLimit = "0";
                    break;
            }
            var channelHoles = new List<Dpoc.HoleDetails>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @" 
SELECT
	date_format(prevEnd, '%Y-%m-%d %H:%i:%s') AS starts,
    curStart as ends,
    timediff(curStart, prevEnd) AS missing
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
        " + dbName + @".recordings_checked_in as rci
    WHERE rci.id > " + idLimit + @"
    AND rci.channel_id = @channelId
    AND rci.recording_type = 4
    ORDER BY rci.id DESC
) AS rci
HAVING missing > '00:00:1'
";
                command.Parameters.AddWithValue("@channelId", channel);
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        channelHoles.Add(new Dpoc.HoleDetails
                        {
                            starts = dr.GetString(0),
                            ends = dr.GetString(1),
                            missing = dr.GetString(2).Split('.')[0]
                        });
                }
                return channelHoles;
            }
        }
        //#####################################################################
        public static List<Dpoc.DeadChannel> GetDeadChannels(string whichWeb)
        {
            string clientLimit = "";
            string pkLimit = "40000000";
            string misLimit = "20000000";
            string cyLimit = "0";
            string roiLimit = "0";

            string clientNow = "";
            string pkNow = "now()";
            string misNow = @"date_sub(now(), interval (
                SELECT case when domain = 'jm' then 7 else 6 end
                  FROM adkontrol_carribean.channels
                  WHERE id = channel_id
			    ) hour)";
            string cyNow = "date_add(now(),interval 1 hour)";
            string roiNow = "now()";

            var deadChannels = new List<DeadChannel>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                string dbName = "";
                switch (whichWeb)
                {
                    case "hr":
                        clientLimit = pkLimit;
                        clientNow = pkNow;
                        dbName = "adkontrol_hrvatska";
                        break;
                    case "mis":
                        clientLimit = misLimit;
                        clientNow = misNow;
                        dbName = "adkontrol_carribean";
                        break;
                    case "cy":
                        clientLimit = cyLimit;
                        clientNow = cyNow;
                        dbName = "adkontrol_cyprus";
                        break;
                    case "roi":
                        clientLimit = roiLimit;
                        clientNow = roiNow;
                        dbName = "adkontrol_roiafrica";
                        break;
                }
                command.CommandText = @"
SELECT 
    tmp.*, 
    CASE WHEN mrs < action_timestamp THEN action_type ELSE null END,
    CASE WHEN mrs < action_timestamp THEN action_timestamp ELSE null END
FROM (
    SELECT 
        domain, 
        station_name, 
        external_id, 
        rci3.machine_name, 
        timediff(" + clientNow + @", 
        date_add(rci3.mrs, INTERVAL rci3.sec SECOND)) AS silence,
        id,
        rci3.mrs
    FROM " + dbName + @".channels c
    JOIN (
	    SELECT rci2.channel_id, rci2.machine_name, rci.mrs, round(rci2.duration/1000) AS sec
	    FROM " + dbName + @".recordings_checked_in rci2
	    JOIN
	    (
		    SELECT *, max(recording_start) AS mrs
		    FROM " + dbName + @".recordings_checked_in
		    WHERE id > " + clientLimit + @"
		    AND recording_type = 4
		    GROUP BY channel_id
		    HAVING mrs < date_sub(" + clientNow + @", interval 15 minute)
	    ) rci ON rci2.id > " + clientLimit + @" AND rci.id = rci2.id
    ) rci3 ON rci3.channel_id=c.id
    ORDER BY silence ASC
) tmp
LEFT JOIN monitor_actions ma ON ma.channel_id = tmp.id
ORDER BY silence ASC";
                Log.Info("Dead channels for " + whichWeb + ": " + command.CommandText);
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        deadChannels.Add(new DeadChannel
                        {
                            domain = dr.GetString(0),
                            channel = dr.GetString(1),
                            externalId = dr.GetString(2),
                            server = dr.GetString(3),
                            silence = dr.GetString(4),
                            id = dr.GetString(5),
                            action = dr.IsDBNull(7) ? -1 : dr.GetDecimal(7),
                            actionTimestamp = dr.IsDBNull(8) ? "-" : dr.GetString(8)
                        });
                }
                return deadChannels;
            }
        }
        //#####################################################################
        public static string GetCaptureActionTimestamp(string channelId)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT max(action_timestamp)
FROM monitor_actions
WHERE channel_id=@channelId
";
                command.Parameters.AddWithValue("@channelId", channelId);
                return (string)command.ExecuteScalar();
            }
        }
        //#####################################################################
        public static Dpoc.ChannelData GetChannelById(string channel, string whichWeb)
        {
            string dbName = "";
            switch (whichWeb)
            {
                case "pk":
                    dbName = Dpoc.hrBaza;
                    break;
                case "mis":
                    dbName = Dpoc.misBaza;
                    break;
                case "cy":
                    dbName = Dpoc.cyBaza;
                    break;
                case "roi":
                    dbName = Dpoc.roiBaza;
                    break;
            }
            var channelData = new ChannelData();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = "SELECT station_name, domain FROM " + dbName + ".channels WHERE id=@channelId";
                command.Parameters.AddWithValue("@channelId", channel);
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        channelData.name = dr.GetString(0);
                        channelData.domain = dr.GetString(1);
                    }
                }
                return channelData;
            }
        }
        //#####################################################################
        public static void UpdateChannelAction(string userId, string channelId, decimal action)
        {
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
INSERT INTO monitor_actions 
(user_id, channel_id, action_type, action_timestamp) 
VALUES 
(@user_id, @channel_id, @action_type, now())"; 
                command.Parameters.AddWithValue("@user_id", userId);
                command.Parameters.AddWithValue("@channel_id", channelId);
                command.Parameters.AddWithValue("@action_type", action);
                command.ExecuteNonQuery();
            }
        }
        #endregion
        #region Duplicates
        public class DuplicateStatusLate
        {
            public decimal runningLate;
            public string nowRunning;
        }
        public class DuplicateStatusFinished
        {
            public decimal finishedLate;
            public string maxDelay;
        }
        public class DuplicateDelay
        {
            public string pksid;
            public string exportTime;
            public string matchedTime;
            public string delay;
            public string tagger;
        }

        //#####################################################################
        public static List<Dpoc.DuplicateDelay> GetDuplicateDelays()
        {
            var duplicateDelays = new List<Dpoc.DuplicateDelay>();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
select 
	hc.pksid, 
    export_date_time, 
    date_sub(date_matched, interval 5 hour) matched_date_time, 
    timediff(date_sub(date_matched, interval 5 hour),
    export_date_time) as delay,
    tagger
from 
	(select * 
	from adkontrol_carribean.harvested_clips 
    order by id desc 
    limit 100) hc 
    left join adkontrol_carribean.match_sample_matched msm on hc.pksid = msm.pksid 
    order by hc.id desc
";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        duplicateDelays.Add(new Dpoc.DuplicateDelay
                        {
                            pksid = dr.GetString(0),
                            exportTime = dr.GetDateTime(1).ToString(),
                            matchedTime = dr.IsDBNull(2) ? "" : dr.GetDateTime(2).ToString(),
                            delay = dr.IsDBNull(3) ? "" : dr.GetString(3),
                            tagger = dr.IsDBNull(4) ? "" : dr.GetString(4)
                        });
                }
                return duplicateDelays;
            }
        }
        //#####################################################################
        public static Dpoc.DuplicateStatusLate GetDuplicateStatusLate()
        {
            var duplicateStatusLate = new Dpoc.DuplicateStatusLate();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
	COUNT(1), 
    coalesce(max(timediff(date_sub(now(), INTERVAL 6 HOUR),export_date_time)),0)
FROM (
	SELECT 
		export_date_time, 
		date_sub(date_matched, INTERVAL 5 HOUR) matched_date_time 
	FROM 
		(SELECT id, pksid, export_date_time
			FROM adkontrol_carribean.harvested_clips 
			ORDER BY id DESC 
			LIMIT 100
		) hc 
	LEFT JOIN adkontrol_carribean.match_sample_matched msm on hc.pksid = msm.pksid 
	ORDER BY hc.id DESC
	) stat
WHERE matched_date_time IS NULL
AND timediff(date_sub(now(), INTERVAL 6 HOUR),export_date_time) > '00:05:00'
";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        duplicateStatusLate.runningLate = dr.GetDecimal(0);
                        duplicateStatusLate.nowRunning = dr.GetString(1);
                    }
                }
                return duplicateStatusLate;
            }
        }
        //#####################################################################
        public static Dpoc.DuplicateStatusFinished GetDuplicateStatusFinished()
        {
            var duplicateStatusFinished = new Dpoc.DuplicateStatusFinished();
            using (var db = Database.Get())
            {
                var command = db.CreateCommand();
                command.CommandText = @"
SELECT 
    count(CASE WHEN delay>'00:05:00' THEN 1 END), 
    max(delay) 
FROM (
    SELECT 
		timediff(date_sub(date_matched, INTERVAL 5 HOUR), export_date_time) AS delay 
	FROM 
		(SELECT id, pksid, export_date_time 
			FROM adkontrol_carribean.harvested_clips 
			ORDER BY id DESC 
			LIMIT 100
		) hc 
	LEFT JOIN adkontrol_carribean.match_sample_matched msm on hc.pksid = msm.pksid 
    ) stat
";
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        duplicateStatusFinished.finishedLate = dr.GetDecimal(0);
                        duplicateStatusFinished.maxDelay = dr.GetString(1);
                    }
                }
                return duplicateStatusFinished;
            }
        }
        #endregion

    }
}


