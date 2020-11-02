using DatabaseCommon;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class Playout
	{
		public Guid SongId;
		public Guid ChannelId;
		public string Title;
		public string Performer;

		public static List<Playout> GetRecent(DateTime start, DateTime end)
		{
			return Database.ListFetcher(@"
SELECT m.song_id, m.channel_id, s.title, s.performer #, m.match_occurred
FROM matches m
LEFT JOIN songs s ON s.id = m.song_id
WHERE
	m.match_occurred BETWEEN @start AND @end AND
	(m.match_end - m.match_start) >= s.duration * 0.7;",
				dr => new Playout
				{
					SongId = dr.GetGuid(0),
					ChannelId = dr.GetGuid(1),
					Title = dr.GetString(2),
					Performer = dr.GetString(3)
				},
				"@start", start,
				"@end", end);
		}
	}
}