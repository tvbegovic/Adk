using AdK.Tagger.Model.Reporting;
using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class ShareByBrandOrAdvertiser : ReportBase
	{
		public IEnumerable<Item> AllMedia;
		public IEnumerable<Item> TvMedia;
		public IEnumerable<Item> RadioMedia;

        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public class Item
		{
			public Guid Id;
			public string Name;
			public decimal Value;
			public string MediaType;
		}

		public ShareByBrandOrAdvertiser( string userId, PeriodInfo period, string industryId, GroupingValue value, BrandOrAdvertiser shareBy, List<Guid> categories, string marketId, int limit )
			: base( userId, value, period )
		{
			var allMedia = new Dictionary<Guid, Item>();
			var tvMedia = new List<Item>();
			var radioMedia = new List<Item>();

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

			using ( var conn = Database.Get() ) {

				var cmd = conn.CreateCommand();

				string industryFilter = "";
				if ( industryId != "All" ) {
					industryFilter = "AND report_base_cache.industry_id = @industry";
					cmd.Parameters.AddWithValue( "@industry", industryId );
				}

				string categoriesFilter = "";
				if ( categories != null && categories.Any() ) {
					categoriesFilter = String.Format( "AND category_id {0}", Database.InClause( categories ) );
				}
				var id = shareBy == BrandOrAdvertiser.Advertiser ? "advertiser_id" : "brand_id";
				var table = shareBy == BrandOrAdvertiser.Advertiser ? "advertisers" : "brands";
				var name = shareBy == BrandOrAdvertiser.Advertiser ? "company_name" : "brand_name";

				cmd.CommandText = string.Format( @"
                    SELECT {0}, SUM({1}) as rowTotal, {7}, media_type
					FROM report_base_cache 
                        INNER JOIN {6} as t ON t.id = report_base_cache.{0}
					WHERE
					    play_date >= @start AND play_date < @end {2} {3} {4}
        			GROUP BY {0}, media_type
        			ORDER BY SUM({1}) DESC
            ", id, _valueColumn(), industryFilter, categoriesFilter, marketFilter, limit, table, name );

				cmd.Parameters.AddWithValue( "@start", _Period.CurrentStart );
				cmd.Parameters.AddWithValue( "@end", _Period.CurrentEnd );

                Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}';
                    {2}
                ", _Period.CurrentStart, _Period.CurrentEnd, cmd.CommandText));


                using ( var dr = cmd.ExecuteReader() ) {
					while ( dr.Read() ) {
						var row = new Item() {
							Id = dr.GetGuid( 0 ),
							Value = dr.IsDBNull( 1 ) ? 0 : dr.GetDecimal( 1 ),
							Name = dr.IsDBNull( 2 ) ? string.Empty : dr.GetString( 2 ),
							MediaType = dr.IsDBNull( 3 ) ? string.Empty : dr.GetString( 3 )
						};

						if ( allMedia.ContainsKey( row.Id ) ) {
							allMedia[row.Id].Value += row.Value;
						}
						else {
							allMedia[row.Id] = row;
						}

						if ( row.MediaType == "TV" )
							tvMedia.Add( row );
						else if ( row.MediaType == "Radio" )
							radioMedia.Add( row );

					}
				}

				AllMedia = _mapValuesAsPercent( allMedia.OrderByDescending( m => m.Value.Value ).Take( limit ).Select( m => m.Value ) );
				TvMedia = _mapValuesAsPercent( tvMedia.OrderByDescending( m => m.Value ).Take( limit ) );
				RadioMedia = _mapValuesAsPercent( radioMedia.OrderByDescending( m => m.Value ).Take( limit ) );
			}

		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="items"></param>
		/// <param name="itemsList"></param>
		private IEnumerable<Item> _mapValuesAsPercent( IEnumerable<Item> items )
		{
			var itemsList = new List<Item>();

			var sum = items.Sum( i => i.Value );

			if ( sum == 0 ) {
				itemsList = items.ToList();
			}
			else {
				foreach ( var item in items ) {
					itemsList.Add( new Item {
						Id = item.Id,
						Value = (item.Value / sum) * 100,
						Name = item.Name
					} );
				}
			}
			return itemsList;
		}

	}
}