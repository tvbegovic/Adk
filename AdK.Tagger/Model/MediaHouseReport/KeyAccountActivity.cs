using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseCommon;

namespace AdK.Tagger.Model.MediaHouseReport
{

	public class ChannelKeyAccountActivity
	{
		public ChannelKeyAccountActivity( Guid channelId, string channelName )
		{
			ChanneId = channelId;
			key = channelName;
			values = new List<BrandValue>();
		}

		public Guid ChanneId { get; set; }

		/// <summary>
		/// key property is needed by nvD3 library to draw chart (*reason for bad naming convention)
		/// </summary>
		public string key { get; set; }

		/// <summary>
		/// values property is needed by nvD3 library to draw chart (*reason for bad naming convention)
		/// </summary>
		public List<BrandValue> values { get; set; }


	}

	public class BrandValue
	{
		public BrandValue( Guid id, string name, decimal value )
		{
			BrandId = id;
			BrandName = name;
			Value = value;
		}

		public Guid BrandId { get; set; }
		public string BrandName { get; set; }
		public decimal Value { get; set; }
	}

	public class KeyAccountActivity : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public List<ChannelKeyAccountActivity> ChartData { get; set; }
		public List<ChannelKeyAccountActivity> PercentageChartData { get; set; }

		public decimal MaxTotalValue { get; set; }
		public decimal MaxPercentageValue { get; set; }
		public decimal SumChartValue { get; set; }
		public KeyAccountActivity( string userId, Guid focusChannelId, IncludeSet include, GroupingValue value, PeriodInfo period, bool showAllAccounts )
			: base( userId, focusChannelId, include, period, value )
		{
			PercentageChartData = new List<ChannelKeyAccountActivity>();
			ChartData = new List<ChannelKeyAccountActivity>();

			MaxPercentageValue = 0;


			if ( _Period.CurrentStart.Date <= _Period.CurrentEnd.Date ) {
				_loadData( userId, showAllAccounts );
			}
		}

		private void _loadData( string userId, bool showAllAcounts )
		{
			using ( var conn = Database.Get() ) {

				string keyAccountsFilter = "";
				if ( !showAllAcounts ) {
					var keyAccounts = KeyAccount.GetByUser( userId );
					var brandKeyAccounts = keyAccounts.Where( ka => ka.IsBrand ).Select( ka => ka.Id );
					var advertiserKeyAccounts = keyAccounts.Where( ka => !ka.IsBrand ).Select( ka => ka.Id );
					//No key accounts defined
					if ( !brandKeyAccounts.Any() && !advertiserKeyAccounts.Any() ) {
						return;
					}
					else if ( brandKeyAccounts.Any() && advertiserKeyAccounts.Any() ) {
						keyAccountsFilter = string.Format( "AND ( C.brand_id {0} OR C.advertiser_id {1} )",
							Database.InClause( brandKeyAccounts ), Database.InClause( advertiserKeyAccounts ) );

					}
					else if ( brandKeyAccounts.Any() ) {
						keyAccountsFilter = string.Format( "AND C.brand_id {0}", Database.InClause( brandKeyAccounts ) );
					}
					else {
						keyAccountsFilter = string.Format( "AND C.advertiser_id {1}", Database.InClause( advertiserKeyAccounts ) );
					}
				}

				var cmd = conn.CreateCommand();
				cmd.CommandText = string.Format( @"
				SELECT C.channel_id, C.brand_id, B.brand_name, SUM(C.{0}) as total
				FROM report_base_cache C
				LEFT JOIN brands B on C.brand_id = B.id
				WHERE
					C.play_date >= @start AND C.play_date < @end AND
					channel_id {1} {2}
				GROUP BY C.channel_id, C.brand_id, B.brand_name
				ORDER BY SUM(C.{0}) DESC", _valueColumn(), Database.InClause( _ChannelIds ), keyAccountsFilter );

				cmd.Parameters.AddWithValue( "@start", _Period.CurrentStart.Date );
				cmd.Parameters.AddWithValue( "@end", _Period.CurrentEnd.Date );

                Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    {2}
                ", _Period.CurrentStart.Date, _Period.CurrentEnd.Date, cmd.CommandText));

                var allBrandsTotalValues = new Dictionary<Guid, BrandValue>();
				var chanelBrandValues = new Dictionary<Guid, List<BrandValue>>();

				using ( var dr = cmd.ExecuteReader() ) {

					while ( dr.Read() ) {
						if ( dr.IsDBNull( 0 ) || dr.IsDBNull( 1 ) ) {
							continue;
						}

						Guid channelId = dr.GetGuid( 0 );
						var channel = Channels.First( c => c.Id == channelId );
						Guid brandId = dr.GetGuid( 1 );
						string brandName = dr.IsDBNull( 2 ) ? "" : dr.GetString( 2 );
						decimal value = dr.IsDBNull( 3 ) ? 0 : dr.GetDecimal( 3 );

						//Calculate total value for brand
						if ( allBrandsTotalValues.ContainsKey( brandId ) ) {
							allBrandsTotalValues[brandId].Value = allBrandsTotalValues[brandId].Value + value;
						}
						else {
							allBrandsTotalValues[brandId] = new BrandValue( brandId, brandName, value );
						}

						var channelActivity = ChartData.FirstOrDefault( c => c.ChanneId == channelId );
						if ( channelActivity == null ) {
							channelActivity = new ChannelKeyAccountActivity( channelId, channel.Name );
							ChartData.Add( channelActivity );
							chanelBrandValues.Add( channelId, new List<BrandValue>() );
						}

						chanelBrandValues[channelId].Add( new BrandValue( brandId, brandName, value ) );

					}

				}

				MaxTotalValue = allBrandsTotalValues.Any() ? allBrandsTotalValues.Max( b => b.Value.Value ) : 0;


				SumChartValue = allBrandsTotalValues.Any() ? allBrandsTotalValues.Sum( b => b.Value.Value ) : 0;
				var orderedAllBrandsTotalValues = allBrandsTotalValues.ToList().OrderByDescending( b => b.Value.Value );

				//For graph to look good all channels should have entry for all brands.
				foreach ( var channelAccountActivity in ChartData ) {
					var percentageChannelData = new ChannelKeyAccountActivity( channelAccountActivity.ChanneId, channelAccountActivity.key );

					foreach ( var brand in orderedAllBrandsTotalValues ) {
						//Every channel need to have same number of brands and order need to be exact the same for graph to show correctly. 
						var brandValue = chanelBrandValues[channelAccountActivity.ChanneId].FirstOrDefault( b => b.BrandId == brand.Key );
						if ( brandValue == null ) {
							brandValue = new BrandValue( brand.Key, brand.Value.BrandName, 0 );
						}

						channelAccountActivity.values.Add( brandValue );
						decimal percentage = (brandValue.Value / SumChartValue) * 100;
						MaxPercentageValue = percentage > MaxPercentageValue ? percentage : MaxPercentageValue;
						percentageChannelData.values.Add( new BrandValue( brandValue.BrandId, brandValue.BrandName, percentage ) );
					}

					PercentageChartData.Add( percentageChannelData );
				}

			}
		}

	}
}