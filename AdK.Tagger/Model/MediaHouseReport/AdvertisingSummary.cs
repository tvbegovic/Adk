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
	public class ReadAllAdvertisingSummaryDataModel
	{
		public Guid ChannelId { get; set; }
		public string ChannelName { get; set; }
		public Guid BrandAdvertiserId { get; set; }
		public string BrandAdvertiserName { get; set; }
		public DateTime PlayDate { get; set; }
		public decimal AirTime { get; set; }
		public decimal EstimateSpend { get; set; }
		public int PlayCount { get; set; }
	}

    public class AdvertisingSummaryGroup
    {
        public AdvertisingSummaryGroup(string groupName, List<AdvertisingSummaryDetailsDataModel> group)
        {
            this.GroupName = groupName;
            this.AdvertisingSummaryDetailsData = group;
        }
        public string GroupName { get; set; }
        public List<AdvertisingSummaryDetailsDataModel> AdvertisingSummaryDetailsData { get; set; }
        public AdvertisingSummaryDetailsDataModel Total { get; set; }
    }

	public class AdvertisingSummaryGroupDataModel
	{
		public AdvertisingSummaryGroupDataModel()
		{
			AdvertisingSummaryDetailsData = new List<AdvertisingSummaryDetailsDataModel>();
		}
		public Guid GropId { get; set; }
		public string GroupName { get; set; }
		public int TotalCount { get; set; }
		public decimal TotalDuraton { get; set; }
		public decimal TotalSpend { get; set; }
		public List<AdvertisingSummaryDetailsDataModel> AdvertisingSummaryDetailsData { get; set; }
	}

	public class AdvertisingSummaryDetailsDataModel
	{
		public Guid DetailId { get; set; }
		public string GroupName { get; set; }
		public string DetailName { get; set; }
		public int Count { get; set; }
		public decimal Duration { get; set; }
		public decimal Spend { get; set; }
	}

	public class AdvertisingSummary : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public string UserId { get; set; }
		public IncludeSet Include { get; set; }
		public BrandAdvertiserOrChannel GroupBy { get; set; }
        public List<AdvertisingSummaryGroup> AdvertisingSummaryDetailsData { get; set; }

		public AdvertisingSummary( string userId, Guid focusChannelId, IncludeSet include, PeriodInfo period, BrandAdvertiserOrChannel groupBy )
			: base( userId, focusChannelId, include, period )
		{
			UserId = userId;
			Include = include;
			GroupBy = groupBy;

			using ( var conn = Database.Get() ) {
				GetAdvertisingSummaryData( conn );
			}
		}

		/// <summary>
		/// Get Advertising Summary data for the selected values
		/// </summary>
		/// <param name="conn"></param>
		private void GetAdvertisingSummaryData( MySqlConnection conn )
		{
			var cmd = conn.CreateCommand();

			if ( GroupBy == BrandAdvertiserOrChannel.Advertiser ) {
				cmd.CommandText = string.Format(
			  @"  SELECT C.id, C.station_name, A.id, A.company_name, R.play_date, R.duration, R.earns, R.play_count
               FROM report_base_cache R
					LEFT JOIN channels C ON R.channel_id = C.id
					LEFT JOIN advertisers A ON R.advertiser_id = A.id
               WHERE R.play_date >= @start AND R.play_date < @end 
					AND R.channel_id {0} 
					ORDER BY C.station_name ASC", Database.InClause( _ChannelIds ) );

			}
			else {
				cmd.CommandText = string.Format(
				 @"  SELECT C.id, C.station_name, B.id, B.brand_name, R.play_date, R.duration, R.earns, R.play_count
               FROM report_base_cache R
					LEFT JOIN channels C ON R.channel_id = C.id
					LEFT JOIN brands B ON R.brand_id = B.id
               WHERE R.play_date >= @start AND R.play_date < @end 
					AND R.channel_id {0} 
					ORDER BY C.station_name ASC", Database.InClause( _ChannelIds ) );
			}

			cmd.Parameters.AddWithValue( "@start", _Period.CurrentStart );
			cmd.Parameters.AddWithValue( "@end", _Period.CurrentEnd );

            Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    {2}
                ", _Period.CurrentStart, _Period.CurrentEnd, cmd.CommandText));


            AdvertisingSummaryDetailsData = new List<AdvertisingSummaryGroup>();
			var dbResultList = new List<ReadAllAdvertisingSummaryDataModel>();
			using ( var reader = cmd.ExecuteReader() ) {
				while ( reader.Read() ) {
					var row = new ReadAllAdvertisingSummaryDataModel();
					row.ChannelId = reader.IsDBNull( 0 ) ? Guid.Empty : reader.GetGuid( 0 );
					row.ChannelName = reader.IsDBNull( 1 ) ? String.Empty : reader.GetString( 1 );
					row.BrandAdvertiserId = reader.IsDBNull( 2 ) ? Guid.Empty : reader.GetGuid( 2 );
					row.BrandAdvertiserName = reader.IsDBNull( 3 ) ? String.Empty : reader.GetString( 3 );
					row.PlayDate = reader.IsDBNull( 4 ) ? DateTime.MinValue : reader.GetDateTime( 4 );
					row.AirTime = reader.IsDBNull( 5 ) ? 0 : reader.GetDecimal( 5 );
					row.EstimateSpend = reader.IsDBNull( 6 ) ? 0 : reader.GetDecimal( 6 );
					row.PlayCount = reader.IsDBNull( 7 ) ? 0 : reader.GetInt32( 7 );

					dbResultList.Add( row );
				}
			}

			if ( GroupBy == BrandAdvertiserOrChannel.Brand || GroupBy == BrandAdvertiserOrChannel.Advertiser ) {
				var advertisingSummaryGroupDataList = dbResultList.GroupBy( g => new {
					g.BrandAdvertiserId,
					g.BrandAdvertiserName
				} ).Select( s => new AdvertisingSummaryGroupDataModel {
					GropId = s.Key.BrandAdvertiserId,
					GroupName = s.Key.BrandAdvertiserName,
					TotalCount = 0,
					TotalDuraton = 0,
					TotalSpend = 0
				} );

				foreach ( var groupData in advertisingSummaryGroupDataList ) {
					var groupByBrandList = dbResultList.Where( w => w.BrandAdvertiserId == groupData.GropId )
				  .GroupBy( g => new {
					  g.ChannelId,
					  g.ChannelName
				  } )
				  .Select( s => new AdvertisingSummaryDetailsDataModel {
					  DetailId = s.Key.ChannelId,
					  DetailName = s.Key.ChannelName,
					  Count = s.Sum( x => x.PlayCount ),
					  Duration = s.Sum( x => x.AirTime ),
					  Spend = s.Sum( x => x.EstimateSpend )
				  } ).OrderByDescending( o => o.Spend );

					AddGroupDataToAdvertisingSummaryDetailsData( groupByBrandList, groupData );

				}
			}
			else {
				var advertisingSummaryGroupDataList = dbResultList.GroupBy( g => new {
					g.ChannelId,
					g.ChannelName
				} ).Select( s => new AdvertisingSummaryGroupDataModel {
					GropId = s.Key.ChannelId,
					GroupName = s.Key.ChannelName,
					TotalCount = 0,
					TotalDuraton = 0,
					TotalSpend = 0
				} );

				foreach ( var groupData in advertisingSummaryGroupDataList ) {
					var groupByChannelList = dbResultList.Where( w => w.ChannelId == groupData.GropId )
						 .GroupBy( g => new {
							 g.BrandAdvertiserId,
							 g.BrandAdvertiserName
						 } )
						 .Select( s => new AdvertisingSummaryDetailsDataModel {
							 DetailId = s.Key.BrandAdvertiserId,
							 DetailName = s.Key.BrandAdvertiserName,
							 Count = s.Sum( x => x.PlayCount ),
							 Duration = s.Sum( x => x.AirTime ),
							 Spend = s.Sum( x => x.EstimateSpend )
						 } ).OrderByDescending( o => o.Spend );


					AddGroupDataToAdvertisingSummaryDetailsData( groupByChannelList, groupData );

				}
			}

		}


		public void AddGroupDataToAdvertisingSummaryDetailsData( IEnumerable<AdvertisingSummaryDetailsDataModel> groupList, AdvertisingSummaryGroupDataModel groupData )
		{
			var groupName = string.Empty;
			int count = 0;
			decimal duration = 0;
			decimal spend = 0;
            AdvertisingSummaryGroup group = new AdvertisingSummaryGroup(groupData.GroupName, new List<AdvertisingSummaryDetailsDataModel>());

			foreach ( var data in groupList ) {
				var asData = new AdvertisingSummaryDetailsDataModel();
				asData.DetailId = data.DetailId;
				asData.GroupName = groupData.GroupName;
				asData.DetailName = data.DetailName;
				asData.Count = data.Count;
				asData.Duration = data.Duration;
                asData.Spend = data.Spend;

				count += data.Count;
				duration += data.Duration;
				spend += data.Spend;
                group.AdvertisingSummaryDetailsData.Add(asData);
			}

			var total = new AdvertisingSummaryDetailsDataModel();
			total.GroupName = groupData.GroupName;
			total.DetailName = "TOTAL";
			total.Count = count;
			total.Duration = duration;
			total.Spend = spend;

            group.Total = total;
            AdvertisingSummaryDetailsData.Add(group);
		}

	}
}