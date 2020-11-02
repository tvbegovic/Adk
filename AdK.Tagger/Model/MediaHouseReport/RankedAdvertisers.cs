using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using AdK.Tagger.Model.Reporting;
using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class RankedAdvertisers : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private List<Guid> _GroupPropertiesChannelsIds;
		private List<Guid> _CompetitorsChannelIds;
		private Dictionary<Guid, Row> _RankedAdvertiserRows;

		public List<Row> RankedAdvertiserRows;
		public List<Channel> GroupPropertiesChannels;
		public List<Channel> CompetitorsChannels;

		public class Row : AdvertiserRowBase
		{
			public ChannelValue[] ChannelValuesGroup;
			public ChannelValue[] ChannelValuesCompetitors;
			public decimal GrandTotal;
		}
		public class ChannelValue : ChannelValueBase
		{
			public decimal Value;
		}

		public static List<Advertiser> SearchAdvertiserNamesByCriteria( string criteria )
		{
			using ( var conn = Database.Get() ) {
				var cmd = conn.CreateCommand();
				var query =
						  string.Format(
								@"SELECT id, company_name
                        FROM advertisers
                        WHERE company_name LIKE @criteria
                        GROUP BY company_name                            
                        ORDER BY company_name ASC
                        LIMIT 15" );
				cmd.Parameters.AddWithValue( "@criteria", "%" + criteria + "%" );

                return Database.ListFetcher( query, dr => new Advertiser() {
					Id = dr.GetGuid( 0 ),
					Name = dr.GetString( 1 )
				}, "@criteria", "%" + criteria + "%" );
			}
		}

		public RankedAdvertisers( string userId, GroupingValue value, PeriodInfo period, string marketId )
			: base( userId, value, period )
		{
			_GroupPropertiesChannelsIds = new List<Guid>();
			_CompetitorsChannelIds = new List<Guid>();
			_RankedAdvertiserRows = new Dictionary<Guid, Row>();

			GroupPropertiesChannels = Channel.GetByUser( userId );
			_GroupPropertiesChannelsIds = GroupPropertiesChannels.Select( c => c.Id ).ToList();
			_CompetitorsChannelIds = Competitor.GetAll( userId )
												.Where( c => GroupPropertiesChannels.Any( gc => gc.Id == c.MyChannelId ) )
												.Select( c => c.OtherChannelId )
                                                .Distinct()
                                                .ToList();

			CompetitorsChannels = Channel.Get( _CompetitorsChannelIds );

			if ( !_GroupPropertiesChannelsIds.Any() && !_CompetitorsChannelIds.Any() ) {
				return;
			}


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
				var channelsToInclude = new List<Guid>();
				channelsToInclude.AddRange( _GroupPropertiesChannelsIds );
				channelsToInclude.AddRange( _CompetitorsChannelIds );
				var channelsFilter = String.Format( "AND channel_id {0}", Database.InClause( channelsToInclude ) );

				_getRankedAdvertisers( conn, channelsFilter, marketFilter );

				// May be empty when considered period has no airings at all
				if ( _RankedAdvertiserRows.Any() ) {
					_getCurrentByChannel( conn, channelsFilter, marketFilter );
				}
			}

			//Retrieve other channel ids
			_getRankedAdvertiserNames( userId );

			RankedAdvertiserRows = _RankedAdvertiserRows.Values.OrderBy( row => row.CurrentRank ).ToList();
			_RankedAdvertiserRows = null;
		}


		/// <summary>
		/// Get the top advertisers for the selected value for the focus channel
		/// </summary>
		/// <param name="conn"></param>
		private void _getRankedAdvertisers( MySqlConnection conn, string channelsFilter, string marketFilter )
		{
			var cmd = conn.CreateCommand();
			cmd.CommandText = string.Format( @"
			SELECT advertiser_id, SUM({0}) as total
			FROM report_base_cache
			WHERE
				play_date >= @start AND play_date < @end {1} {2}
			GROUP BY advertiser_id
			ORDER BY SUM({0}) DESC", _valueColumn(), channelsFilter, marketFilter);
			cmd.Parameters.AddWithValue( "@start", _Period.CurrentStart );
			cmd.Parameters.AddWithValue( "@end", _Period.CurrentEnd );

            Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    {2}
                ", _Period.CurrentStart, _Period.CurrentEnd, cmd.CommandText));

            using ( var dr = cmd.ExecuteReader() ) {
				while ( dr.Read() ) {
					Guid advertiserId = dr.GetGuid( 0 );

					var row = new Row() {
						ChannelValuesGroup = Enumerable.Range( 0, _GroupPropertiesChannelsIds.Count ).Select( i => new ChannelValue() ).ToArray(),
						ChannelValuesCompetitors = Enumerable.Range( 0, _CompetitorsChannelIds.Count ).Select( i => new ChannelValue() ).ToArray(),
						GrandTotal = dr.IsDBNull( 1 ) ? 0 : dr.GetDecimal( 1 )
					};
					row.CurrentRank = _RankedAdvertiserRows.Count + 1;

					_RankedAdvertiserRows[advertiserId] = row;
				}
			}
		}

		/// <summary>
		/// Get the advertiser total values for the compared channels
		/// </summary>
		/// <param name="conn"></param>
		private void _getCurrentByChannel( MySqlConnection conn, string channelsFilter, string marketFilter )
		{
			string rankedAdviserFilter = "";
			if ( _RankedAdvertiserRows.Keys.Any() ) {
				rankedAdviserFilter = String.Format( "AND advertiser_id {0}", Database.InClause( _RankedAdvertiserRows.Keys ) );
			}

			var cmd = conn.CreateCommand();
			cmd.CommandText = string.Format( @"
			SELECT advertiser_id, channel_id, SUM({0}) as total
			FROM report_base_cache
			WHERE
				play_date >= @start AND play_date < @end {1} {2} {3}
			GROUP BY advertiser_id, channel_id", _valueColumn(), channelsFilter, rankedAdviserFilter, marketFilter );
			cmd.Parameters.AddWithValue( "@start", _Period.CurrentStart );
			cmd.Parameters.AddWithValue( "@end", _Period.CurrentEnd );

			using ( var dr = cmd.ExecuteReader() ) {
				while ( dr.Read() ) {
					Guid advertiserId = dr.GetGuid( 0 );
					var row = _RankedAdvertiserRows[advertiserId];

					Guid channelId = dr.GetGuid( 1 );
					decimal rowValue = dr.IsDBNull( 2 ) ? 0 : dr.GetDecimal( 2 );

					if ( _GroupPropertiesChannelsIds.Contains( channelId ) ) {
						int channelIndex = _GroupPropertiesChannelsIds.IndexOf( channelId );
						row.ChannelValuesGroup[channelIndex].Total = rowValue;
						row.ChannelValuesGroup[channelIndex].Value = rowValue;
					}
					else {
						int channelIndex = _CompetitorsChannelIds.IndexOf( channelId );
						row.ChannelValuesCompetitors[channelIndex].Total = rowValue;
						row.ChannelValuesCompetitors[channelIndex].Value = rowValue;
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="userId"></param>
		private void _getRankedAdvertiserNames( string userId )
		{
			var keyAccounts = KeyAccount.GetByUser( userId );
			var advertiserDic = Company.Get( _RankedAdvertiserRows.Keys ).ToDictionary( a => a.Id );

			foreach ( var advertiserRow in _RankedAdvertiserRows ) {
				advertiserRow.Value.AdvertiserName = advertiserDic[advertiserRow.Key].Name;
				advertiserRow.Value.AdvertiserId = advertiserRow.Key.ToString();
				advertiserRow.Value.IsKeyAccount = keyAccounts.Any( ba => !ba.IsBrand && ba.Id == advertiserRow.Key );
			}
		}
	}
}