using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace DatabaseCommon
{
	public static class DatabaseConstants
	{
		public static string MatchesTableRequiredJoins
		{
			get
			{
				return @"FROM matches
						LEFT JOIN songs ON matches.song_id = songs.id
						LEFT JOIN channels ON matches.channel_id = channels.id
						LEFT JOIN accounts ON accounts.user_id = songs.user_id";
			}
		}

		public static string MatchesTableRequiredJoinsWithEarns
		{
			get
			{
				return String.Format( @"{0} LEFT JOIN pricedefs 
						ON pricedefs.product_id = songs.product_id 
						AND channels.id = pricedefs.channel_id
						AND pricedefs.hour = hour(matches.match_occurred) 
						AND pricedefs.dow = weekday(matches.match_occurred)",
						MatchesTableRequiredJoins );
			}
		}

		public static string MatchesTableRequiredWherePart
		{
			get
			{
				return @"songs.duration > 0 AND
						(matches.match_end - matches.match_start) >= (songs.duration * channels.match_threshold) AND
						songs.deleted = 0 AND
						songs.suppress_chart = 0 AND
						accounts.suppress_chart = 0";
			}
		}

		public static string MatchTableEarnsSelect
		{
			get { return "cast((songs.duration) * pricedefs.pps as decimal(19,4))"; }
		}

		public static string MatchTableEarnsSelectAsEarns
		{
			get { return String.Format( "{0} as earns", MatchTableEarnsSelect ); }
		}

	}
}
