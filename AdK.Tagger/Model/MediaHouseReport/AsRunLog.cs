using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using AdK.Tagger.Model.Reporting;
using DatabaseCommon;
using MySql.Data.MySqlClient;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class ReadAllAsRunLogDataModel
	{
        public Guid ChannelId { get; set; }
		public string ChannelName { get; set; }
		public string BrandvertiserName { get; set; }
		public Guid? ProductId { get; set; }
		public Guid? SongId { get; set; }
		public string SongTitle { get; set; }
		public decimal SongDuration { get; set; }
		public string SongUrl { get; set; }
		public DateTime PlayDateTime { get; set; }
		public bool HavePriceDefinition { get; set; }
		public string PlayDate
		{
			get
			{
				return this.PlayDateTime > DateTime.MinValue ? this.PlayDateTime.ToString( "yyyy-MM-dd" ) : string.Empty;
			}
		}
		public string PlayTime
		{
			get
			{ return this.PlayDateTime > DateTime.MinValue ? this.PlayDateTime.ToString( "HH:mm:ss" ) : string.Empty; }
		}
		public decimal AirTime { get; set; }
		public decimal EstimateSpend { get; set; }
	}

	public class AsRunChannelDataModel
	{
		public AsRunChannelDataModel()
		{
			AsRunDetailData = new List<AsRunDetailDataModel>();
		}
		public Guid ChannelId { get; set; }
		public string ChannelName { get; set; }
		public int AdCount { get; set; }
		public decimal EstimateSpend { get; set; }
		public decimal TotalAirTime { get; set; }
		public List<AsRunDetailDataModel> AsRunDetailData { get; set; }
	}

	public class AsRunDetailDataModel
	{
		public Guid? SongId { get; set; }
		public Guid ChannelId { get; set; }
		public Guid? ProductId { get; set; }
		public DateTime PlayDateTime { get; set; }
		public string PlayDate { get; set; }
		public string PlayTime { get; set; }
		public string SongTitle { get; set; }
		public decimal SongDuration { get; set; }
		public string SongUrl { get; set; }
		public string BrandvertiserName { get; set; }
		public int AdCount { get; set; }
		public decimal EstimateSpend { get; set; }
		public decimal TotalAirTime { get; set; }
		public bool IsLeadRow { get; set; }
		public bool HavePriceDefinition { get; set; }

	}


	public class AsRunLog : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public string UserId { get; set; }
		public List<AsRunChannelDataModel> AsRunData { get; set; }

		public AsRunLog( string userId, Guid focusChannelId, IncludeSet include, string brandOrAdvertiserId, BrandOrAdvertiser brandOrAdvertiser, DateTime customDate, bool showDuplicates )
			: base( userId, focusChannelId, include )
		{
			UserId = userId;

			using ( var conn = Database.Get() ) {
				_getAsRunData( conn, brandOrAdvertiserId, brandOrAdvertiser, customDate, showDuplicates );
			}
		}

		/// <summary>
		/// Get As-Run Log data for the selected values
		/// </summary>
		/// <param name="conn"></param>
		private void _getAsRunData( MySqlConnection conn, string brandOrAdvertiserId, BrandOrAdvertiser brandOrAdvertiser, DateTime customDate, bool showDuplicates )
		{
			var cmd = conn.CreateCommand();

			string duplicatesSelect = String.Format( "MAX(songs.duration), MAX({0})", DatabaseConstants.MatchTableEarnsSelect );
			string duplicatesGroupBy = "GROUP BY matches.channel_id, matches.song_id, matches.match_occurred";
			if ( showDuplicates ) {
				duplicatesGroupBy = "";
				duplicatesSelect = String.Format( "songs.duration, {0}", DatabaseConstants.MatchTableEarnsSelectAsEarns );
			}

			bool useCondition = !String.IsNullOrWhiteSpace( brandOrAdvertiserId );
			string filterCondition = useCondition ? (brandOrAdvertiser == BrandOrAdvertiser.Brand ? "AND songs.brand_id = @id" : "AND brands.advertiser_id = @id") : "";

			string select =
				String.Format(@"{0}, channels.id, channels.station_name, songs.id, songs.title, songs.filename, songs.product_id, songs.pksid, songs.duration,
							matches.match_occurred, pricedefs.pps, {1}", brandOrAdvertiser == BrandOrAdvertiser.Brand ? "songs.brand" : "advertisers.company_name", duplicatesSelect );

			if ( brandOrAdvertiser == BrandOrAdvertiser.Brand ) {
				cmd.CommandText = string.Format(
				@"SELECT {0} {1}
                WHERE DATE(matches.match_occurred) = DATE(@date)
						AND {2} {3} AND matches.channel_id {4} {5}
                ORDER BY channels.station_name ASC, matches.match_occurred ASC",
				select, DatabaseConstants.MatchesTableRequiredJoinsWithEarns, DatabaseConstants.MatchesTableRequiredWherePart, filterCondition, Database.InClause( _ChannelIds ), duplicatesGroupBy );
			}
			else {
				cmd.CommandText = string.Format(
				@"SELECT {0} {1}
                INNER JOIN brands on songs.brand_id = brands.id
                INNER JOIN advertisers on brands.advertiser_id = advertisers.id
                WHERE DATE(matches.match_occurred) = DATE(@date)
					 AND {2} {3} AND matches.channel_id {4} {5}
                ORDER BY channels.station_name ASC, matches.match_occurred ASC",
				select, DatabaseConstants.MatchesTableRequiredJoinsWithEarns, DatabaseConstants.MatchesTableRequiredWherePart, filterCondition, Database.InClause( _ChannelIds ), duplicatesGroupBy );

			}

			cmd.Parameters.AddWithValue( "@date", customDate );
			if ( useCondition )
				cmd.Parameters.AddWithValue( "@id", brandOrAdvertiserId );

            Log.Info(string.Format(@"
                    SET @date:='{0:yyyy-MM-dd}'; 
                    SET @id:='{1}';
                    {2}
                ", customDate, brandOrAdvertiserId, cmd.CommandText));


            AsRunData = new List<AsRunChannelDataModel>();
			var dbResultList = new List<ReadAllAsRunLogDataModel>();
			using ( var dr = cmd.ExecuteReader() ) {
				while ( dr.Read() ) {
					var row = new ReadAllAsRunLogDataModel();
					if ( !dr.IsDBNull( 1 ) ) {
						row.BrandvertiserName = dr.GetStringOrDefault( 0 );
						row.ChannelId = dr.GetGuid( 1 );
						row.ChannelName = dr.GetStringOrDefault( 2 );
						row.SongId = dr.GetGuidOrNull( 3 );
						row.SongTitle = dr.IsDBNull( 4 ) ? dr.GetStringOrDefault( 5 ) : dr.GetString( 4 );
						row.ProductId = dr.GetGuidOrNull( 6 );
						row.SongUrl = Song.GetMp3Url( dr.GetStringOrDefault( 7 ) );
						row.SongDuration = dr.GetIntOrDefault(8);
						row.PlayDateTime = dr.GetDateOrDefault( 9 );
						row.HavePriceDefinition = !dr.IsDBNull( 10 );
						row.AirTime = dr.GetDecimalOrDefault( 11 );
						row.EstimateSpend = dr.GetDecimalOrDefault( 12 );

						dbResultList.Add( row );
					}
				}
			}

			var asRunDataList = dbResultList.GroupBy( g => new {
				g.ChannelId,
				g.ChannelName
			} ).Select( s => new AsRunChannelDataModel {
				ChannelId = s.Key.ChannelId,
				ChannelName = s.Key.ChannelName,
				AdCount = 0,
				EstimateSpend = 0,
				TotalAirTime = 0
			} );

			AsRunData.AddRange( asRunDataList );

			foreach ( var asRunData in AsRunData ) {
				var groupByDateList = dbResultList.Where( w => w.ChannelId == asRunData.ChannelId )
					  .GroupBy( g => new DateTime( g.PlayDateTime.Year, g.PlayDateTime.Month, g.PlayDateTime.Day ) )
					  .Select( s => new AsRunDetailDataModel {
						  PlayDateTime = s.Key.Date,
						  PlayDate = s.Key.Date.ToString( "yyyy-MM-dd" ),
						  AdCount = s.Count(),
						  EstimateSpend = s.Sum( x => x.EstimateSpend ),
						  TotalAirTime = s.Sum( x => x.AirTime ),
						  IsLeadRow = true
					  } );

				foreach ( var dateRow in groupByDateList ) {

					var details = dbResultList.Where(
						  w => w.ChannelId == asRunData.ChannelId && w.PlayDateTime.Date == dateRow.PlayDateTime.Date )
						  .Select( s => new AsRunDetailDataModel {
							  ChannelId = s.ChannelId,
							  SongId = s.SongId,
							  PlayDateTime = s.PlayDateTime,
							  PlayDate = s.PlayDate,
							  PlayTime = s.PlayTime,
							  SongTitle = s.SongTitle,
							  BrandvertiserName = s.BrandvertiserName,
							  ProductId = s.ProductId,
							  EstimateSpend = s.EstimateSpend,
							  TotalAirTime = s.AirTime,
							  HavePriceDefinition = s.HavePriceDefinition,
							  SongUrl = s.SongUrl,
							  SongDuration = s.SongDuration
						  } ).ToList();

					asRunData.AsRunDetailData.Add( dateRow );
					asRunData.AsRunDetailData.AddRange( details );

					asRunData.AdCount += dateRow.AdCount;
					asRunData.EstimateSpend += dateRow.EstimateSpend;
					asRunData.TotalAirTime += dateRow.TotalAirTime;
				}
			}
		}
	}
}