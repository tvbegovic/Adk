using System.Runtime.CompilerServices;
using System.Web.Caching;
using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
    public enum SongStatus
	{
		New = 0,
		Processed = 1,
		Mailed = 3,
		Uploaded = 2
	}

	public class Song
	{
		public Guid Id = Guid.Empty;
		public string MasterSongId;
		public string UserId;
		public string PksId;
		public string Filename;
		public decimal Duration; // ms
		public string Title;
		public string Performer; // Also known as Advertiser for spots
		public string Album; // Also known as Category for spots
		public string Year;
		public string Region;
		public string Role;
		public string MediaLink;
		public bool SuppressChart;
		public bool Deleted;
		public byte Status;
		public long AlreadyImported;
		public DateTime? Created;
		public DateTime? Modified;
		public DateTime? ScannedToDate;
		public Guid ProductId;
		public string Brand;
		public Guid BrandId;
		public Guid CategoryId;
		public string ProductTemp;
		public string MessageType;
		public string Campaign;
		public DateTime ScanExpiresOn;
		public bool FilePresent;

		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private static string[] _videoFormats = { ".flv", "flv", ".webm", "webm", ".mp4", "mp4" };

		public void Create()
		{
			if ( Id == Guid.Empty ) {
				Id = Guid.NewGuid();
				Created = DateTime.Now;
				Database.Insert( @"
				INSERT INTO songs (id, user_id, pksid, filename, duration, title, brand, album, performer, created)
				VALUES (@id, @user_id, @pksid, @filename, @duration, @title, @brand, @album, @performer, @created)",
					"@id", Id,
					"@user_id", UserId,
					"@pksid", PksId,
					"@filename", Filename,
					"@duration", Duration,
					"@title", Title,
					"@brand", Brand,
					"@album", Album,
					"@performer", Performer,
					"@created", Created );
			}
		}

		public static void UpdateDurationAndStatus( string pksId, decimal duration, SongStatus status )
		{
			Database.ExecuteNonQuery(
				"UPDATE songs SET duration = @duration, status = @status WHERE pksid = @pksid",
				"@pksid", pksId,
				"@status", (int)status,
				"@duration", duration );
		}

		public static void UpdateDescription( Guid id, string title, string brand, string album, string performer )
		{
			Log.Info( string.Format( "Updating description of spot ID {0}, {1}, {2}, {3}, {4}", id, title, brand, album, performer ) );
			Database.ExecuteNonQuery(
				"UPDATE songs SET title = @title, brand = @brand, album = @album, performer = @performer WHERE id = @id",
				"@id", id,
				"@title", title,
				"@brand", brand,
				"@album", album,
				"@performer", performer );
		}

		public static void DeleteLogically( Guid id )
		{
			Log.Info( "Logical delete of spot ID " + id );
			Database.ExecuteNonQuery(
				"UPDATE songs SET deleted = 1 WHERE id = @id",
				"@id", id );
		}

		public static void UpdateStatus( Guid id, SongStatus status )
		{
			Log.Info( "Logical delete of spot ID " + id );
			Database.ExecuteNonQuery(
				"UPDATE songs SET status = @status WHERE id = @id",
				"@id", id, "@status", (int)status );
		}

		public static void SetUploadedStatusToUserScannedSpots( string userId )
		{
			Database.ExecuteNonQuery(
				"UPDATE songs SET status = @status WHERE user_id = @userId AND status != @status AND scanned_to_date IS NOT NULL",
				"@userId", userId, "@status", (int)SongStatus.Uploaded );
		}

		public static List<Song> GetUserSongs( TaggerUser user,
			int pageSize, int pageNum,
			string sortColumn, bool ascending,
			List<Guid> channelIds,
			DateTime? dateFrom,
			DateTime? dateTo,
			string nameFilter,
			string brandFilter,
			string categoryFilter,
			string advertiserFilter,
			List<SongStatus> songStatuses,
			out int totalCount )
		{
			string select = @"
			SELECT {0}
			FROM songs
			WHERE user_id = @user_id AND deleted = 0
			{1} # filters
			{2} # ordering
			{3} # paging";

			var parameters = new List<object>
			{
				"@user_id", user.Id
			};

			Action<string, object> addParam = ( name, value ) => {
				parameters.Add( name );
				parameters.Add( value );
			};

			Action<string, string> addFilterParam = ( name, filter ) => {
				addParam( name, string.Format( "%{0}%", filter.Trim() ) );
			};

			#region Filters
			string filters = "";
			if ( channelIds != null && channelIds.Any() ) {
				filters += "AND EXISTS (SELECT * FROM report_base_cache c WHERE c.song_id = id AND c.channel_id " + Database.InClause( channelIds );
				if ( dateFrom.HasValue ) {
					filters += " AND play_date >= @dateFrom";
					addParam( "@dateFrom", dateFrom.Value.ToString( "yyyy-MM-dd" ) );
				}
				if ( dateTo.HasValue ) {
					filters += " AND play_date <= @dateTo";
					addParam( "@dateTo", dateTo.Value.ToString( "yyyy-MM-dd" ) );
				}
				filters += ")\n";
			}

			if ( !string.IsNullOrWhiteSpace( nameFilter ) ) {
				filters += "AND title LIKE @nameFilter\n";
				addFilterParam( "@nameFilter", nameFilter );
			}

			if ( !string.IsNullOrWhiteSpace( brandFilter ) ) {
				filters += "AND brand LIKE @brandFilter\n";
				addFilterParam( "@brandFilter", brandFilter );
			}

			if ( !string.IsNullOrWhiteSpace( categoryFilter ) ) {
				filters += "AND album LIKE @categoryFilter\n";
				addFilterParam( "@categoryFilter", categoryFilter );
			}

			if ( !string.IsNullOrWhiteSpace( advertiserFilter ) ) {
				filters += "AND performer LIKE @advertiserFilter\n";
				addFilterParam( "@advertiserFilter", advertiserFilter );
			}

			if ( songStatuses != null && songStatuses.Any() ) {
				filters = String.Format( "{0} AND (", filters );
				var or = "";
				foreach ( var status in songStatuses ) {
					filters = String.Format( "{0} {1} status = {2}", filters, or, (int)status );
					or = "OR";
				}

				filters = String.Format( "{0} )", filters );

			}

			#endregion

			string selectCount = string.Format( select, "COUNT(id)", filters, "", "" );

			totalCount = Database.Count( selectCount, parameters.ToArray() );

			#region Order by
			string orderBy;
			switch ( sortColumn ) {
				case "name":
					orderBy = "title";
					break;
				case "brand":
					orderBy = "brand";
					break;
				case "category":
					orderBy = "album";
					break;
				case "advertiser":
					orderBy = "performer";
					break;
				case "duration":
					orderBy = "duration";
					break;
				default:
					orderBy = "created";
					break;
			}
			if ( !ascending )
				orderBy += " DESC";
			#endregion

			string selectSpots = string.Format( select, "id, pksid, filename, duration, title, brand, album, performer, created, scanned_to_date, status", filters, "ORDER BY " + orderBy, "LIMIT @offset, @max" );
			addParam( "@offset", pageSize * pageNum );
			addParam( "@max", pageSize );

			return Database.ListFetcher( selectSpots,
				dr => new Song {
					Id = dr.GetGuid( 0 ),
					PksId = dr.GetString( 1 ),
					Filename = dr.GetNullableString( 2 ),
					Duration = dr.GetDecimal( 3 ),
					Title = dr.GetNullableString( 4 ),
					Brand = dr.GetNullableString( 5 ),
					Album = dr.GetNullableString( 6 ),
					Performer = dr.GetNullableString( 7 ),
					Created = dr.IsDBNull( 8 ) ? (DateTime?)null : dr.GetDateTime( 8 ),
					ScannedToDate = dr.GetDateOrNull( 9 ),
                    Status = dr.GetByte(10),
					UserId = user.Id
				},
				parameters.ToArray() );
		}

		public static List<Service.AjaxSpot> GetUserSpots( TaggerUser user, string termFilter, bool onlyUploaded = false )
		{
			string scannedOnlyFilter = "";
			if ( onlyUploaded ) {
				scannedOnlyFilter = String.Format( "AND (status = {0} || status = {1})", (int)SongStatus.Uploaded, (int)SongStatus.Mailed );
			}

			var query = String.Format( @"SELECT id, pksid, filename, duration, title, brand, created
							FROM songs
							WHERE user_id = @user_id AND deleted = 0 {0}", scannedOnlyFilter );

			var parameters = new List<object>
			{
				"@user_id", user.Id
			};
			Action<string, object> addParam = ( name, value ) => {
				parameters.Add( name );
				parameters.Add( value );
			};

			if ( !String.IsNullOrWhiteSpace( termFilter ) ) {

				string filters = String.Format( @"AND CASE WHEN NULLIF(title, '') IS NULL
						THEN filename LIKE @term
						ELSE title LIKE @term
						END" );

				query = String.Format( "{0} {1}", query, filters );

				addParam( "@term", string.Format( "%{0}%", termFilter.Trim() ) );
			}

			return Database.ListFetcher( query,
				dr => new Service.AjaxSpot() {
					Guid = dr.GetString( 0 ),
					Filename = dr.GetNullableString( 2 ),
					FileUrl = GetMp3Url( dr.GetString( 1 ) ),
					Duration = dr.GetDecimal( 3 ),
					Name = dr.GetNullableString( 4 ),
					Brand = dr.GetNullableString( 5 ),
					Created = dr.IsDBNull( 6 ) ? "" : dr.GetDateTime( 6 ).ToString( "s" ),
				}, parameters.ToArray() );
		}

		public static List<string> SuggestBrands( string userId, string search, int max )
		{
			return Database.ListFetcher(
				"SELECT DISTINCT(brand) FROM songs WHERE user_id = @user_id AND brand LIKE @search ORDER BY brand LIMIT @max",
				dr => dr.GetString( 0 ),
				"@user_id", userId,
				"@search", string.Format( "%{0}%", search.Trim() ),
				"@max", max
			);
		}
		public static List<string> SuggestCategories( string userId, string search, int max )
		{
			return Database.ListFetcher(
				"SELECT DISTINCT(album) FROM songs WHERE user_id = @user_id AND album LIKE @search ORDER BY album LIMIT @max",
				dr => dr.GetString( 0 ),
				"@user_id", userId,
				"@search", string.Format( "%{0}%", search.Trim() ),
				"@max", max
			);
		}
		public static List<string> SuggestAdvertisers( string userId, string search, int max )
		{
			return Database.ListFetcher(
				"SELECT DISTINCT(performer) FROM songs WHERE user_id = @user_id AND performer LIKE @search ORDER BY performer LIMIT @max",
				dr => dr.GetString( 0 ),
				"@user_id", userId,
				"@search", string.Format( "%{0}%", search.Trim() ),
				"@max", max
			);
		}

		public string GetMp3Url()
		{
			return GetMp3Url( this.PksId, this.UserId );
		}

		public static string GetMp3Url( string pksId, string userId = null )
		{
			if ( String.IsNullOrWhiteSpace( pksId ) ) return string.Empty;

			string sampleRootUrl = Settings.Get( "Song", "SampleRootUrl" );
			if ( string.IsNullOrWhiteSpace( sampleRootUrl ) )
				throw new ApplicationException( "Missing Song - SampleRootUrl setting in DB" );

			var url = new Uri( sampleRootUrl, UriKind.Absolute );
			url = new Uri( url, ".." );

			//For demo, pksid = b274300c461a40859a6b7b4580725628.mp3
			pksId = "b274300c461a40859a6b7b4580725628";

			return userId != null
					? String.Format( "{0}{1}/{2}.mp3", url.OriginalString, userId.Replace( "-", "" ), pksId )
					: String.Format( "{0}{1}.mp3", sampleRootUrl, pksId );
		}

        public static string GetVideoUrl(string pksId, string userId = null)
        {
            if (String.IsNullOrWhiteSpace(pksId)) return string.Empty;

            string sampleRootUrl = Settings.Get("Song", "SampleRootUrl");
            if (string.IsNullOrWhiteSpace(sampleRootUrl))
                throw new ApplicationException("Missing Song - SampleRootUrl setting in DB");

            var formats = Database.ItemFetcher<string>(
                "SELECT mc.media_types FROM songs as s JOIN song_media_clips as mc ON s.id = mc.song_id WHERE s.pksid = @pksId",
                dr => dr.GetString(0),
                "@pksId", pksId
            );

            string existingFormat = null;
            if (formats != null)
            {
                foreach (var format in _videoFormats)
                {
                    if (formats.Contains(format))
                        existingFormat = format.Substring(0, 1) == "." ? format : "." + format;
                }
            }

            if (existingFormat != null)
            {
                var url = new Uri(sampleRootUrl, UriKind.Absolute);
                url = new Uri(url, "..");

				//DEMO
				pksId = "c5691ff8e2ae413fa206d0d4074fd26d";

                return userId != null
                    ? String.Format("{0}{1}/{2}" + existingFormat, url.OriginalString, userId.Replace("-", ""), pksId)
                    : String.Format("{0}{1}" + existingFormat, sampleRootUrl, pksId);
            }
            else
            {
                return null;
            }
            
        }

		public static List<Song> GetByIds(IList<Guid> ids)
		{
			return Database.ListFetcher( "SELECT id, title FROM songs WHERE id " + Database.InClause(ids) , dr =>
				new Song {
					Id = dr.GetGuid( 0 ),
					Title = dr.GetString( 1 )
				} );
		}

    }
}
