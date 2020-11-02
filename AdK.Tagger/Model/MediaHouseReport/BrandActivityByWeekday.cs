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
	public class BrandWeekdayActivity
	{
		public BrandWeekdayActivity( int weekday, string dayName )
		{
			Weekday = weekday;
			key = dayName;
			values = new List<BrandValue>();
		}

		public int Weekday { get; set; }

		/// <summary>
		/// key property is needed by nvD3 library to draw chart (*reason for bad naming convention)
		/// </summary>
		public string key { get; set; }

		/// <summary>
		/// values property is needed by nvD3 library to draw chart (*reason for bad naming convention)
		/// </summary>
		public List<BrandValue> values { get; set; }


	}

	public class BrandActivityByWeekday : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public List<BrandWeekdayActivity> ChartData { get; set; }
		public List<BrandWeekdayActivity> PercentageChartData { get; set; }

        public decimal MaxTotalValue { get; set; }
        public decimal MaxPercentageValue { get; set; }
        public decimal TotalChartValue { get; set; }

		public BrandActivityByWeekday( string userId, GroupingValue value, PeriodInfo period, string industryId, List<Guid> categories, string marketId, int limit )
			: base( userId, value, period )
		{
			ChartData = new List<BrandWeekdayActivity>();
			PercentageChartData = new List<BrandWeekdayActivity>();

            MaxPercentageValue = 0;


            using ( var conn = Database.Get() ) {
				var cmd = conn.CreateCommand();

				string marketFilter = "";
				if ( !String.IsNullOrWhiteSpace( marketId ) && marketId.ToLower() != "all" ) {
					var marketChannels = MarketChannels.GetMarketChannels( marketId ).Select( mc => mc.ChannelId );
					if ( marketChannels.Any() ) {
						marketFilter = String.Format( "AND C.channel_id {0}", Database.InClause( marketChannels ) );
					}
					else {
						return; //no channels no data to display
					}
				}

				string industryFilter = "";
				if ( industryId != "All" ) {
					industryFilter = "AND C.industry_id = @industry";
					cmd.Parameters.AddWithValue( "@industry", industryId );
				}

				string categoriesFilter = "";
				if ( categories != null && categories.Any() ) {
					categoriesFilter = String.Format( "AND C.category_id {0}", Database.InClause( categories ) );
				}

				cmd.CommandText = string.Format( @"
				SELECT C.brand_id, B.brand_name, 
                              DAYOFWEEK(C.play_date) - 1 AS weekday, 
                              DAYNAME(C.play_date) as dayname,
                              SUM(IFNULL({0}, 0)) AS total
                        FROM report_base_cache C
                        LEFT JOIN brands B on C.brand_id = B.id
                        WHERE
                              C.play_date >= @start AND C.play_date < @end {1} {2} {3}
                        GROUP BY C.brand_id, B.brand_name, DAYOFWEEK(C.play_date) - 1, DAYNAME(C.play_date)
				", _valueColumn(), industryFilter, categoriesFilter, marketFilter );

				cmd.Parameters.AddWithValue( "@start", _Period.CurrentStart.Date );
				cmd.Parameters.AddWithValue( "@end", _Period.CurrentEnd.Date );

                Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}';
                    {2}
                ", _Period.CurrentStart.Date, _Period.CurrentEnd.Date, cmd.CommandText));

                var allBrandsTotalValues = new Dictionary<Guid, BrandValue>();
				var weekdayBrandValues = new Dictionary<int, List<BrandValue>>();

				using ( var dr = cmd.ExecuteReader() ) {
					while ( dr.Read() ) {
						Guid brandId = dr.GetGuid( 0 );
						var brandName = dr.GetString( 1 );
						var weekday = dr.GetInt32( 2 );
						var dayName = dr.GetString( 3 );
						var total = dr.GetDecimal( 4 );


						if ( allBrandsTotalValues.ContainsKey( brandId ) ) {
							allBrandsTotalValues[brandId].Value += total;
						}
						else {
							allBrandsTotalValues[brandId] = new BrandValue( brandId, brandName, total );
						}

						var weekdayActivity = ChartData.FirstOrDefault( c => c.Weekday == weekday );
						if ( weekdayActivity == null ) {
							weekdayActivity = new BrandWeekdayActivity( weekday, dayName );
							ChartData.Add( weekdayActivity );
							weekdayBrandValues.Add( weekday, new List<BrandValue>() );
						}

						weekdayBrandValues[weekday].Add( new BrandValue( brandId, brandName, total ) );


						TotalChartValue += total;
					}
				}

                TotalChartValue = TotalChartValue == 0 ? 1 : TotalChartValue;

                MaxTotalValue = allBrandsTotalValues.Any() ? allBrandsTotalValues.Max(b => b.Value.Value) : 0;


                var orderedAllBrandsTotalValues = allBrandsTotalValues.ToList().OrderByDescending( b => b.Value.Value ).Take( limit );

				foreach ( var weekdayActivity in ChartData ) {
					var percentageChannelData = new BrandWeekdayActivity( weekdayActivity.Weekday, weekdayActivity.key );


					foreach ( var brand in orderedAllBrandsTotalValues ) {
						var brandValue = weekdayBrandValues[weekdayActivity.Weekday].FirstOrDefault( b => b.BrandId == brand.Key );
						if ( brandValue == null ) {
							brandValue = new BrandValue( brand.Key, brand.Value.BrandName, 0 );
						}

						weekdayActivity.values.Add( brandValue );

						decimal percentage = (brandValue.Value / TotalChartValue) * 100;

                        MaxPercentageValue = percentage > MaxPercentageValue ? percentage : MaxPercentageValue;

                        percentageChannelData.values.Add( new BrandValue( brandValue.BrandId, brandValue.BrandName, percentage ) );
					}

					PercentageChartData.Add( percentageChannelData );

				}

			}
		}

	}
}