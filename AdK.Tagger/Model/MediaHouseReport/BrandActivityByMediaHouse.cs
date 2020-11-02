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

	public class MediaHouseActivityValue
	{
		public MediaHouseActivityValue( Guid id, string name, decimal value )
		{
			Id = id;
			Name = name;
			Value = value;
		}

		public Guid Id { get; set; }
		public string Name { get; set; }
		public decimal Value { get; set; }
	}


	public class BrandMediaHouseActivity
	{
		public Guid MediaHouse { get; set; }

		/// <summary>
		/// key property is needed by nvD3 library to draw chart (*reason for bad naming convention)
		/// </summary>
		public string key { get; set; }

		/// <summary>
		/// values property is needed by nvD3 library to draw chart (*reason for bad naming convention)
		/// </summary>
		public List<MediaHouseActivityValue> values { get; set; }

		public BrandMediaHouseActivity( Guid mediaHouseId, string mediaHouseName )
		{
			MediaHouse = mediaHouseId;
			key = mediaHouseName;
			values = new List<MediaHouseActivityValue>();
		}
	}

	public class BrandActivityByMediaHouse : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public List<BrandMediaHouseActivity> ChartData { get; set; }
		public List<BrandMediaHouseActivity> PercentageChartData { get; set; }

        public decimal MaxTotalValue { get; set; }
        public decimal MaxPercentageValue { get; set; }
        public decimal TotalChartValue { get; set; }

		public BrandActivityByMediaHouse( string userId, GroupingValue value, PeriodInfo period, string industryId, BrandOrAdvertiser groupBy, List<Guid> categories, string marketId, int limit )
			: base( userId, value, period )
		{
			ChartData = new List<BrandMediaHouseActivity>();
			PercentageChartData = new List<BrandMediaHouseActivity>();

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

				if ( groupBy == BrandOrAdvertiser.Brand ) {
					cmd.CommandText = string.Format( @"
				    SELECT C.brand_id, B.brand_name, 
                              C.channel_id,
                              CH.external_id as channel_name,
                              SUM(IFNULL({0}, 0)) AS total
                        FROM report_base_cache C
                        LEFT JOIN brands B on C.brand_id = B.id
                        LEFT JOIN channels CH on C.channel_id = CH.id
                        WHERE
                              C.play_date >= @start AND C.play_date < @end {1} {2} {3}
                        GROUP BY C.brand_id, B.brand_name, C.channel_id, CH.external_id
				", _valueColumn(), industryFilter, categoriesFilter, marketFilter );
				}
				else {
					cmd.CommandText = string.Format( @"
				    SELECT C.brand_id, A.company_name, 
                              C.channel_id,
                              CH.external_id as channel_name,
                              SUM(IFNULL({0}, 0)) AS total
                        FROM report_base_cache C
                        LEFT JOIN advertisers A on C.advertiser_id = A.id
                        LEFT JOIN channels CH on C.channel_id = CH.id
                        WHERE
                              C.play_date >= @start AND C.play_date < @end {1} {2} {3}
                        GROUP BY C.brand_id, A.company_name, C.channel_id, CH.external_id
				", _valueColumn(), industryFilter, categoriesFilter, marketFilter );
				}

				cmd.Parameters.AddWithValue( "@start", _Period.CurrentStart.Date );
				cmd.Parameters.AddWithValue( "@end", _Period.CurrentEnd.Date );

                Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}';
                    {2}
                ", _Period.CurrentStart.Date, _Period.CurrentEnd.Date, cmd.CommandText));


                var allTotalValues = new Dictionary<Guid, MediaHouseActivityValue>();
				var mediaHouseValue = new Dictionary<Guid, List<MediaHouseActivityValue>>();

				using ( var dr = cmd.ExecuteReader() ) {
					while ( dr.Read() ) {
						Guid id = dr.GetGuid( 0 );
						var name = dr.GetString( 1 );
						var mediaHouseId = dr.GetGuid( 2 );
						var mediaHouseName = dr.GetString( 3 );
						var total = dr.GetDecimal( 4 );


						if ( allTotalValues.ContainsKey( id ) ) {
							allTotalValues[id].Value += total;
						}
						else {
							allTotalValues[id] = new MediaHouseActivityValue( id, name, total );
						}

						var mediaHouseActivity = ChartData.FirstOrDefault( c => c.MediaHouse == mediaHouseId );
						if ( mediaHouseActivity == null ) {
							mediaHouseActivity = new BrandMediaHouseActivity( mediaHouseId, mediaHouseName );
							ChartData.Add( mediaHouseActivity );
							mediaHouseValue.Add( mediaHouseId, new List<MediaHouseActivityValue>() );
						}

						mediaHouseValue[mediaHouseId].Add( new MediaHouseActivityValue( id, name, total ) );

						TotalChartValue += total;
					}
				}

                TotalChartValue = TotalChartValue == 0 ? 1 : TotalChartValue;

                MaxTotalValue = allTotalValues.Any() ? allTotalValues.Max(b => b.Value.Value) : 0;

                var orderedAllBrandsTotalValues = allTotalValues.ToList().OrderByDescending( b => b.Value.Value ).Take( limit );

				foreach ( var mediaHouseActivity in ChartData ) {
					var percentageChannelData = new BrandMediaHouseActivity( mediaHouseActivity.MediaHouse, mediaHouseActivity.key );

					foreach ( var brand in orderedAllBrandsTotalValues ) {
						var val = mediaHouseValue[mediaHouseActivity.MediaHouse].FirstOrDefault( b => b.Id == brand.Key );
						if ( val == null ) {
							val = new MediaHouseActivityValue( brand.Key, brand.Value.Name, 0 );
						}

						decimal percentage = (val.Value / TotalChartValue) * 100;

                        MaxPercentageValue = percentage > MaxPercentageValue ? percentage : MaxPercentageValue;

                        percentageChannelData.values.Add( new MediaHouseActivityValue( val.Id, val.Name, percentage ) );

						mediaHouseActivity.values.Add( val );
					}

					PercentageChartData.Add( percentageChannelData );

				}

			}
		}

	}
}