using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Net;

namespace AdK.Tagger.Model
{
	public class TaggerSong
	{
		public int Id;
		public Guid SongId;
		public string Title;
		public string PksId; // Used to build song mp3 filename

		public static TaggerSong GetNext( string userId, string filter )
		{
			using ( var connection = Database.Get() ) {
				var nextSong = _GetNext( connection, userId, filter );

				if ( nextSong == null ) {
					_LoadMore( connection );
					nextSong = _GetNext( connection, userId, filter );
				}

				return nextSong;
			}
		}
		public static string[] GetUntagged( string userId, string filter, int max )
		{
			using ( var connection = Database.Get() ) {
				var command = connection.CreateCommand();

				command.CommandText = @"
					SELECT title FROM tagger_song
					WHERE
						(title LIKE @filter OR title LIKE @filter2) AND
						NOT EXISTS (SELECT 1 FROM tagger_vote WHERE tagger_vote.tagger_song_id = tagger_song.id AND tagger_vote.user_id = @userId)
					LIMIT @max";

				command.Parameters.AddWithValue( "@userId", userId );
				command.Parameters.AddWithValue( "@filter", filter + "%" );
				command.Parameters.AddWithValue( "@filter2", "% " + filter + "%" );
				command.Parameters.AddWithValue( "@max", max );

				var titles = new List<string>();
				using ( var dr = command.ExecuteReader() ) {
					while ( dr.Read() )
						titles.Add( dr.GetString( 0 ) );
				}
				return titles.ToArray();
			}
		}
		public static List<TaggerSong> GetTagged( int tagId )
		{
			using ( var connection = Database.Get() ) {
				var command = connection.CreateCommand();

				command.CommandText = @"SELECT tagger_song.id, tagger_song.song_id, tagger_song.title, songs.pksid
					FROM tagger_song, tagger_vote, tagger_vote_tag, songs
					WHERE
						tagger_vote.tagger_song_id = tagger_song.id AND
						tagger_vote.id = tagger_vote_tag.tagger_vote_id AND
						tagger_song.song_id = songs.id AND
						tagger_vote_tag.tagger_tag_id = @tagId
					GROUP BY tagger_song.id
					LIMIT @max";

				command.Parameters.AddWithValue( "@tagId", tagId );
				command.Parameters.AddWithValue( "@max", 10 );

				var songs = new List<TaggerSong>();
				using ( var dr = command.ExecuteReader() ) {
					while ( dr.Read() ) {
						songs.Add( new TaggerSong {
							Id = dr.GetInt32( 0 ),
							SongId = dr.GetGuid( 1 ),
							Title = dr.GetString( 2 ),
							PksId = dr.GetString( 3 )
						} );
					}
				}

				return songs;
			}
		}
		private static TaggerSong _GetNext( MySqlConnection connection, string userId, string filter )
		{
			var command = connection.CreateCommand();

			bool hasFilter = !string.IsNullOrWhiteSpace( filter );
			string query = @"
				SELECT ts.id, ts.song_id, ts.title FROM tagger_song ts join songs s on ts.song_id = s.id
				WHERE
					{0}
					NOT EXISTS (SELECT 1 FROM tagger_vote WHERE tagger_vote.tagger_song_id = ts.id AND tagger_vote.user_id = @userId)
				order by s.created desc
				LIMIT 1";
			command.CommandText = string.Format( query, hasFilter ? "(ts.title LIKE @filter OR ts.title LIKE @filter2) AND" : "" );

			command.Parameters.AddWithValue( "@userId", userId );
			if ( hasFilter ) {
				command.Parameters.AddWithValue( "@filter", filter + "%" );
				command.Parameters.AddWithValue( "@filter2", "% " + filter + "%" );
			}

			TaggerSong nextSong = null;

			using ( var dr = command.ExecuteReader() ) {
				if ( dr.Read() ) {
					nextSong = new TaggerSong {
						Id = dr.GetInt32( 0 ),
						SongId = dr.GetGuid( 1 ),
						Title = dr.GetString( 2 )
					};
				}
			}

			if ( nextSong != null )
				nextSong.PksId = GetPksId( connection, null, nextSong.SongId );

			return nextSong;
		}
		private static int _LoadMore( MySqlConnection connection, int quantity = 200 )
		{
			int loadedCount = 0;

			using ( var transaction = connection.BeginTransaction() ) {
				var command = connection.CreateCommand();
				command.Transaction = transaction;
				command.CommandText = @"
					SELECT id, title FROM songs
					WHERE
						product_id IS NULL AND
						deleted = 0 AND
						NOT EXISTS (SELECT 1 FROM tagger_song WHERE tagger_song.song_id = songs.id)";
				if ( quantity > 0 ) {
					command.CommandText += " LIMIT @quantity";
					command.Parameters.AddWithValue( "@quantity", quantity );
				}

				var loadedSongs = new List<TaggerSong>();

				using ( var dr = command.ExecuteReader() ) {
					while ( dr.Read() )
						loadedSongs.Add( new TaggerSong {
							SongId = dr.GetGuid( 0 ),
							Title = dr.GetString( 1 )
						} );
				}

				loadedCount = loadedSongs.Count;

				foreach ( var song in loadedSongs )
					song._InsertIntoDb( connection );

				transaction.Commit();
			}

			return loadedCount;
		}
		private static int _CountRemaining( MySqlConnection connection, string userId )
		{
			var command = connection.CreateCommand();
			command.CommandText = @"
				SELECT COUNT(id) FROM tagger_song
				WHERE
					NOT EXISTS (SELECT 1 FROM tagger_vote WHERE tagger_vote.tagger_song_id = tagger_song.id AND tagger_vote.user_id = @userId)";
			command.Parameters.AddWithValue( "@userId", userId );

			object scalar = command.ExecuteScalar();
			return scalar is int ? (int)scalar : 0;
		}
		private void _InsertIntoDb( MySqlConnection connection )
		{
			var command = connection.CreateCommand();
			command.CommandText = @"INSERT INTO tagger_song (song_id, title) VALUES (@song_id, @title)";
			command.Parameters.AddWithValue( "@song_id", this.SongId.ToString() );
			command.Parameters.AddWithValue( "@title", this.Title );
			command.ExecuteNonQuery();
			this.Id = (int)command.LastInsertedId;
		}
		public static string GetPksId( MySqlConnection connection, MySqlTransaction transaction, Guid songId )
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT pksid FROM songs WHERE id = @id";
			command.Parameters.AddWithValue( "@id", songId.ToString() );

