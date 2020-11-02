using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseCommon;

namespace AdK.Tagger.Model.Audit
{
    public class AuditDetails
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        public List<Group> Result;

        public AuditDetails(IEnumerable<Guid> channelIds, IEnumerable<Guid> songIds, DateTime dateFrom, DateTime dateTo)
        {
            Result = new List<Group>();
            if (channelIds.Any() && songIds.Any())
            {
                string select = String.Format(@"SELECT m.id, m.channel_id, s.title, s.duration, m.match_start, m.match_end, m.match_occurred, s.id, c.match_threshold
	                            FROM matches AS m
	                            JOIN songs AS s ON m.song_id = s.id
	                            JOIN accounts AS a ON a.user_id = s.user_id
	                            JOIN channels AS c ON m.channel_id = c.id
	                            WHERE
	                            s.duration > 0 AND
	                            s.deleted = 0 AND
	                            s.suppress_chart = 0 AND
	                            a.suppress_chart = 0 AND
	                            m.match_occurred >= @start AND m.match_occurred < @end AND
	                            m.song_id {0} AND m.channel_id {1}
	                            GROUP BY m.channel_id, m.song_id, m.match_occurred", Database.InClause(songIds), Database.InClause(channelIds));

                IEnumerable<Row> dbRows = Database.ListFetcher(select, dr => new Row
                {
                    ChannelId = dr.GetGuid(1),
                    Title = dr.GetStringOrDefault(2),
                    Duration = dr.GetDoubleOrDefault(3),
                    Start = dr.GetDoubleOrDefault(4),
                    End = dr.GetDoubleOrDefault(5),
                    PlayTime = dr.GetDateTime(6),
                    SongId = dr.GetGuid(7),
                    Limit = dr.GetDoubleOrDefault(8)
                },
                    "@start", dateFrom,
                    "@end", dateTo.AddDays(1)); //include this day in query

                foreach (var item in dbRows) {
                    var group = this.Result.FirstOrDefault(i => i.Date.Date == item.PlayTime.Date && i.ChannelId == item.ChannelId);
                    if (group == null)
                    {
                        group = new Group(item.ChannelId, item.PlayTime.Date);
                        this.Result.Add(group);
                    }
                    group.Rows.Add(item);
                }
            }
        }

        public class Group
        {
            public Group(Guid channelId, DateTime date) {
                this.Date = date;
                this.ChannelId = channelId;
                this.Rows = new List<Row>();
            }
            
            public Guid ChannelId;
            public DateTime Date;
            public List<Row> Rows;
        }

        public class Row
        {
            public Guid ChannelId;
            public string Title;
            public DateTime PlayTime;
            public double Duration;
            public double Start;
            public double End;
            public Guid SongId;
            public double Limit;
        }
    }
}