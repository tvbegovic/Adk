using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseCommon;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class InvestmentTrend : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public IEnumerable<LineChartModel> InvestmentTrendLineChartData { get; set; }
		public InvestmentTrend( string userId, GroupingValue value, string industryId, Media media, BrandOrAdvertiser shareBy, List<Guid> categories,  int limit, string marketId)
			: base( userId )
		{
            _GroupingValue = value;
			InvestmentTrendLineChartData = new List<LineChartModel>();

			using ( var conn = Database.Get() ) {

				string marketFilter = "";
				if ( !String.IsNullOrWhiteSpace( marketId ) && marketId.ToLower() != "all" ) {
					var marketChannels = MarketChannels.GetMarketChannels( marketId ).Select( mc => mc.ChannelId );
					if ( marketChannels.Any() ) {
						marketFilter = String.Format( "AND c.channel_id {0}", Database.InClause( marketChannels ) );
					}
					else {
						return; //no channels no data to display
					}
				}

				string mediaFilter = "";
				if ( media != Media.All ) {
					mediaFilter = String.Format( "AND media_type = '{0}'",
						media == Media.Radio ? "Radio" : "TV" );
				}

				var cmd = conn.CreateCommand();
				string industryFilter = "";
				if ( industryId != "All" ) {
					industryFilter = "AND c.industry_id = @industry";
					cmd.Parameters.AddWithValue( "@industry", industryId );
				}

				string categoriesFilter = "";
				if ( categories != null && categories.Any() ) {
					categoriesFilter = String.Format( "AND c.category_id {0}", Database.InClause( categories ) );
				}

				if ( shareBy == BrandOrAdvertiser.Brand ) {
					cmd.CommandText = string.Format( @"
                    SELECT b.brand_name, SUM(c.{0}) as total, Year(c.play_date) as play_year, MONTH(c.play_date) as play_month
                    FROM report_base_cache c
                    JOIN brands as b on c.brand_id = b.id
                    WHERE 
                        c.play_date >= @start AND c.play_date < @end
					     {1} {2} {3} {4}
                    Group By b.brand_name, play_year, play_month
				    ", _valueColumn(), industryFilter, mediaFilter, categoriesFilter, marketFilter );
				}
				else {
					cmd.CommandText = string.Format( @"
                    SELECT a.company_name, SUM(c.{0}) as total, Year(c.play_date) as play_year, MONTH(c.play_date) as play_month
                    FROM report_base_cache c
                    JOIN advertisers as a on c.advertiser_id = a.id
                    WHERE 
                        c.play_date >= @start AND c.play_date < @end
					     {1} {2} {3} {4}
                    Group By a.company_name, play_year, play_month
				    ", _valueColumn(), industryFilter, mediaFilter, categoriesFilter, marketFilter );
				}

                var dateTo = new DateTime( DateTime.Now.Year, DateTime.Now.Month, 1 );
				var dateFrom = new DateTime( DateTime.Now.Year - 1, DateTime.Now.Month, 1 );

				cmd.Parameters.AddWithValue( "@start", dateFrom );
				cmd.Parameters.AddWithValue( "@end", dateTo );

                Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}';
                    {2}
                ", dateFrom, dateTo, cmd.CommandText));

                var values = new Dictionary<string, LineChartModel>();

				using ( var dr = cmd.ExecuteReader() ) {
					//Every record is sum data by brand
					while ( dr.Read() ) {
						string name = dr.GetString( 0 );
						decimal monthTotal = dr.IsDBNull( 1 ) ? 0 : dr.GetDecimal( 1 );
						int playYear = dr.IsDBNull( 2 ) ? 0 : dr.GetInt32( 2 );
						int playMonth = dr.IsDBNull( 3 ) ? 0 : dr.GetInt32( 3 );
						DateTime playDate = new DateTime( playYear, playMonth, 1 );

						if ( !values.ContainsKey( name ) ) {
							values[name] = new LineChartModel( name );
						}

						var val = values[name];
						val.Total += monthTotal;
						val.Values.Add( new PointValue {
							Key = playDate.ToString( "yyy-MM-dd" ),
							Value = monthTotal,
							Date = playDate
						} );

					}
				}

				InvestmentTrendLineChartData = values.OrderByDescending( v => v.Value.Total ).Take( limit ).Select( v => v.Value );

				//Add empty values for non exisitng months
				while ( dateFrom < dateTo ) {
					foreach ( var data in InvestmentTrendLineChartData ) {
						if ( !data.Values.Any( v => v.Date.Month == dateFrom.Month ) ) {
							data.Values.Add( new PointValue() {
								Key = dateFrom.ToString( "yyy-MM-dd" ),
								Value = 0,
								Date = dateFrom
							} );
						}

						data.Values = data.Values.OrderBy( bv => bv.Date ).ToList();
					}
					dateFrom = dateFrom.AddMonths( 1 );
				}

			}
		}

	}
}