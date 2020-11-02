using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseCommon;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using MySql.Data.MySqlClient;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class AdvertisingLogsByBrand : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public class AdvertisingLogDataModel
		{
			public Guid ChannelId;
			public string ChannelName;
			public string PlayTime;
			public string Name;
			public string SongTitle;
			public decimal AirTime;
			public string SongUrl;
			public decimal SongDuration;
		}

		public class AdvertisingLog
		{
			public string ChannelName;
			public List<AdvertisingLogDataModel> Logs;
		}


		public IEnumerable<AdvertisingLog> AdvertisingLogs { get; set; }

		private List<AdvertisingLogDataModel> _AdvertisingLogRows { get; set; }
        public AdvertisingLogsByBrand(string userId, Guid channelId, IncludeSet include, string industryId, Media media, DateTime date, List<Guid> categories, BrandOrAdvertiser brandOrAdvertiser)
			: base( userId, channelId, include )
		{
			_AdvertisingLogRows = new List<AdvertisingLogDataModel>();

			using ( var conn = Database.Get() ) {
				_getAdvertisingLogs( conn, media, industryId, date, categories, brandOrAdvertiser );
			}

			AdvertisingLogs = _AdvertisingLogRows
									  .GroupBy( ar => ar.ChannelName )
									  .Select( i => new AdvertisingLog {
										  ChannelName = i.Key,
										  Logs = i.ToList()
									  } );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="conn"></param>
		private void _getAdvertisingLogs( MySqlConnection conn, Media media, string industryId, DateTime date, List<Guid> categories, BrandOrAdvertiser brandOrAdvertiser )
		{
			string mediaFilter = "";
			if ( media != Media.All ) {
				mediaFilter = String.Format( "AND media_type = '{0}'",
					 media == Media.Radio ? "Radio" : "TV" );
			}

			var cmd = conn.CreateCommand();

			string industryFilter = "";
			if ( industryId != "All" ) {
				industryFilter = "AND brands.industry_id = @industry";
				cmd.Parameters.AddWithValue( "@industry", industryId );
			}

			string categoriesFilter = "";
			if ( categories != null && categories.Any() ) {
				categoriesFilter = String.Format( "AND songs.category_id {0}", Database.InClause( categories ) );
			}

            if(brandOrAdvertiser == BrandOrAdvertiser.Brand) {
                cmd.CommandText = string.Format(
					 @"SELECT matches.channel_id, channels.station_name, matches.match_occurred, brands.brand_name, songs.title, MAX(matches.match_end), songs.pksid, songs.duration
					{0}
               INNER JOIN brands ON songs.brand_id = brands.id
                    WHERE DATE(matches.match_occurred) = DATE(@date) AND {1}
                    AND matches.channel_id {2} {3} {4} {5}
               GROUP BY matches.channel_id, channels.station_name, songs.brand_id, songs.brand, matches.match_occurred
               ORDER BY channels.station_name ASC, matches.match_occurred ASC", DatabaseConstants.MatchesTableRequiredJoins,DatabaseConstants.MatchesTableRequiredWherePart, Database.InClause(_ChannelIds), mediaFilter, industryFilter, categoriesFilter);

            }
             else
            {
                cmd.CommandText = string.Format(
                @"SELECT matches.channel_id, channels.station_name, matches.match_occurred, advertisers.company_name, songs.title, MAX(matches.match_end), songs.pksid, songs.duration
					{0}
               INNER JOIN brands ON songs.brand_id = brands.id
               INNER JOIN advertisers ON brands.advertiser_id = advertisers.id
                    WHERE DATE(matches.match_occurred) = DATE(@date) AND {1}
                    AND matches.channel_id {2} {3} {4} {5}
               GROUP BY matches.channel_id, channels.station_name, advertisers.id, advertisers.company_name, matches.match_occurred
               ORDER BY channels.station_name ASC, matches.match_occurred ASC", DatabaseConstants.MatchesTableRequiredJoins, DatabaseConstants.MatchesTableRequiredWherePart, Database.InClause( _ChannelIds ), mediaFilter, industryFilter, categoriesFilter );
            }

			cmd.Parameters.AddWithValue( "@date", date );

            Log.Info(string.Format(@"
                    SET @date:='{0:yyyy-MM-dd}'; 
                    {1}
                ", date, cmd.CommandText));

            using ( var reader = cmd.ExecuteReader() ) {
				while ( reader.Read() ) {
					var row = new AdvertisingLogDataModel();
					if ( !reader.IsDBNull( 1 ) ) {
						row.ChannelId = reader.IsDBNull( 0 ) ? Guid.Empty : reader.GetGuid( 0 );
						row.ChannelName = reader.IsDBNull( 1 ) ? String.Empty : reader.GetString( 1 );
						row.PlayTime = reader.IsDBNull( 2 ) ? String.Empty : reader.GetDateTime( 2 ).ToString( "HH:mm:ss" );
						row.Name = reader.IsDBNull( 3 ) ? String.Empty : reader.GetString( 3 );
						row.SongTitle = reader.IsDBNull( 4 ) ? String.Empty : reader.GetString( 4 );
						row.AirTime = reader.IsDBNull( 5 ) ? 0 : reader.GetDecimal( 5 );
						row.SongUrl = reader.IsDBNull( 6 ) ? null : Song.GetMp3Url( reader.GetString( 6 ) );
						row.SongDuration = reader.GetDecimalOrDefault( 7 );

						_AdvertisingLogRows.Add( row );
					}
				}
			}
		}
	}
}