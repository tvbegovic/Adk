using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using AdK.Tagger.Model.Reporting;
using DatabaseCommon;
using MySql.Data.MySqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class PivotResultModel
	{
		public string StationName;
		public decimal Jan;
		public decimal Feb;
		public decimal Mar;
		public decimal Apr;
		public decimal May;
		public decimal Jun;
		public decimal Jul;
		public decimal Aug;
		public decimal Sep;
		public decimal Oct;
		public decimal Nov;
		public decimal Dec;
		public int Year;
		public decimal Total;
	}

	public class MarketTrend
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public string UserId { get; set; }
		public List<LineChartModel> MarketOverviewTrendList { get; set; }

		public MarketTrend( string userId, GroupingValue value, string mediaType, string marketId )
		{
			using ( var conn = Database.Get() ) {
				_getMonthTrend( conn, value == GroupingValue.Duration ? "duration" : "earns", mediaType, marketId );
			}
		}

		#region 12 Month Trend

		private void _getMonthTrend( MySqlConnection conn, string sumField, string mediaType, string marketId )
		{
			string marketFilter = "";
			if ( !String.IsNullOrWhiteSpace( marketId ) && marketId.ToLower() != "all" ) {
				var marketChannels = MarketChannels.GetMarketChannels( marketId ).Select( mc => mc.ChannelId );
				if ( marketChannels.Any() ) {
					marketFilter = String.Format( "AND channel_id {0}", Database.InClause( marketChannels ) );
				}
				else {
					return; //no channels no data to display
				}
			}

			var cmd = conn.CreateCommand();
			cmd.CommandText = string.Format( @"SELECT channels.station_name,                
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=1 THEN report_base_cache.{0} END) AS 'Jan 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=2 THEN report_base_cache.{0} END) AS 'Feb 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=3 THEN report_base_cache.{0} END) AS 'Mar 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=4 THEN report_base_cache.{0} END) AS 'Apr 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=5 THEN report_base_cache.{0} END) AS 'May 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=6 THEN report_base_cache.{0} END) AS 'Jun 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=7 THEN report_base_cache.{0} END) AS 'Jul 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=8 THEN report_base_cache.{0} END) AS 'Aug 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=9 THEN report_base_cache.{0} END) AS 'Sep 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=10 THEN report_base_cache.{0} END) AS 'Oct 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=11 THEN report_base_cache.{0} END) AS 'Nov 15',
                SUM(CASE WHEN MONTH(report_base_cache.play_date)=12 THEN report_base_cache.{0} END) AS 'Dec 15',
                YEAR(report_base_cache.play_date) AS 'Year'
                FROM channels
                INNER JOIN report_base_cache 
                    ON (report_base_cache.channel_id = channels.id)
                WHERE report_base_cache.play_date >= @start AND report_base_cache.play_date < @end {1}
					AND ((@mediaType IS NULL) OR (@mediaType IS NOT NULL AND report_base_cache.media_type=@mediaType))
                GROUP BY channels.station_name, YEAR(report_base_cache.play_date)
                ORDER BY channels.station_name, YEAR(report_base_cache.play_date)", sumField, marketFilter );

			var start = new DateTime( DateTime.Today.AddMonths( -12 ).Year, DateTime.Today.AddMonths( -12 ).Month, 1 );
			var end = new DateTime( DateTime.Today.Year, DateTime.Today.Month, 1 );
            
			cmd.Parameters.AddWithValue( "@start", start );
			cmd.Parameters.AddWithValue( "@end", end );
			cmd.Parameters.AddWithValue( "@mediaType", mediaType != "" ? mediaType : null );

            Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    SET @mediaType:={2}; 
                    {3}
                ", start, end, mediaType != "" ? "'" + mediaType + "'" : "null", cmd.CommandText));

            MarketOverviewTrendList = new List<LineChartModel>();
			var dbResultList = new List<PivotResultModel>();
			decimal? total = 0;
			using ( var reader = cmd.ExecuteReader() ) {
				while ( reader.Read() ) {
					var res = new PivotResultModel();
					res.StationName = reader.GetString( 0 );
					res.Jan = reader.IsDBNull( 1 ) ? 0 : reader.GetDecimal( 1 );
					res.Feb = reader.IsDBNull( 2 ) ? 0 : reader.GetDecimal( 2 );
					res.Mar = reader.IsDBNull( 3 ) ? 0 : reader.GetDecimal( 3 );
					res.Apr = reader.IsDBNull( 4 ) ? 0 : reader.GetDecimal( 4 );
					res.May = reader.IsDBNull( 5 ) ? 0 : reader.GetDecimal( 5 );
					res.Jun = reader.IsDBNull( 6 ) ? 0 : reader.GetDecimal( 6 );
					res.Jul = reader.IsDBNull( 7 ) ? 0 : reader.GetDecimal( 7 );
					res.Aug = reader.IsDBNull( 8 ) ? 0 : reader.GetDecimal( 8 );
					res.Sep = reader.IsDBNull( 9 ) ? 0 : reader.GetDecimal( 9 );
					res.Oct = reader.IsDBNull( 10 ) ? 0 : reader.GetDecimal( 10 );
					res.Nov = reader.IsDBNull( 11 ) ? 0 : reader.GetDecimal( 11 );
					res.Dec = reader.IsDBNull( 12 ) ? 0 : reader.GetDecimal( 12 );
					res.Year = reader.GetInt16( 13 );

					decimal resAmount = 0;
					for ( int i = 1; i <= 12; i++ ) {
						resAmount += reader.IsDBNull( i ) ? 0 : reader.GetDecimal( i );
					}

					res.Total = resAmount;
					total += resAmount;

					dbResultList.Add( res );
				}
			}

			var allStations = dbResultList.GroupBy( g => g.StationName ).Select( s => new { StationName = s.Key } );

			var chartRange = Enumerable.Range( 0, (end.Year - start.Year) * 12 + (end.Month - start.Month) )
				 .Select( m => new DateTime( start.Year, start.Month, 15 ).AddMonths( m ) ).ToArray();

			var tempOthersList = new List<PointValue>();
			var min = total / 100 * 3;
			foreach ( var station in allStations ) {
				var values = new List<PointValue>();
				var data = new LineChartModel();
				data.Key = station.StationName;
				decimal amount = 0;
				foreach ( var date in chartRange ) {
					var point = new PointValue();
					point.Date = date;
					point.Key = date.ToString( "yyy-MM-dd" );

					var pointValue = dbResultList.FirstOrDefault( x => x.StationName == station.StationName && x.Year == date.Year );
					if ( pointValue != null ) {
						switch ( date.Month ) {
							case 1:
								point.Value = pointValue.Jan;
								amount += point.Value;
								break;
							case 2:
								point.Value = pointValue.Feb;
								amount += point.Value;
								break;
							case 3:
								point.Value = pointValue.Mar;
								amount += point.Value;
								break;
							case 4:
								point.Value = pointValue.Apr;
								amount += point.Value;
								break;
							case 5:
								point.Value = pointValue.May;
								amount += point.Value;
								break;
							case 6:
								point.Value = pointValue.Jun;
								amount += point.Value;
								break;
							case 7:
								point.Value = pointValue.Jul;
								amount += point.Value;
								break;
							case 8:
								point.Value = pointValue.Aug;
								amount += point.Value;
								break;
							case 9:
								point.Value = pointValue.Sep;
								amount += point.Value;
								break;
							case 10:
								point.Value = pointValue.Oct;
								amount += point.Value;
								break;
							case 11:
								point.Value = pointValue.Nov;
								amount += point.Value;
								break;
							case 12:
								point.Value = pointValue.Dec;
								amount += point.Value;
								break;
						}
					}

					values.Add( point );
				}

				data.Total = amount;
				data.Values.AddRange( values );

				if ( amount > min ) {
					MarketOverviewTrendList.Add( data );
				}
				else {
					tempOthersList.AddRange( data.Values );
				}
			}

			LineChartModel allOthers = new LineChartModel();
			allOthers.Key = "Others";
			allOthers.Values = tempOthersList.GroupBy( g => g.Date ).Select( s => new PointValue {
				Key = s.Key.ToString( "yyy-MM-dd" ),
				Value = s.Sum( x => x.Value )
			} ).ToList();

			if ( allOthers.Values.Any() && allOthers.Values.Sum( v => v.Value ) > 0 ) {
				MarketOverviewTrendList.Add( allOthers );
			}


		}

		#endregion
	}
}