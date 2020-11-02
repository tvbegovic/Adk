using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace AdK.Tagger.Model
{
	public class SampleMatch
	{
		public string PksId;
		public string CustomerName;
		public string FirstWords;
		public decimal Duration;
		public DateTime? CreateDate;
		public List<Match> Matches;

		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		public SampleMatch(string pksid, string customerName, string firstWords, decimal duration, DateTime? createDate)
		{
			PksId = pksid;
			CustomerName = customerName;
			FirstWords = firstWords;
			Duration = duration;
			CreateDate = createDate;
		}

		public static int ImportNewMatches()
		{
			int importedCount = 0;
			Log.Info( String.Format( "Import new matches for base url: {0} ", ConfigurationManager.AppSettings["SampleMatch.CustomersRootUrl"] ) );
			var unimportedSamples = GetUnimportedSamples();
			foreach (var sample in unimportedSamples)
			{
				if (sample.HasBeenMatched())
				{
					if (sample.ImportMatches())
						importedCount++;
				}
			}
			return importedCount;
		}

		private static IEnumerable<SampleMatch> GetUnimportedSamples()
		{
			var unmatched = new List<SampleMatch>();

			using (var con = Database.Get())
			{
				var cmd = con.CreateCommand();
				cmd.CommandText = @"SELECT s.pksid, s.user_id, s.title, s.duration, s.created FROM songs s WHERE s.pksid NOT IN (SELECT pksid FROM match_imported)";
				using (var dr = cmd.ExecuteReader())
				{
					while (dr.Read())
						unmatched.Add(new SampleMatch(dr.GetString(0), dr.GetString(1).Replace("-", ""), dr.GetNullableString(2), dr.GetDecimal(3), dr.GetNullableDate(4)));
				}
			}

			return unmatched;
		}

		/// <summary>
		/// Pick max most recent matches
		/// </summary>
		/// <param name="max"></param>
		/// <returns></returns>
		public static IEnumerable<string> GetRecentMatches(int max)
		{
			string select = @"SELECT source_pksid FROM match_sample ms
LEFT JOIN match_imported mi ON ms.source_pksid = mi.pksid
WHERE
	mi.comment IS NULL AND
	id = (SELECT id FROM match_sample WHERE source_pksid = ms.source_pksid LIMIT 1) # for a given pksID, take any match
ORDER BY match_date DESC
LIMIT @max";
			return Database.ListFetcher(select, dr => dr.GetString(0), "@max", max);
		}
		public static int CountUncommentedMatches()
		{
			string select = @"SELECT COUNT(source_pksid) FROM match_sample ms
LEFT JOIN match_imported mi ON ms.source_pksid = mi.pksid
WHERE
	mi.comment IS NULL AND
	id = (SELECT id FROM match_sample WHERE source_pksid = ms.source_pksid LIMIT 1) # for a given pksID, take any match";
			return Database.Count(select);
		}
		/// <summary>
		/// Return matches for the given day
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>
		public static IEnumerable<string> GetMatchesByDate(DateTime date)
		{
			string select = @"SELECT source_pksid FROM match_sample ms
LEFT JOIN match_imported mi ON ms.source_pksid = mi.pksid
LEFT JOIN songs s ON s.pksid = ms.source_pksid
WHERE
	mi.comment IS NULL AND
	ms.id = (SELECT id FROM match_sample WHERE source_pksid = ms.source_pksid LIMIT 1) # for a given pksID, take any match
AND DATE(s.created) = DATE(@date)";
			return Database.ListFetcher(select, dr => dr.GetString(0), "@date", date);
		}
		public static int CountMatchesByDate(DateTime date)
		{
			string select = @"SELECT COUNT(source_pksid) FROM match_sample ms
LEFT JOIN match_imported mi ON ms.source_pksid = mi.pksid
LEFT JOIN songs s ON s.pksid = ms.source_pksid
WHERE
	mi.comment IS NULL AND
	ms.id = (SELECT id FROM match_sample WHERE source_pksid = ms.source_pksid LIMIT 1) # for a given pksID, take any match
AND DATE(s.created) = DATE(@date)";
			return Database.Count(select, "@date", date);
		}

		public string GetRoolUrl()
		{
			return RemoteRootUrl(CustomerName, PksId);
		}
		public static string RemoteRootUrl(string customerName, string pksId)
		{
			return ConfigurationManager.AppSettings["SampleMatch.CustomersRootUrl"] + customerName + "/" + pksId;
		}
		public bool HasBeenMatched()
		{
			string matchedFileUrl = GetRoolUrl() + ".mp3.matches";
			return WebUtility.Exists(matchedFileUrl);
		}
		public bool ImportMatches()
		{
			string matchesFileUrl = GetRoolUrl() + ".mp3.matches/samples.mtyx";
			string matchesFileContent = WebUtility.DownloadString(matchesFileUrl);
			if (matchesFileContent != null)
				using (var sr = new StringReader(matchesFileContent))
				{
					var matches = new List<Match>();
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						var match = Match.Parse(line);
						if (match != null && match.SourceStart < 3000 && match.TargetStart < 3000) // Eliminate erroneous entries with huge match positions
							matches.Add(match);
						else
							Log.Info(matchesFileUrl);
					}

					InsertMatches(matches);
					return true;
				}
			return false;
		}

		public static SampleMatch Get(string pksid)
		{
			var ms = Database.ItemFetcher(@"SELECT s.user_id, s.title, s.duration, s.created FROM songs s WHERE s.pksid = @pksid",
				dr => new SampleMatch(pksid, dr.GetStringOrDefault(0), dr.GetStringOrDefault(1), dr.GetDecimalOrDefault(2), dr.GetDateOrNull(3)),
				"@pksid", pksid);
			if (ms != null)
			{
				var asSource = Match.GetForSource(pksid);
				var asTarget = Match.GetForTarget(pksid);
				var nonDuplicates = asTarget.Where(m1 => !asSource.Any(m2 => m2.TargetSample == m1.TargetSample)).ToList();
				ms.Matches = asSource;
				ms.Matches.AddRange(nonDuplicates);
			}

			return ms;
		}

		private void InsertMatches(IEnumerable<Match> matches)
		{
			using (var con = Database.Get())
			using (var transaction = con.BeginTransaction())
			{
				if (!IsImported(transaction))
				{
					foreach (var match in matches)
						match.Insert(transaction, PksId);

					MarkImported(transaction);

					transaction.Commit();
				}
			}
		}
		private bool IsImported(MySqlTransaction transaction)
		{
			return transaction.Connection.RecordExists(transaction, "match_imported", "pksid", PksId);
		}
		private void MarkImported(MySqlTransaction transaction)
		{
			Database.Insert(transaction.Connection, transaction,
				"INSERT INTO match_imported (pksid) VALUES (@pksid)",
				"@pksid", PksId);
		}

		public static ClipForInbox DownloadClipForInbox(string customerName, string pksId)
		{
			string xml = WebUtility.DownloadString(RemoteRootUrl(customerName, pksId).Replace("tbstrips-clients", "clients") + ".xml");
			try { return ClipForInbox.Deserialize(xml); }
			catch (Exception ex)
			{
				Log.Warn("DownloadClipForInbox " + ex.ToString());
			}
			return null;
		}

		public static void SetComment(string pksId, string masterPksId, string userId, string comment, int? analyzeDuration)
		{
			Database.ExecuteNonQuery(@"
UPDATE match_imported
SET
	master_pksid = @master_pksid,
	user_id = @user_id,
	comment = @comment,
	analyze_duration = @analyze_duration
WHERE pksid = @pksid",
				"@pksid", pksId,
				"@master_pksid", masterPksId,
				"@user_id", userId,
				"@comment", comment,
				"@analyze_duration", analyzeDuration);
		}

		public class Match
		{
			public string TargetSample;
			public decimal SourceStart;
			public decimal SourceEnd;
			public decimal TargetStart;
			public decimal TargetEnd;
			public decimal Duration;
			public decimal Difference;
			public DateTime MatchDate;
			public string CustomerName;
			public string FirstWords;
			public DateTime? CreateDate;

			public static Match Parse(string line)
			{
				string[] cells = line.Split('\t');

				try
				{
					var match = new Match();
					match.TargetSample = cells[0];
					if (match.TargetSample.EndsWith(".mp3.hash"))
						match.TargetSample = match.TargetSample.Substring(0, match.TargetSample.Length - ".mp3.hash".Length);

					match.TargetStart = decimal.Parse(cells[1], CultureInfo.InvariantCulture);
					match.TargetEnd = decimal.Parse(cells[2], CultureInfo.InvariantCulture);
					match.SourceStart = decimal.Parse(cells[3], CultureInfo.InvariantCulture);
					match.SourceEnd = decimal.Parse(cells[4], CultureInfo.InvariantCulture);
					match.Duration = decimal.Parse(cells[5], CultureInfo.InvariantCulture);
					match.Difference = decimal.Parse(cells[6], CultureInfo.InvariantCulture);
					match.MatchDate = DateTime.SpecifyKind(DateTime.Parse(cells[7]), DateTimeKind.Utc);

					return match;
				}
				catch { };

				return null;
			}

			public void Insert(MySqlTransaction transaction, string sourcePksId)
			{
				var cmd = transaction.Connection.CreateCommand();
				cmd.Transaction = transaction;
				cmd.CommandText = @"INSERT INTO match_sample
						(source_pksid, source_start, source_end, target_pksid, target_start, target_end, duration, difference, match_date)
						VALUES
						(@source_pksid, @source_start, @source_end, @target_pksid, @target_start, @target_end, @duration, @difference, @match_date)";
				cmd.Parameters.AddWithValue("@source_pksid", sourcePksId);
				cmd.Parameters.AddWithValue("@source_start", SourceStart);
				cmd.Parameters.AddWithValue("@source_end", SourceEnd);
				cmd.Parameters.AddWithValue("@target_pksid", TargetSample);
				cmd.Parameters.AddWithValue("@target_start", TargetStart);
				cmd.Parameters.AddWithValue("@target_end", TargetEnd);
				cmd.Parameters.AddWithValue("@duration", Duration);
				cmd.Parameters.AddWithValue("@difference", Difference);
				cmd.Parameters.AddWithValue("@match_date", MatchDate);
				cmd.ExecuteNonQuery();
			}

			/// <summary>
			/// Get target matches for that source
			/// </summary>
			/// <param name="sourcePksid"></param>
			/// <returns></returns>
			public static List<Match> GetForSource(string sourcePksid)
			{
				string select = @"SELECT ms.source_start, ms.source_end, ms.target_pksid, ms.target_start, ms.target_end, s.duration, ms.difference, ms.match_date, s.user_id, s.title, s.created
					FROM match_sample ms
					JOIN songs s ON s.pksid = ms.target_pksid
					WHERE ms.source_pksid = @source_pksid";

				return Database.ListFetcher<Match>(select,
					dr => new Match
					{
						SourceStart = dr.GetDecimal(0),
						SourceEnd = dr.GetDecimal(1),
						TargetSample = dr.GetString(2),
						TargetStart = dr.GetDecimal(3),
						TargetEnd = dr.GetDecimal(4),
						Duration = dr.GetDecimal(5),
						Difference = dr.GetDecimal(6),
						MatchDate = dr.GetDateTime(7),
						CustomerName = dr.GetNullableString(8),
						FirstWords = dr.GetNullableString(9),
						CreateDate = dr.GetNullableDate(10)
					},
					"@source_pksid", sourcePksid);
			}
			/// <summary>
			/// Get source marches for that target. Source and Target data are reversed when reading the table.
			/// </summary>
			/// <param name="targetPksid"></param>
			/// <returns></returns>
			public static List<Match> GetForTarget(string targetPksid)
			{
				string select = @"SELECT ms.target_start, ms.target_end, ms.source_pksid, ms.source_start, ms.source_end, s.duration, ms.difference, ms.match_date, s.user_id, s.title, s.created
					FROM match_sample ms
					JOIN songs s ON s.pksid = ms.source_pksid
					WHERE ms.target_pksid = @target_pksid";

				return Database.ListFetcher<Match>(select,
					dr => new Match
					{
						SourceStart = dr.GetDecimal(0),
						SourceEnd = dr.GetDecimal(1),
						TargetSample = dr.GetString(2),
						TargetStart = dr.GetDecimal(3),
						TargetEnd = dr.GetDecimal(4),
						Duration = dr.GetDecimal(5),
						Difference = dr.GetDecimal(6),
						MatchDate = dr.GetDateTime(7),
						CustomerName = dr.GetNullableString(8),
						FirstWords = dr.GetNullableString(9),
						CreateDate = dr.GetNullableDate(10)
					},
					"@target_pksid", targetPksid);
			}
		}
	}

	public class MatchesImportService
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		Thread _queueingThread;
		bool _shouldStop;
		public void Start()
		{
			_queueingThread = new Thread(QueueingServiceThreadProc) { Name = "Matches Import Service" };
			_shouldStop = false;
			_queueingThread.Start();
		}
		public void Stop()
		{
			_shouldStop = true;
			_queueingThread.Join();
		}
		void QueueingServiceThreadProc()
		{
			while (!_shouldStop)
			{
				try
				{
					int importedCount = Model.SampleMatch.ImportNewMatches();
					Log.Info(string.Format("Imported {0} matches", importedCount));
				}
				catch (Exception ex)
				{
					Log.Info(string.Format("Importing matches failed: {0}", ex.ToString()));
				}
				Thread.Sleep(TimeSpan.FromMinutes(15));
			}
		}
	}
}