using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseCommon;

namespace AdK.Tagger.Model.Audit
{
	public class AuditLog
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		public object Rows;

		public AuditLog( IEnumerable<Guid> channelIds, IEnumerable<Guid> songIds, DateTime dateFrom, DateTime dateTo, int auditId = 0 )
		{
			Log.Info( String.Format( "Run audit for date from {0} to date to {1}", dateFrom, dateTo ) );

			Rows = new object();
			if ( channelIds.Any() && songIds.Any() ) {

				var channelThresholds = new List<AuditChannelThreshold>();
				if ( auditId != 0 ) {
					channelThresholds = AuditChannelThreshold.GetForAudit( auditId );
				}

				string select = String.Format( @"SELECT m.id, m.channel_id, m.song_id,
						DATE(m.match_occurred) AS play_date, 
						HOUR(m.match_occurred) AS play_hour,
                        c.match_threshold, s.duration as songDuration, (m.match_end - m.match_start) as matchDuration
						FROM matches AS m
						JOIN songs AS s ON m.song_id = s.id
						JOIN accounts a ON a.user_id = s.user_id
						JOIN channels c ON m.channel_id = c.id
						WHERE
						s.duration > 0 AND
						s.deleted = 0 AND
						s.suppress_chart = 0 AND
						a.suppress_chart = 0 AND
						m.match_occurred >= @start AND m.match_occurred < @end AND
						m.song_id {0} AND m.channel_id {1}", Database.InClause( songIds ),
					 Database.InClause( channelIds ) );

				var dbRows = Database.ListFetcher( select, dr => new AuditLogRow {
					ChannelId = dr.GetGuid( 1 ),
					SongId = dr.GetGuid( 2 ),
					PlayDate = dr.GetDateTime( 3 ),
					PlayHour = dr.GetInt32( 4 ),
					MatchThreshold = dr.GetDecimalOrDefault( 5 ),
					SongDuration = dr.GetDecimalOrDefault( 6 ),
					MatchDuration = dr.GetDecimalOrDefault( 7 )
				}, "@start", dateFrom,
				"@end", dateTo.AddDays( 1 ) ); //include this day in query (m.match_end - m.match_start) >= (s.duration * 0.5)

				var channelThresholdsDic = new Dictionary<Guid, decimal>();
				foreach ( var row in dbRows ) {
					var auditThreshold = channelThresholds.FirstOrDefault( ct => ct.ChannelId == row.ChannelId );
					if ( !channelThresholdsDic.ContainsKey( row.ChannelId ) ) {
						channelThresholdsDic.Add( row.ChannelId, auditThreshold != null ? auditThreshold.Threshold : row.MatchThreshold );
					}
				}

				Rows = dbRows.GroupBy( row => row.ChannelId ).Select( gChannel => new {
					ChannelId = gChannel.Key,
					Dates = gChannel.GroupBy( row => row.PlayDate ).Select( gDate => new {
						PlayDate = gDate.Key,
						Songs = gDate.GroupBy( row => row.SongId ).Select( gSong => new {
							SongId = gSong.Key,
							CountByHour = Enumerable.Range( 0, 24 )
							 .Select( hour => gSong.Where( row => row.PlayHour == hour ) )
							 .Select( row => new HourCount {
								 NormalMatches = row.Count( r => r.MatchDuration >= r.SongDuration * channelThresholdsDic[r.ChannelId] ),
								 TotalMatches = row.Count()
							 } )
						} )
					} ).OrderBy( d => d.PlayDate )
				} );


			}
		}

		public class AuditLogRow
		{
			public Guid ChannelId;
			public Guid SongId;
			public DateTime PlayDate;
			public int PlayHour;
			public decimal MatchThreshold;
			public decimal SongDuration;
			public decimal MatchDuration;
		}

		public class HourCount
		{
			public int NormalMatches { get; set; }
			public int TotalMatches { get; set; }
			public int PartialMatches { get { return TotalMatches - NormalMatches; } }
		}
	}
}