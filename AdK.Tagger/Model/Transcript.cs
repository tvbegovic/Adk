using DatabaseCommon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace AdK.Tagger.Model
{
	public class Transcript
	{
		public int Id;
		public Guid SongId;
		public string PksId; // Used to build song mp3 filename
		public decimal Duration;
		public string UserId;
		public bool Training;
		public int? ReviewOf;
		public bool IsMaster;
		public StatusEnum Status;
		public DateTime? EditStart;
		public DateTime? EditEnd;
		public string FullText;
		public string SongTitle;
		public string Brand;
		public string Domain;

		
		public enum StatusEnum : byte
		{
			None = 0,
			CorrectionPending = 1,
			Corrected = 2,
			Trashed = 3
		}

		public List<Part> Parts = new List<Part>();

		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		public class Part
		{
			public int Id;
			public decimal TimeStart;
			public decimal TimeEnd;

			public string Text;

			public DateTime? EditStart;
			public DateTime? EditEnd;

			public void Insert( MySqlConnection db, MySqlTransaction transaction, int transcriptId )
			{
				var command = db.CreateCommand();
				command.Transaction = transaction;

				var stopWatch = Stopwatch.StartNew();

				command.CommandText = @"
					INSERT INTO song_transcript_part(transcript_id, time_start, time_end, text, edit_start, edit_end)
					VALUES (@transcript_id, @time_start, @time_end, @text, @edit_start, @edit_end)";

				command.Parameters.AddWithValue( "@transcript_id", transcriptId );
				command.Parameters.AddWithValue( "@time_start", TimeStart );
				command.Parameters.AddWithValue( "@time_end", TimeEnd );
				command.Parameters.AddWithValue( "@text", Text );
				command.Parameters.AddWithValue( "@edit_start", EditStart.Value );
				command.Parameters.AddWithValue( "@edit_end", EditEnd.Value );

				command.ExecuteNonQuery();

				Database.LogCommand(command, stopWatch);
			}
			public static List<Part> LoadParts( int transcriptId )
			{
				var parts = new List<Part>();

				using ( var db = Database.Get() ) {
					var command = db.CreateCommand();
					command.CommandText = @"SELECT id, time_start, time_end, text, edit_start, edit_end FROM song_transcript_part WHERE transcript_id = @id ORDER BY time_start";
					command.Parameters.AddWithValue( "@id", transcriptId );

					using ( var dr = command.ExecuteReader() ) {
						while ( dr.Read() ) {
							var part = new Part {
								Id = dr.GetInt32( 0 ),
								TimeStart = dr.GetInt32( 1 ),
								TimeEnd = dr.GetInt32( 2 ),
								Text = dr.GetString( 3 ),
								EditStart = dr.GetDateTime( 4 ),
								EditEnd = dr.GetDateTime( 5 )
							};
							parts.Add( part );
						}
					}
				}

				return parts;
			}
		}

		public static Transcript Get( int transcriptId )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"
				SELECT st.song_id, s.pksid, s.duration, st.training, st.review_of, st.user_id, st.edit_start, st.edit_end, s.title, s.brand
				FROM song_transcript st
				LEFT JOIN songs s
				ON s.id = st.song_id
				WHERE st.id = @transcriptId";
				command.Parameters.AddWithValue( "@transcriptId", transcriptId );

				using ( var dr = command.ExecuteReader() ) {
					if ( dr.Read() ) {
						var transcript = Create( dr.GetGuid( 0 ), dr.GetString( 1 ), dr.GetDecimal( 2 ), training: dr.GetBoolean( 3 ), reviewOf: dr.IsDBNull( 4 ) ? (int?)null : dr.GetInt32( 4 ), title: dr.GetStringOrDefault(5), brand:dr.GetStringOrDefault(6) );
						transcript.Id = transcriptId;
						transcript.UserId = dr.GetString( 5 );
						transcript.EditStart = dr.GetDateTime( 6 );
						transcript.EditEnd = dr.GetDateTime( 7 );
						transcript.Parts = Part.LoadParts( transcriptId );
						return transcript;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Selects a song whose transcript count hasn't been reached, not already transcribed by the given user, taken randomly
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		public static Transcript GetNextQueued( string userId, bool lookupUserDomains = false )
		{
			var stopWatch = Stopwatch.StartNew();
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();

				var songIdsBeingTranscribed = TranscriptConcurrency.GetSongIds();
				string excludeSongsBeingTranscribed = songIdsBeingTranscribed.Any() ?
					" NOT s.Id IN (" + string.Join( ",", songIdsBeingTranscribed.Select( id => "'" + id + "'" ) ) + ") AND " :
					string.Empty;

				string domainJoin = string.Empty, domainWhere = string.Empty;
				if(lookupUserDomains && Database.Exists("SELECT domain_id FROM user_domain WHERE user_id = @userId","@userId", userId))
				{
					domainJoin = " INNER JOIN user_domain ON domains.id = user_domain.domain_id AND user_domain.user_id = @userId";					
				}


				command.CommandText = string.Format(@"
				SELECT s.id, s.pksid, s.duration, s.title, s.brand, domains.domain_name
				FROM
				# select queued songs whose request_count has not been reached
				(SELECT song_id, priority FROM
					(SELECT
						song_id,
						SUM(request_count) as queue_length,
						(SELECT COUNT(id) FROM song_transcript st WHERE st.song_id = tq.song_id AND st.training = 0 AND st.review_of IS NULL) as queue_done,
						MAX(priority) as priority
					FROM song_transcript_queue tq
					INNER JOIN songs ON songs.id = tq.song_id
					WHERE songs.file_present = 1 OR songs.file_present IS NULL
					GROUP BY tq.song_id ORDER BY tq.created) as done
				WHERE queue_length > queue_done
				) wq

				INNER JOIN songs s ON wq.song_id = s.id # for PSKID and duration
				INNER JOIN harvested_clips ON s.id = harvested_clips.song_id			
                LEFT OUTER JOIN channels ON harvested_clips.channel_id = channels.id
                LEFT OUTER JOIN domains ON channels.domain = domains.domain				
				{1}
				WHERE
					# exclude the songs currently being transcribed
					{0}
					# exclude the songs already transcribed by this user
					NOT EXISTS (SELECT 1 FROM song_transcript st WHERE st.song_id = s.id AND st.user_id = @userId AND st.training = 0 AND st.review_of IS NULL)
				ORDER BY wq.priority DESC, s.created DESC
				LIMIT @randomExtend", excludeSongsBeingTranscribed, domainJoin );

				command.Parameters.AddWithValue( "@userId", userId );
				command.Parameters.AddWithValue( "@randomExtend", 10 );

				var transcripts = new List<Transcript>();
				
				using ( var dr = command.ExecuteReader() ) {
					Database.LogCommand(command, stopWatch);
					while ( dr.Read() )
						transcripts.Add( Create( dr.GetGuid( 0 ), dr.GetString( 1 ), dr.GetDecimal( 2 ), training: false, reviewOf: null, title: dr.GetStringOrDefault(3), brand: dr.GetStringOrDefault(4), domain: dr.GetStringOrDefault(5) ));
					Log.Info(string.Format("GetNextQueued: Got {0} transcripts", transcripts.Count));
					
				}

				return PickRandomWithFilePresent( transcripts );
			}
		}

		public static Transcript PickRandomWithFilePresent( List<Transcript> transcripts )
		{
			Log.Info("PickRandomWithFilePresent: Picking random song for transcribing");
			var random = new Random( DateTime.Now.Millisecond );
			while ( transcripts.Any() ) {
				int index = random.Next( transcripts.Count );
				var transcript = transcripts[index];
				bool? isFilePresent = TaggerSong.IsFilePresent( transcript.PksId );
				if ( isFilePresent == true )
				{
					Log.Info("PickRandomWithFilePresent: Found song for transcript pksId: " + transcript.PksId);
					return transcript;
				}
					
				else
				{
					Log.Info("No file present for song: " + transcript.PksId);
					transcripts.RemoveAt( index );
				}
					
			}
			return null;
		}

		#region Training
		// song_transcript_training -- songs ready for training
		// song_transcript with training = 1 -- transcriptions of training songs, user_id = null being the correct text
		// song_transcript with review_of not null -- corrected version of a transcript by another transcriber
		public static Transcript GetTraining( string userId )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"
SELECT s.id, s.pksid, s.duration
FROM song_transcript_training stt

INNER JOIN songs s ON stt.song_id = s.id # for PSKID and duration

WHERE
	# exclude the songs already transcribed by this user
	NOT EXISTS (SELECT 1 FROM song_transcript st WHERE st.song_id = s.id AND st.user_id = @userId AND st.training = 1)
ORDER BY s.created DESC";
				command.Parameters.AddWithValue( "@userId", userId );

				var transcripts = new List<Transcript>();
				using ( var dr = command.ExecuteReader() ) {
					while ( dr.Read() )
						transcripts.Add( Create( dr.GetGuid( 0 ), dr.GetString( 1 ), dr.GetDecimal( 2 ), training: true, reviewOf: null ) );
				}

				return PickRandomWithFilePresent( transcripts );
			}
		}
		public static int GetTrainingQueueLength( string userId )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"
SELECT COUNT(stt.id)
FROM song_transcript_training stt

WHERE
	# exclude the songs already transcribed by this user
	NOT EXISTS (SELECT 1 FROM song_transcript st WHERE st.song_id = stt.song_id AND st.user_id = @userId AND st.training = 1)";
				command.Parameters.AddWithValue( "@userId", userId );

				int i = (int)(long)command.ExecuteScalar();
				return i;
			}
		}
		public static void MoveToTraining( int transcriptId )
		{
			// TODO: verify that the song_id is not already in training,
			// mark the queue for this song as totally transcribed (or delete it from queue)
			// clear the transcript.user_id to null
			// then add the song_id into training
			throw new NotImplementedException();
		}
		public static void RestartTraining( string userId )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"DELETE FROM song_transcript WHERE song_transcript.user_id = @userId AND song_transcript.training = 1";
				command.Parameters.AddWithValue( "@userId", userId );
				command.ExecuteNonQuery();
			}
		}
		#endregion

		#region Review
		public static Transcript GetReview( string userId )
		{
			int reviewOneOutOf = 5;
			var queue = _GetReviewQueue( userId );
			if ( queue.Any() ) {
				// Pick a user that needs the most review
				var picked = queue.OrderByDescending( t => t.Item2 / reviewOneOutOf - t.Item3 ).First();

				using ( var db = Database.Get() ) {
					var command = db.CreateCommand();
					command.CommandText = @"
SELECT st.id, s.id, s.pksid, s.duration
FROM song_transcript st
INNER JOIN songs s ON st.song_id = s.id # for PSKID and duration
WHERE
	st.user_id = @userId AND
	st.training = 0
ORDER BY st.edit_start
LIMIT @skip, @take
";
					command.Parameters.AddWithValue( "@userId", userId );
					command.Parameters.AddWithValue( "@skip", reviewOneOutOf * picked.Item3 );
					command.Parameters.AddWithValue( "@take", reviewOneOutOf );

					var transcripts = new List<Transcript>();
					using ( var dr = command.ExecuteReader() ) {
						while ( dr.Read() )
							transcripts.Add( Create( dr.GetGuid( 1 ), dr.GetString( 2 ), dr.GetDecimal( 3 ), training: false, reviewOf: dr.GetInt32( 0 ) ) );
					}

					var transcript = PickRandomWithFilePresent( transcripts );
					if ( transcript != null )
						transcript.Parts = Part.LoadParts( transcript.ReviewOf.Value );
					return transcript;
				}
			}
			return null;
		}
		public static int GetReviewQueueLength( string userId )
		{
			int reviewOneOutOf = 5;
			var queue = _GetReviewQueue( userId );
			int queueLength = queue
				.Select( t => t.Item2 / reviewOneOutOf - t.Item3 )
				.DefaultIfEmpty()
				.Sum();
			return queueLength;
		}
		private static List<Tuple<Guid, int, int>> _GetReviewQueue( string userId )
		{
			int reviewOneOutOf = 5;
			var queue = new List<Tuple<Guid, int, int>>();

			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"
SELECT uc.user_id, IFNULL(transcripts.num, 0), IFNULL(reviews.num, 0)
FROM user_claim uc
LEFT JOIN
	(SELECT user_id, COUNT(id) as num FROM song_transcript st WHERE st.training = 0 AND st.review_of IS NULL GROUP BY st.user_id) transcripts
	ON transcripts.user_id = uc.user_id
LEFT JOIN
	(SELECT user_id, COUNT(id) as num FROM song_transcript st WHERE st.training = 0 AND st.review_of IS NOT NULL GROUP BY st.user_id) reviews
	ON reviews.user_id = uc.user_id

WHERE
	uc.name = 'module' AND uc.value = 'transcript' # keep users transcripting
	AND uc.user_id <> @userId # exclude current user as they don't review themselves
	AND IFNULL(transcripts.num, 0) >= @reviewOneOutOf * (IFNULL(reviews.num, 0) + 1)
";
				command.Parameters.AddWithValue( "@reviewOneOutOf", reviewOneOutOf );
				command.Parameters.AddWithValue( "@userId", userId );

				using ( var reader = command.ExecuteReader() ) {
					while ( reader.Read() )
						queue.Add( new Tuple<Guid, int, int>( reader.GetGuid( 0 ), reader.GetInt32( 1 ), reader.GetInt32( 2 ) ) );
				}
			}
			return queue;
		}
		#endregion

		public static int GetQueueLength( string userId, bool lookupUserDomains = false )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();

				var songIdsBeingTranscribed = TranscriptConcurrency.GetSongIds();
				string excludeSongsBeingTranscribed = songIdsBeingTranscribed.Any() ?
					" AND wq.song_id NOT IN (" + string.Join( ",", songIdsBeingTranscribed.Select( id => "'" + id + "'" ) ) + ") " :
					string.Empty;

				string domainJoin = string.Empty;
				if(lookupUserDomains && Database.Exists("SELECT domain_id FROM user_domain WHERE user_id = @userId","@userId", userId))
				{
					domainJoin = @"
							INNER JOIN harvested_clips ON songs.id = harvested_clips.song_id			
							INNER JOIN channels ON harvested_clips.channel_id = channels.id
							INNER JOIN domains ON channels.domain = domains.domain		
							INNER JOIN user_domain ON domains.id = user_domain.domain_id AND user_domain.user_id = @userId";					
				}

				command.CommandText = string.Format(@"
				SELECT COUNT(wq.song_id)
				FROM

				# select queued songs whose request_count has not been reached
				(SELECT song_id FROM
					(SELECT tq.song_id, SUM(request_count) as queue_length,
						(SELECT COUNT(id) FROM song_transcript st WHERE st.song_id = tq.song_id AND st.training = 0 AND st.review_of IS NULL) as queue_done
					FROM song_transcript_queue tq
					INNER JOIN songs ON songs.id = tq.song_id
					{0}
					WHERE songs.file_present = 1 OR songs.file_present IS NULL
					GROUP BY tq.song_id ORDER BY tq.created) as done
				WHERE queue_length > queue_done
				) wq

				WHERE
					# exclude the songs already transcribed by this user
				NOT EXISTS (SELECT 1 FROM song_transcript st WHERE st.song_id = wq.song_id AND st.user_id = @userId AND st.training = 0 AND st.review_of IS NULL) {1}"
				,domainJoin,excludeSongsBeingTranscribed);

				command.Parameters.AddWithValue( "@userId", userId );

				int i = (int)(long)command.ExecuteScalar();
				return i;
			}
		}
		public static void AddToQueue( Guid songId, int requestCount, byte priority = 50 )
		{
			Database.ExecuteNonQuery( @"INSERT INTO song_transcript_queue(song_id, request_count, priority) VALUES (@songId, @requestCount, @priority)",
				"@songId", songId,
				"@requestCount", requestCount,
				"@priority", priority );
		}

		public static void AddToQueueBulk(List<SongTranscriptQueue> queueRows)
		{
			using(var conn = Database.Get())
			{
				foreach(var row in queueRows)
				{
					Database.ExecuteNonQuery(conn, null, @"INSERT INTO song_transcript_queue(song_id, request_count, priority) VALUES (@songId, @requestCount, @priority)",
					"@songId", row.songId,
					"@requestCount", row.requestCount,
					"@priority", row.priority);
				}
			}
		}

		public static void Unqueue( string[] songIds )
		{
			string inClause = Database.InClause( songIds );

			#region Queue done
			string selectQueueDone = @"
SELECT song_id, COUNT(*)
FROM song_transcript WHERE training = 0 AND review_of IS NULL AND
song_id " + inClause + @"
GROUP BY song_id";

			var queueDone = Database
				.ListFetcher( selectQueueDone, dr => new Tuple<Guid, int>( dr.GetGuid( 0 ), dr.GetInt32( 1 ) ) )
				.ToDictionary( tuple => tuple.Item1, tuple => tuple.Item2 );
			#endregion

			#region Current queue
			string selectQueued = @"SELECT song_id, id, request_count FROM song_transcript_queue WHERE song_id " + inClause;
			var queued = Database
				.ListFetcher( selectQueued, dr => new Tuple<Guid, int, int>( dr.GetGuid( 0 ), dr.GetInt32( 1 ), dr.GetInt32( 2 ) ) )
				.GroupBy( tuple => tuple.Item1 );
			#endregion

			// Reduce queue to be less or equal to done
			foreach ( var songQueue in queued ) {
				int done = queueDone.ContainsKey( songQueue.Key ) ? queueDone[songQueue.Key] : 0;

				foreach ( var queueItem in songQueue ) {
					if ( done >= queueItem.Item3 )
						done -= queueItem.Item3;
					else // done < queueItem.Item3
					{
						Database.ExecuteNonQuery( "UPDATE song_transcript_queue SET request_count = @request_count WHERE id = @id",
							"@request_count", done,
							"@id", queueItem.Item2 );
						done = 0;
					}
				}
			}
		}

		private static Transcript Create( Guid songId, string pskId, decimal duration, bool training, int? reviewOf, string title = null, string brand = null, string domain = "" )
		{
			return new Transcript {
				SongId = songId,
				PksId = pskId,
				Duration = duration,
				Training = training,
				Parts = _CreateParts( duration ),
				ReviewOf = reviewOf,
				SongTitle = title,
				Brand = brand,
				Domain = domain
			};
		}
		public static List<Transcript> GetBySong( Guid songId )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();

				command.CommandText = @"
				SELECT st.id, s.pksid, s.duration, st.user_id, st.edit_start, st.edit_end, st.is_master, st.status, st.full_text
				FROM songs s
				LEFT JOIN song_transcript st
				ON s.id = st.song_id
				WHERE s.id = @songId AND st.training = 0 AND st.review_of IS NULL AND st.user_id IS NOT NULL";
				command.Parameters.AddWithValue( "@songId", songId.ToString() );

				var transcripts = new List<Transcript>();
				using ( var dr = command.ExecuteReader() ) {
					while ( dr.Read() ) {
						var transcript = Create( songId, dr.GetString( 1 ), dr.GetDecimal( 2 ), training: false, reviewOf: null );
						transcript.Id = dr.GetInt32( 0 );
						transcript.UserId = dr.GetString( 3 );
						transcript.EditStart = dr.GetDateTime( 4 );
						transcript.EditEnd = dr.GetDateTime( 5 );
						transcript.IsMaster = dr.GetBoolean( 6 );
						transcript.Status = (StatusEnum)dr.GetByte( 7 );
						transcript.Parts = Part.LoadParts( transcript.Id );
						transcript.FullText = dr.IsDBNull( 8 ) ? null : dr.GetString( 8 );
						transcripts.Add( transcript );
					}
				}
				return transcripts;
			}
		}
		public static Guid? GetSongId( int transcriptId )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();

				command.CommandText = @"SELECT song_id FROM song_transcript WHERE id = @transcriptId";
				command.Parameters.AddWithValue( "@transcriptId", transcriptId );

				object oGuid = command.ExecuteScalar();
				if ( oGuid is string ) {
					Guid songId;
					if ( Guid.TryParse( (string)oGuid, out songId ) )
						return songId;
				}
			}
			return null;
		}

		private static List<Part> _CreateParts( decimal songDuration )
		{
			decimal targetPartDuration = Configuration.Get().TargetPartDuration;

			int partCount = Math.Max( 1, (int)Math.Round( songDuration / targetPartDuration ) );

			var parts = new List<Part>();
			for ( int i = 0; i < partCount; ++i ) {
				var part = new Part {
					TimeStart = songDuration * i / partCount,
					TimeEnd = songDuration * (i + 1) / partCount
				};
				parts.Add( part );
			}
			return parts;
		}

		public void Insert()
		{
			Debug.Assert( Id == 0 );

			using ( var db = Database.Get() )
			using ( var transaction = db.BeginTransaction() ) {
				var command = db.CreateCommand();
				command.Transaction = transaction;
				var stopWatch = Stopwatch.StartNew();

				command.CommandText = @"
					INSERT INTO song_transcript(song_id, user_id, training, review_of, edit_start, edit_end, status, is_master)
					VALUES (@song_id, @user_id, @training, @review_of, @edit_start, @edit_end, @status, @is_master)";

				command.Parameters.AddWithValue( "@song_id", SongId.ToString() );
				command.Parameters.AddWithValue( "@user_id", UserId );
				command.Parameters.AddWithValue( "@training", Training );
				command.Parameters.AddWithValue( "@review_of", ReviewOf );
				command.Parameters.AddWithValue( "@edit_start", EditStart.Value );
				command.Parameters.AddWithValue( "@edit_end", EditEnd.Value );
				command.Parameters.AddWithValue( "@status", Status );
				command.Parameters.AddWithValue( "@is_master", IsMaster );

				command.ExecuteNonQuery();

				Database.LogCommand(command, stopWatch);

				
								
				this.Id = (int)command.LastInsertedId;

				Log.Info(string.Format("Inserted transcript: UserId={4} Id={0}, SongId={1}, start={2}, end={3} ",this.Id, SongId, EditStart, EditEnd,UserId));

				foreach ( var part in Parts )
				{
					part.Insert( db, transaction, this.Id );
					Log.Info(string.Format("Inserted transcript part: id={0}, start={1}, end={2}, text={3}", Id, part.EditStart, part.EditEnd, part.Text)); 
				}
					

				transaction.Commit();
			}
		}

		public static void FixMissingFullText()
		{
			var transcriptIds = Database.ListFetcher( "SELECT id FROM song_transcript WHERE training = 0 AND is_master = 1 AND full_text IS NULL", dr => dr.GetInt32( 0 ) );
			foreach ( int transcriptId in transcriptIds ) {
				var parts = Part.LoadParts( transcriptId );
				string fullText = string.Join( " ", parts.Select( p => p.Text ).ToArray() );
				SaveFullText( transcriptId, fullText, true, StatusEnum.Corrected );
			}
		}
		public static void SaveFullText( int transcriptId, string fullText, bool isMaster, StatusEnum status, bool updateTimeModified = true )
		{
			using ( var db = Database.Get() ) {
				var cmdUpdate = db.CreateCommand();
				string timeModified = "";
				if ( updateTimeModified ) {
					timeModified = ", time_modified = @timeModified";
					cmdUpdate.Parameters.AddWithValue( "@timeModified", DateTime.UtcNow );

				}
				cmdUpdate.CommandText = String.Format( @"UPDATE song_transcript SET full_text = @fullText, is_master = @isMaster, status = @status {0} WHERE id = @id", timeModified );

				cmdUpdate.Parameters.AddWithValue( "@id", transcriptId );
				cmdUpdate.Parameters.AddWithValue( "@fullText", fullText );
				cmdUpdate.Parameters.AddWithValue( "@isMaster", isMaster );
				cmdUpdate.Parameters.AddWithValue( "@status", status );
				cmdUpdate.ExecuteNonQuery();
			}
		}
		public static void SetMaster( int transcriptId )
		{
			Guid? songId = Transcript.GetSongId( transcriptId );
			if ( !songId.HasValue ) // That would mean that no transcript has that ID
				return;

			using ( var db = Database.Get() )
			using ( var transaction = db.BeginTransaction() ) {
				var cmdClearMaster = db.CreateCommand();
				cmdClearMaster.Transaction = transaction;
				cmdClearMaster.CommandText = @"UPDATE song_transcript SET is_master = 0 WHERE song_id = @songId AND training = 0 AND review_of IS NULL";
				cmdClearMaster.Parameters.AddWithValue( "@songId", songId.Value.ToString() );
				cmdClearMaster.ExecuteNonQuery();

				var cmdSetMaster = db.CreateCommand();
				cmdSetMaster.Transaction = transaction;
				cmdSetMaster.CommandText = @"UPDATE song_transcript SET is_master = 1, status = @status WHERE id = @transcriptId";
				cmdSetMaster.Parameters.AddWithValue( "@transcriptId", transcriptId );
				cmdSetMaster.Parameters.AddWithValue( "@status", StatusEnum.Corrected );
				cmdSetMaster.ExecuteNonQuery();

				transaction.Commit();
			}
		}

		/// <summary>
		/// Returns statistics for each user about the number of transcribed songs, the overall performance
		/// </summary>
		/// <returns></returns>
		public static List<TranscriptStatistic> GetStatistics()
		{
			string select = @"SELECT song_transcript.user_id, users.email, accounts.name,
count(song_transcript.song_id) as song_count,
sum(TIMESTAMPDIFF(SECOND, song_transcript.edit_start, song_transcript.edit_end)) as edit_time,
sum(songs.duration) as songs_duration
FROM song_transcript
INNER JOIN songs ON song_transcript.song_id = songs.id
INNER JOIN users ON song_transcript.user_id = users.id
INNER JOIN user_claim ON song_transcript.user_id = user_claim.user_id
LEFT JOIN accounts ON accounts.user_id = users.id
WHERE user_claim.name = 'module' AND user_claim.value = 'transcript' AND song_transcript.training = 0 AND song_transcript.review_of IS NULL
GROUP BY user_id";
			return Database.ListFetcher( select, dr => new TranscriptStatistic {
				UserId = dr.GetString( 0 ),
				Email = dr.GetString( 1 ),
				Name = dr.GetNullableString( 2 ),
				SongCount = dr.GetInt32( 3 ),
				EditTime = dr.GetDecimal( 4 ),
				SongDuration = dr.GetDecimal( 5 )
			}
			);
		}
		/// <summary>
		/// Returns statistics for each user about the number of transcribed songs in the defined period, the overall performance
		/// </summary>
		/// <returns></returns>
		public static List<TranscriptStatistic> GetStatistics( DateTime dateStart, DateTime dateEnd )
		{
			string select = @"SELECT song_transcript.user_id, users.email, accounts.name,
count(song_transcript.song_id) as song_count,
sum(TIMESTAMPDIFF(SECOND, song_transcript.edit_start, song_transcript.edit_end)) as edit_time,
sum(songs.duration) as songs_duration
FROM song_transcript
INNER JOIN songs ON song_transcript.song_id = songs.id
INNER JOIN users ON song_transcript.user_id = users.id
INNER JOIN user_claim ON song_transcript.user_id = user_claim.user_id
LEFT JOIN accounts ON accounts.user_id = users.id
WHERE user_claim.name = 'module' AND user_claim.value = 'transcript' AND
	song_transcript.training = 0 AND song_transcript.review_of IS NULL AND
	DATE(song_transcript.edit_start + INTERVAL @timeZoneOffset MINUTE) >= Date(@dateStart) AND
	DATE(song_transcript.edit_start + INTERVAL @timeZoneOffset MINUTE) < Date(@dateEnd)
GROUP BY user_id";
			return Database.ListFetcher( select, dr => new TranscriptStatistic {
				UserId = dr.GetString( 0 ),
				Email = dr.GetString( 1 ),
				Name = dr.GetNullableString( 2 ),
				SongCount = dr.GetInt32( 3 ),
				EditTime = dr.GetDecimal( 4 ),
				SongDuration = dr.GetDecimal( 5 )
			},
				"@timeZoneOffset", Localization.TimeZoneOffset,
				"@dateStart", dateStart,
				"@dateEnd", dateEnd
			);
		}
		public static List<TranscriptStatistic> GetStatistics( string userId )
		{
			string select = @"SELECT DATE(song_transcript.edit_start + INTERVAL @timeZoneOffset MINUTE), users.email, accounts.name,
count(song_transcript.song_id) as song_count,
sum(TIMESTAMPDIFF(SECOND, song_transcript.edit_start, song_transcript.edit_end)) as edit_time,
sum(songs.duration) as songs_duration
FROM song_transcript
INNER JOIN songs ON song_transcript.song_id = songs.id
INNER JOIN users ON song_transcript.user_id = users.id
LEFT JOIN accounts ON accounts.user_id = users.id
WHERE song_transcript.user_id = @userId AND song_transcript.training = 0 AND song_transcript.review_of IS NULL
GROUP BY DATE(song_transcript.edit_start + INTERVAL @timeZoneOffset MINUTE)";
			return Database.ListFetcher( select, dr => new TranscriptStatistic {
				Day = dr.GetDateTime( 0 ),
				Email = dr.GetString( 1 ),
				Name = dr.GetNullableString( 2 ),
				SongCount = dr.GetInt32( 3 ),
				EditTime = dr.GetDecimal( 4 ),
				SongDuration = dr.GetDecimal( 5 )
			},
				"@userId", userId,
				"@timeZoneOffset", Localization.TimeZoneOffset
			);
		}
		public static List<TranscriptStatistic> GetStatistics( string userId, DateTime dateStart, DateTime dateEnd )
		{
			string select = @"SELECT DATE(song_transcript.edit_start + INTERVAL @timeZoneOffset MINUTE), users.email, accounts.name,
count(song_transcript.song_id) as song_count,
sum(TIMESTAMPDIFF(SECOND, song_transcript.edit_start, song_transcript.edit_end)) as edit_time,
sum(songs.duration) as songs_duration
FROM song_transcript
INNER JOIN songs ON song_transcript.song_id = songs.id
INNER JOIN users ON song_transcript.user_id = users.id
LEFT JOIN accounts ON accounts.user_id = users.id
WHERE song_transcript.user_id = @userId AND song_transcript.training = 0 AND song_transcript.review_of IS NULL AND
	DATE(song_transcript.edit_start + INTERVAL @timeZoneOffset MINUTE) >= Date(@dateStart) AND
	DATE(song_transcript.edit_start + INTERVAL @timeZoneOffset MINUTE) < Date(@dateEnd)
GROUP BY DATE(song_transcript.edit_start + INTERVAL @timeZoneOffset MINUTE)";
			return Database.ListFetcher( select, dr => new TranscriptStatistic {
				Day = dr.GetDateTime( 0 ),
				Email = dr.GetString( 1 ),
				Name = dr.GetNullableString( 2 ),
				SongCount = dr.GetInt32( 3 ),
				EditTime = dr.GetDecimal( 4 ),
				SongDuration = dr.GetDecimal( 5 )
			},
				"@userId", userId,
				"@timeZoneOffset", Localization.TimeZoneOffset,
				"@dateStart", dateStart,
				"@dateEnd", dateEnd
			);
		}
		public static List<TranscriptStatistic> GetStatistics( string userId, DateTime date )
		{
			var statistics = new List<TranscriptStatistic>();

			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"SELECT song_transcript.id, song_transcript.edit_start,
TIMESTAMPDIFF(SECOND, song_transcript.edit_start, song_transcript.edit_end) as edit_time,
songs.duration as song_duration
FROM song_transcript
INNER JOIN songs ON song_transcript.song_id = songs.id
WHERE song_transcript.user_id = @userId AND song_transcript.training = 0 AND song_transcript.review_of IS NULL AND DATE(song_transcript.edit_start + INTERVAL @timeZoneOffset MINUTE) = Date(@date)";
				command.Parameters.AddWithValue( "@userId", userId );
				command.Parameters.AddWithValue( "@timeZoneOffset", Localization.TimeZoneOffset );
				command.Parameters.AddWithValue( "@date", date );

				using ( var dr = command.ExecuteReader() ) {
					while ( dr.Read() ) {
						var statistic = new TranscriptStatistic {
							Id = dr.GetInt32( 0 ),
							Day = dr.GetDateTime( 1 ),
							EditTime = dr.GetDecimal( 2 ),
							SongDuration = dr.GetDecimal( 3 )
						};
						statistics.Add( statistic );
					}
				}
			}

			LoadTexts( statistics );

			return statistics;
		}

		public static List<TranscriptStatistic> GetStatistics( string userId, DateTime dateStart, int dayCount )
		{
			var statistics = GetStatistics( userId );

			statistics = Enumerable.Range( 0, dayCount )
				.Select( iDay => {
					var item = statistics.FirstOrDefault( s => s.Day.Value.Date == dateStart.AddDays( iDay ).Date );
					if ( item == null )
						item = new TranscriptStatistic {
							Day = dateStart.AddDays( iDay ).Date,
							SongCount = 0,
							EditTime = 0,
							SongDuration = 0,
							UserId = userId
						};
					return item;
				} )
				.ToList();

			return statistics;
		}

		public static void LoadTexts( List<TranscriptStatistic> statistics )
		{
			using ( var db = Database.Get() ) {
				foreach ( var statistic in statistics ) {
					var command = db.CreateCommand();
					command.CommandText = @"SELECT text FROM song_transcript_part WHERE transcript_id = @id ORDER BY time_start";
					command.Parameters.AddWithValue( "@id", statistic.Id );

					var text = new StringBuilder();
					using ( var dr = command.ExecuteReader() ) {
						while ( dr.Read() )
							text.AppendLine( dr.GetString( 0 ) );
					}
					statistic.Text = text.ToString();
				}
			}
		}
		public string GetTranscriptPartsFullText()
		{
			return string.Join( " ", Parts.Select( p => p.Text ).ToArray() );
		}

		public decimal? GetPerformance()
		{
			if ( EditStart.HasValue && EditEnd.HasValue && Duration > 0 ) {
				TimeSpan EditTime = EditEnd.Value - EditStart.Value;
				return (decimal)EditTime.TotalSeconds / Duration;
			}
			return null;
		}
		public enum PerformanceGrade
		{
			NONE,
			BAD,
			GOOD,
			EXCELLENT
		}
		public static PerformanceGrade GetPerformanceGrade( decimal? performance )
		{
			if ( !performance.HasValue )
				return PerformanceGrade.NONE;
			return performance < 2 ?
				PerformanceGrade.EXCELLENT :
				(performance < 5 ? PerformanceGrade.GOOD : PerformanceGrade.BAD);
		}

		public class Configuration
		{
			public bool SendMailOnNewTranscript;
			public string SendMailOnNewTranscriptTo;
			public int NotificationThrottleDelay;
			public decimal TargetPartDuration;
			public string OrderNotificationSubject;
			public string OrderNotificationBody;
			public string TranscriptDoneNotificationSubject;
			public string TranscriptDoneNotificationBody;
			public string AwaitingTranscriptsNotificationSubject;
			public string AwaitingTranscriptsNotificationBody;
			public bool AutoValidateFinishedTranscripts;
			public string NewUserDefaultRights;
			public decimal EarningPerSecond;

			public bool AutoTranscribeNewSamples;
			public int NewSamplesTranscriptionCount;
			public DateTime? AutoTranscribeStartDate;
			public bool FilterByUserDomain;
			public string Userid;

			private const string _Module = "Transcript";
			public static Configuration Get()
			{
				return new Configuration {
					SendMailOnNewTranscript = Settings.Get( _Module, "SendMailOnNewTranscript", false ),
					SendMailOnNewTranscriptTo = Settings.Get( _Module, "SendMailOnNewTranscript.To" ),
					NotificationThrottleDelay = Settings.Get( _Module, "NotificationThrottleDelay", 15 ),
					TargetPartDuration = Settings.Get( _Module, "TargetPartDuration", 5M ),
					OrderNotificationSubject = Settings.Get( _Module, "OrderNotification.Subject", "New samples available for transcription" ),
					OrderNotificationBody = Settings.Get( _Module, "OrderNotification.Body", @"<p>Dear [transcriberName],</p>
<p>new samples are available for you to transcribe.</p>
<p>[transcriptUrl]</p>" ),
					TranscriptDoneNotificationSubject = Settings.Get( _Module, "TranscriptDoneNotification.Subject", "Transcript done at [transcriptDateTime]" ),
					TranscriptDoneNotificationBody = Settings.Get( _Module, "TranscriptDoneNotification.Body", @"<p>User: [transcriberName]</p>
<p>Media: [sampleUrl]</p>
<p>Text:</p>
<p>[transcriptText]</p>
<p>Performance: [performance] ([performanceGrade])</p>" ),
					AwaitingTranscriptsNotificationSubject = Settings.Get( _Module, "AwaitingTranscriptsNotification.Subject", "There are samples waiting for you to transcribe" ),
					AwaitingTranscriptsNotificationBody = Settings.Get( _Module, "AwaitingTranscriptsNotification.Body", @"<p>Dear: [transcriberName]</p>
<p>[transcriptCount] samples are awaiting you for transcription.</p>
<p>[transcriptUrl]</p>" ),
					AutoValidateFinishedTranscripts = Settings.Get( _Module, "AutoValidateFinishedTranscripts", false ),
					NewUserDefaultRights = Settings.Get( _Module, "NewUserDefaultRights" ),
					EarningPerSecond = Settings.Get( _Module, "EarningPerSecond", 0M ),

					AutoTranscribeNewSamples = Settings.Get( _Module, "AutoTranscribeNewSamples", false ),
					NewSamplesTranscriptionCount = Settings.Get( _Module, "NewSamplesTranscriptionCount", 1 ),
					AutoTranscribeStartDate = Settings.Get( _Module, "AutoTranscribeStartDate", DateTime.Now.Date ),
					FilterByUserDomain = Settings.Get(_Module,"FilterByUserDomain", false),
					Userid = Settings.Get(_Module, "UserId")

				};
			}

			public void Save()
			{
				Settings.Set( _Module, "SendMailOnNewTranscript", SendMailOnNewTranscript );
				Settings.Set( _Module, "SendMailOnNewTranscript.To", SendMailOnNewTranscriptTo );
				Settings.Set( _Module, "NotificationThrottleDelay", NotificationThrottleDelay );
				Settings.Set( _Module, "TargetPartDuration", TargetPartDuration );
				Settings.Set( _Module, "OrderNotification.Subject", OrderNotificationSubject );
				Settings.Set( _Module, "OrderNotification.Body", OrderNotificationBody );
				Settings.Set( _Module, "TranscriptDoneNotification.Subject", TranscriptDoneNotificationSubject );
				Settings.Set( _Module, "TranscriptDoneNotification.Body", TranscriptDoneNotificationBody );
				Settings.Set( _Module, "AwaitingTranscriptsNotification.Subject", AwaitingTranscriptsNotificationSubject );
				Settings.Set( _Module, "AwaitingTranscriptsNotification.Body", AwaitingTranscriptsNotificationBody );
				Settings.Set( _Module, "AutoValidateFinishedTranscripts", AutoValidateFinishedTranscripts );
				Settings.Set( _Module, "NewUserDefaultRights", NewUserDefaultRights );
				Settings.Set( _Module, "EarningPerSecond", EarningPerSecond );

				Settings.Set( _Module, "AutoTranscribeNewSamples", AutoTranscribeNewSamples );
				Settings.Set( _Module, "NewSamplesTranscriptionCount", NewSamplesTranscriptionCount );
				Settings.Set( _Module, "AutoTranscribeStartDate", AutoTranscribeStartDate );
				Settings.Set( _Module, "FilterByUserDomain", FilterByUserDomain );
			}

			public static DateTime? GetLastSent()
			{
				return Settings.Get( _Module, "OrderNotification.LastDate", (DateTime?)null );
			}
			public static void SetLastSend( DateTime? value )
			{
				Settings.Set( _Module, "OrderNotification.LastDate", value );
			}
		}

		[TestClass]
		public class ConfigurationTest
		{
			[TestMethod]
			public void LastSendDateRoundTrip()
			{
				DateTime? d1 = null;
				Configuration.SetLastSend( d1 );
				DateTime? d2 = Configuration.GetLastSent();
				Assert.AreEqual( d1, d2 );

				d1 = DateTime.UtcNow;
				Configuration.SetLastSend( d1 );
				d2 = Configuration.GetLastSent();
				Assert.AreEqual( d1, d2 );
			}
		}

		public static List<Transcript> GetBySongIds( IList<Guid> songIds )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();

				command.CommandText = @"
				SELECT st.id, st.full_text, st.song_id, edit_start
				FROM song_transcript st				
				WHERE st.song_id " + Database.InClause(songIds);
				
				var transcripts = new List<Transcript>();
				using ( var dr = command.ExecuteReader() ) {
					while ( dr.Read() ) {
						var transcript = new Transcript();
						transcript.Id = dr.GetInt32( 0 );
						transcript.FullText = dr.IsDBNull( 1 ) ? null : dr.GetString( 1 );
						transcript.SongId = dr.GetGuid(2);
						transcript.EditStart = dr.GetDateOrNull(3);
						transcripts.Add( transcript );
					}
				}
				return transcripts.GroupBy(t=>t.SongId).Select(g=> g.OrderByDescending(t=>t.EditStart).FirstOrDefault()).ToList();
			}
		}
	}
	public class TranscriptStatistic
	{
		public int Id;
		public string UserId;
		public string Email;
		public string Name;
		public DateTime? Day;
		public int SongCount;
		/// <summary>
		/// Edit time in seconds
		/// </summary>
		public decimal EditTime;
		/// <summary>
		/// Sample duration in seconds
		/// </summary>
		public decimal SongDuration;
		public string Text;

		public decimal? Performance { get { return SongDuration != 0 ? (decimal?)EditTime / SongDuration : null; } }
	}

	public class SongTranscriptQueue
	{
		public int id;
		public Guid songId;
		public int requestCount;
		public byte priority;

		public static List<SongTranscriptQueue> GetForSongIds(IList<Guid> songIds)
		{
			return Database.ListFetcher("SELECT id, song_id, request_count, priority FROM song_transcript_queue WHERE song_id " + Database.InClause(songIds),
				dr => new SongTranscriptQueue
				{
					id = dr.GetInt32(0),
					songId = dr.GetGuidOrDefault(1),
					requestCount = dr.GetIntOrDefault(2),
					priority = dr.GetByte(3)
				});
		}
	}

	public class TranscriptQueueingService
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();
		Thread _queueingThread;
		bool _shouldStop;
		public void Start()
		{
			_queueingThread = new Thread( QueueingServiceThreadProc ) { Name = "Transcript Queueing Service" };
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
			while ( !_shouldStop ) {
				var config = Transcript.Configuration.Get();

				if ( config.AutoTranscribeNewSamples && config.NewSamplesTranscriptionCount > 0 ) {
					int totalCount;
					var samples = Sample.GetFiltered(
						pageNum: 0,
						pageSize: 100, // If there are more than that amount, they will be handled in the next iteration
						sortColumn: null, // defaults to create date
						ascending: false, // provides the most recent samples first
						startDate: config.AutoTranscribeStartDate,
						endDate: null,
						text: null,
						status: "notOrdered", // Samples for which transcripts have not yet been ordered
						product: null,
						category: null,
						company: null,
						brand: null,
						userId: config.Userid,
						totalCount: out totalCount
					);
					var songIds = samples.Select(s => s.Id).ToList();
					var existingQueueItemsSongIds = SongTranscriptQueue.GetForSongIds(songIds).Select(q => q.songId).ToList();
					if(existingQueueItemsSongIds.Count > 0)
					{
						Log.Warn<string>("QueueingService - samples contain existing queue items. SongIds: " + string.Join(", ", existingQueueItemsSongIds));
					}
					
					samples = samples.Where(s => !existingQueueItemsSongIds.Contains(s.Id)).ToList();
					var samplesToBeAdded = new List<Sample>();

					foreach (var g in samples.GroupBy(s => s.Id))
					{
						if(g.Count() > 1)
						{
							//Found duplicate songs
							Log.Warn<string>("QueueingService - samples contain duplicate. SongId: " + g.Key.ToString());
						}
						Log.Info<string>(string.Format("Queued for Transcription '{0}' x{1}", g.First().Id, config.NewSamplesTranscriptionCount));
						samplesToBeAdded.Add(g.First());
					}
					
					Transcript.AddToQueueBulk(samplesToBeAdded.Select(s => new SongTranscriptQueue
					{
						songId = s.Id,
						requestCount = config.NewSamplesTranscriptionCount,
						priority = 50
					}).ToList());											
					
				}

				Thread.Sleep( TimeSpan.FromMinutes( 1 ) );
			}
		}
	}
}
