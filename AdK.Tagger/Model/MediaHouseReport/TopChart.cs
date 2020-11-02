using AdK.Tagger.Model.Reporting;
using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class TopChart : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private bool _SortByPreviousPeriod;
		private Func<decimal, ComputationResult> _ValueAdapter;

		public class Row : AdvertiserRowBase
		{
			public int PreviousRank;
			public decimal PreviousTotal;
		}
		public class ChannelValue : ChannelValueBase { }

		public TopChart( string userId, Guid focusChannelId, IncludeSet include, PeriodInfo period, GroupingValue value )
			: base( userId, focusChannelId, include, period, value )
		{
			_ValueAdapter = _GetValueAdapter();

			using ( var conn = Database.Get() ) {
				_getTopCurrent( conn );

				if ( _AdvertiserRows.Any() ) // May be empty when considered period has no airings at all
				{
					_getCurrentByChannel( conn );

					_getPreviousTotals( conn );
				}
			}

			_getAdvertiserNames( userId );

			Rows = _AdvertiserRows.Values.ToList();
			_AdvertiserRows = null;

		}

		/// <summary>
		/// Get the top advertisers for the selected value for the focus channel
		/// </summary>
		/// <param name="conn"></param>
		private void _getTopCurrent( MySqlConnection conn )
		{
			var cmd = conn.CreateCommand();
			cmd.CommandText = string.Format( @"
			SELECT advertiser_id, SUM({0}) as total
			FROM report_base_cache
			WHERE
				play_date >= @start AND play_date < @end AND
				channel_id " + Database.InClause( _ChannelIds ) + @"
			GROUP BY advertiser_id
			ORDER BY SUM({0}) DESC", _valueColumn() );
            var start = _SortByPreviousPeriod ? _Period.PreviousStart : _Period.CurrentStart;
            var end = _SortByPreviousPeriod ? _Period.PreviousEnd : _Period.CurrentEnd;

            cmd.Parameters.AddWithValue( "@start", start );
			cmd.Parameters.AddWithValue( "@end", end );

            Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    {2}
                ", start, end, cmd.CommandText));

            using ( var dr = cmd.ExecuteReader() ) {
				while ( dr.Read() ) {
					Guid advertiserId = dr.GetGuid( 0 );

					var row = new Row() {
						ChannelValues = Enumerable.Range( 0, _ChannelIds.Count ).Select( i => new ChannelValue() ).ToArray(),
						CurrentTotal = dr.IsDBNull( 1 ) ? 0 : dr.GetDecimal( 1 )
					};
					if ( _SortByPreviousPeriod )
						row.PreviousRank = _AdvertiserRows.Count + 1;
					else
						row.CurrentRank = _AdvertiserRows.Count + 1;

					_AdvertiserRows[advertiserId] = row;
				}
			}
		}

		/// <summary>
		/// Get the advertiser total values for the compared channels
		/// </summary>
		/// <param name="conn"></param>
		private void _getCurrentByChannel( MySqlConnection conn )
		{
			var cmd = conn.CreateCommand();
			cmd.CommandText = string.Format( @"
			SELECT advertiser_id, channel_id, SUM({0}) as total
			FROM report_base_cache
			WHERE
				play_date >= @start AND play_date < @end AND
				advertiser_id " + Database.InClause( _AdvertiserRows.Keys ) + @" AND
				channel_id " + Database.InClause( _ChannelIds ) + @"
			GROUP BY advertiser_id, channel_id", _valueColumn() );

            var start = _SortByPreviousPeriod ? _Period.PreviousStart : _Period.CurrentStart;
            var end = _SortByPreviousPeriod ? _Period.PreviousEnd : _Period.CurrentEnd;

            cmd.Parameters.AddWithValue( "@start", start );
			cmd.Parameters.AddWithValue( "@end", end );

            Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    {2}
                ", start, end, cmd.CommandText));

            using ( var dr = cmd.ExecuteReader() ) {
				while ( dr.Read() ) {
					Guid advertiserId = dr.GetGuid( 0 );
					var row = _AdvertiserRows[advertiserId];

					Guid channelId = dr.GetGuid( 1 );
					int channelIndex = _ChannelIds.IndexOf( channelId );
					row.ChannelValues[channelIndex].Total = dr.IsDBNull( 2 ) ? 0 : dr.GetDecimal( 2 );
				}
			}
		}

		/// <summary>
		/// Get advertiser values for the previous period
		/// </summary>
		/// <param name="conn"></param>
		private void _getPreviousTotals( MySqlConnection conn )
		{
			var cmd = conn.CreateCommand();
			cmd.CommandText = string.Format( @"
			SELECT advertiser_id, SUM({0}) as total
			FROM report_base_cache
			WHERE
				play_date >= @start AND play_date < @end AND
				channel_id " + Database.InClause( _ChannelIds ) + @"
			GROUP BY advertiser_id
			ORDER BY SUM({0}) DESC", _valueColumn() );

			cmd.Parameters.AddWithValue( "@start", _SortByPreviousPeriod ? _Period.CurrentStart : _Period.PreviousStart );
			cmd.Parameters.AddWithValue( "@end", _SortByPreviousPeriod ? _Period.CurrentEnd : _Period.PreviousEnd );

			using ( var dr = cmd.ExecuteReader() ) {
				int rank = 1;
				while ( dr.Read() ) {
					Guid advertiserId = dr.GetGuid( 0 );
					var value = dr.IsDBNull( 1 ) ? 0 : dr.GetDecimal( 1 );
					if ( _AdvertiserRows.ContainsKey( advertiserId ) ) {
						var row = _AdvertiserRows[advertiserId];
						(row as Row).PreviousTotal = value;

						if ( _SortByPreviousPeriod )
							row.CurrentRank = rank;
						else
							(row as Row).PreviousRank = rank;
					}
					++rank;
				}
			}
		}
	}
}