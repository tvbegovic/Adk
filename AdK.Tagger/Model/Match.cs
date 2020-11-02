using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Model
{
	public class Match
	{
		public string id { get; set; }
		public string song_id { get; set; }
		public string channel_id { get; set; }
		public DateTime? match_occurred { get; set; }
		public double? match_start { get; set; }
		public double? match_end { get; set; }
		public double? min_ber { get; set; }
		public DateTime? date_scanned { get; set; }
		public double? earns { get; set; }
		public string earns_problem { get; set; }
		public DateTime? create_stamp { get; set; }

		public double? duration
		{
			get
			{
				return match_end - match_start;
			}
		}

		public Song Song { get; set; }

		public static List<Match> GetByCriteria(string channelId, DateTime from, DateTime to)
		{
			return Database.ListFetcher<Match>(
				@"SELECT matches.*, songs.title, songs.created, songs.duration
				  FROM matches INNER JOIN songs ON matches.song_id = songs.id INNER JOIN channels ON matches.channel_id = channels.id
				  WHERE matches.channel_id = @channelId
				  AND (matches.match_end - matches.match_start) > songs.duration * channels.match_threshold
				  AND (matches.match_occurred BETWEEN @from AND @to OR DATE_ADD(matches.match_occurred, INTERVAL (matches.match_end - matches.match_start) SECOND) BETWEEN @from AND @to)
				  ",
				(dr) => new Match
				{
					id = dr.GetString(0),
					song_id = dr.GetStringOrDefault(1),
					channel_id = dr.GetStringOrDefault(2),
					match_occurred = dr.GetDateOrNull(3),
					match_start = dr.GetDoubleOrNull(4),
					match_end = dr.GetDoubleOrNull(5),
					min_ber = dr.GetDoubleOrNull(6),
					date_scanned = dr.GetDateOrNull(7),
					earns = dr.GetDoubleOrNull(8),
					earns_problem = dr.GetStringOrDefault(9),
					create_stamp = dr.GetDateOrNull(10),
					Song = new Song
					{
						Title = dr.GetStringOrDefault(11),
						Created = dr.GetDateOrNull(12),
						Duration = dr.GetDecimal(13)
					}
				}, "@channelId", channelId, "@from", from, "@to", to);
		}
	}
}
