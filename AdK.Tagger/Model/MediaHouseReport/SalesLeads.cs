using AdK.Tagger.Model.Reporting;
using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Linq;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class SalesLeads : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private bool _LessThan;
		private decimal _LimitValue;
		private Func<decimal, ComputationResult> _ValueAdapter;
        private string _IndustryId;

		public class Row : AdvertiserRowBase
		{
		}
		public class ChannelValue : ChannelValueBase
		{
			public string LastDate;

			public long LastDateTimeStamp { get; set; }

		}

		public SalesLeads( string userId, Guid focusChannelId, IncludeSet include, PeriodInfo period, bool lessthan, decimal limitValue, string industryId )
			: base( userId, focusChannelId, include, period, GroupingValue.Spend )
		{
			_LessThan = lessthan;
			_LimitValue = limitValue;
			_ValueAdapter = _GetValueAdapter();
            _IndustryId = industryId;

			using ( var conn = Database.Get() ) {
				_getTopCurrent( conn );

				if ( _AdvertiserRows.Any() ) // May be empty when considered period has no airings at all
				{
					_getByChannelTotalAndLastAirings( conn );
				}
			}

			_getAdvertiserNames( userId );

			_getRowsEmptyForFocusChannel();

			_removeFocusChannelColumn();

			_AdvertiserRows = null;
		}

		/// <summary>
		/// Get the top advertisers for the selected value for the focus channel
		/// </summary>
		/// <param name="conn"></param>
		private void _getTopCurrent( MySqlConnection conn )
		{
			var cmd = conn.CreateCommand();
            string industryFilter = "";
            if (_IndustryId != "All")
            {
                industryFilter = "AND industry_id = @industry";
                cmd.Parameters.AddWithValue("@industry", _IndustryId);
            }

			cmd.CommandText = string.Format(@"
SELECT advertiser_id, SUM(earns) as total
FROM report_base_cache
WHERE
	play_date >= @start AND play_date < @end AND
	channel_id " + Database.InClause(_ChannelIds) + @"
    {0}
GROUP BY advertiser_id
HAVING SUM(earns) " + (_LessThan ? "<" : ">=") + @" @limitValue
ORDER BY SUM(earns) DESC", industryFilter);
			cmd.Parameters.AddWithValue( "@start", _Period.CurrentStart );
			cmd.Parameters.AddWithValue( "@end", _Period.CurrentEnd );
            cmd.Parameters.AddWithValue("@limitValue", _LimitValue);

            Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    SET @limitValue:='{2}'; 
                    {3}
                ", _Period.CurrentStart, _Period.CurrentEnd, _LimitValue, cmd.CommandText));


            using ( var dr = cmd.ExecuteReader() ) {
				while ( dr.Read() ) {
					Guid advertiserId = dr.GetGuid( 0 );

					var row = new Row() {
						ChannelValues = Enumerable.Range( 0, _ChannelIds.Count ).Select( i => new ChannelValue() ).ToArray(),
						CurrentTotal = dr.IsDBNull( 1 ) ? 0 : dr.GetDecimal( 1 )
					};
					row.CurrentRank = _AdvertiserRows.Count + 1;

					_AdvertiserRows[advertiserId] = row;
				}
			}
		}

		private void _getByChannelTotalAndLastAirings( MySqlConnection conn )
		{
			var cmd = conn.CreateCommand();
			cmd.CommandText = @"
SELECT advertiser_id, channel_id, SUM(earns) as total, MAX(play_date) as last_date
FROM report_base_cache
WHERE
	play_date >= @start AND play_date < @end AND
	advertiser_id " + Database.InClause( _AdvertiserRows.Keys ) + @" AND
	channel_id " + Database.InClause( _ChannelIds ) + @"
GROUP BY advertiser_id, channel_id";
			cmd.Parameters.AddWithValue( "@start", _Period.CurrentStart );
            cmd.Parameters.AddWithValue("@end", _Period.CurrentEnd);

            Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    {2}
                ", _Period.CurrentStart, _Period.CurrentEnd, cmd.CommandText));

            using ( var dr = cmd.ExecuteReader() ) {
				while ( dr.Read() ) {
					Guid advertiserId = dr.GetGuid( 0 );
					var row = _AdvertiserRows[advertiserId];

					Guid channelId = dr.GetGuid( 1 );
					int channelIndex = _ChannelIds.IndexOf( channelId );
					row.ChannelValues[channelIndex].Total = dr.IsDBNull( 2 ) ? 0 : dr.GetDecimal( 2 );
					DateTime? lastDate = dr.IsDBNull( 3 ) ? null : (DateTime?)dr.GetDateTime( 3 );

					if ( lastDate != null ) {

						(row.ChannelValues[channelIndex] as ChannelValue).LastDate = lastDate.GetValueOrDefault().ToString( "d" );
						(row.ChannelValues[channelIndex] as ChannelValue).LastDateTimeStamp = lastDate.GetValueOrDefault().Ticks;
					}
				}
			}
		}

		private void _getRowsEmptyForFocusChannel()
		{
			Rows = _AdvertiserRows.Values
				.Where( row => row.ChannelValues[0].Total == 0 )
				.OrderBy( row => row.CurrentRank )
				.ToList();
		}

		private void _removeFocusChannelColumn()
		{
			// Update ranks to fill gaps due to removed items
			Rows.Select( ( row, index ) => new { row, Rank = index + 1 } ).ToList().ForEach( a => {
				a.row.CurrentRank = a.Rank;
				a.row.ChannelValues = a.row.ChannelValues.Skip( 1 ).ToArray();
			} );
			Channels.RemoveAt( 0 );
		}
	}
}