			using ( var dr = command.ExecuteReader() ) {
				if ( dr.Read() )
					return dr.GetString( 0 );
			}
			return null;
		}
		public static string GetPksId( Guid songId )
		{
			using ( var db = Database.Get() )
				return GetPksId( db, null, songId );
		}

		#region MP3 file
		private static readonly NLog.Logger LogIsFilePresent = NLog.LogManager.GetCurrentClassLogger();

		public static string GetMp3Url( string pksId, string userId = null )
		{
			return Song.GetMp3Url( pksId, userId );
		}

		/// <summary>
		/// Checks wether the file_present flag is set. If not, verifies the file presence and sets the flag.
		/// </summary>
		/// <param name="pksId"></param>
		/// <returns>True if the audio file is available at the expected URL</returns>
		public static bool? IsFilePresent( string pksId )
		{
			bool? isFilePresent = _GetFilePresentFlag( pksId );
			if ( isFilePresent.HasValue )
				return isFilePresent.Value;
			else {
				isFilePresent = IsFilePresentNow( pksId );
				if ( isFilePresent.HasValue )
					_SetFilePresentFlag( pksId, isFilePresent.Value );
			}
			return isFilePresent;
		}
		public static bool? IsFilePresentNow( string pksId )
		{
			bool? isFilePresent = null;

			string fileUrl = GetMp3Url( pksId );
			var request = WebRequest.Create( fileUrl );
			request.Method = "HEAD";

			try {
				HttpStatusCode statusCode;
				using ( HttpWebResponse response = (HttpWebResponse)request.GetResponse() )
					statusCode = response.StatusCode;
				isFilePresent = statusCode == HttpStatusCode.OK;
				LogIsFilePresent.Info( string.Format( "File for song {0} {1} found (status {2})", fileUrl, isFilePresent.Value ? "is" : "isn't", statusCode ) );
			}
			catch ( Exception ex ) {
                LogIsFilePresent.Warn( "IsFilePresentNow (url: " + fileUrl + ") " + ex.ToString(), ex );
			}

			return isFilePresent;
		}
		private static bool? _GetFilePresentFlag( string pksId )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"SELECT file_present FROM songs WHERE pksid = @pksid";
				command.Parameters.AddWithValue( "@pksid", pksId );
				using ( var reader = command.ExecuteReader() ) {
					if ( reader.Read() )
						return reader.IsDBNull( 0 ) ? (bool?)null : reader.GetBoolean( 0 );
				}
			}
			return false;
		}
		private static void _SetFilePresentFlag( string pksId, bool isFilePresent )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"UPDATE songs SET file_present = @file_present WHERE pksid = @pksid";
				command.Parameters.AddWithValue( "@pksid", pksId );
				command.Parameters.AddWithValue( "@file_present", isFilePresent );
				command.ExecuteNonQuery();
			}
		}
		#endregion
	}
}