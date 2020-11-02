using AdK.Tagger.Model.Reporting;
using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class AdvetiserActivityTrendInfo
	{
		public string channelId;
		public IncludeSet include;
		public PeriodInfo period;
		public int timeFrom;
		public int timeTo;
		public GroupingValue value;
	}

	public class AdvertisingActivityTrend : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private Func<decimal, ComputationResult> _ValueAdapter;
		private List<ChannelActivity> _Activities = new List<ChannelActivity>();
		private DateTime _dateFrom;
		private DateTime _dateTo;
		private int _timeFrom;
		private int _timeTo;

		public List<ChannelActivity> AdvertisingActivities = new List<ChannelActivity>();
		public class Row
		{
			public Guid ChannelId;
			public DateTime PlayDate;
			public int Value;
		}

		public class ChannelActivity
		{
			public Guid ChannelId;

			public List<PointValue> Values;

			public ChannelActivity( Guid channelId )
			{
				this.ChannelId = channelId;
				this.Values = new List<PointValue>();
			}
		}

		public AdvertisingActivityTrend( string userId, Guid focusChannelId, IncludeSet include, PeriodInfo period, int timeFrom, int timeTo, GroupingValue value )
			: base( userId, focusChannelId, include, period, value )
		{
			_dateFrom = this._Period.CurrentStart;
			_dateTo = this._Period.CurrentEnd; //Include last day
			_timeFrom = timeFrom;
			_timeTo = timeTo;
			_ValueAdapter = _GetValueAdapter();
			_getAdvertiserNames( userId );


			using ( var conn = Database.Get() ) {
				_getAdvertiserActivity( conn );
			}

			this.AdvertisingActivities = _Activities;
			this._Activities = null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="conn"></param>
		private void _getAdvertiserActivity( MySqlConnection conn )
		{
			List<Row> _Rows = new List<Row>();
			var cmd = conn.CreateCommand();
			cmd.CommandText = string.Format( @"
			SELECT channel_id, play_date, sum({0})
			FROM report_base_cache 
			WHERE
				play_date >= @dateStart AND play_date < @dateEnd AND
				 play_hour >= @timeStart AND play_hour < @timeEnd AND
				channel_id " + Database.InClause( _ChannelIds ) + @"
			GROUP BY play_date, channel_id", _valueColumn() );

			cmd.Parameters.AddWithValue( "@dateStart", _dateFrom.Date );
			cmd.Parameters.AddWithValue( "@dateEnd", _dateTo.Date );
			cmd.Parameters.AddWithValue( "@timeStart", _timeFrom );
			cmd.Parameters.AddWithValue( "@timeEnd", _timeTo );

            Log.Info(string.Format(@"
                    SET @dateStart:='{0:yyyy-MM-dd}'; 
                    SET @dateEnd:='{1:yyyy-MM-dd}'; 
                    SET @timeStart:='{2}'; 
                    SET @timeEnd:='{3}'; 
                    {4}
                ", _dateFrom.Date, _dateTo.Date, _timeFrom, _timeTo, cmd.CommandText));

            using ( var dr = cmd.ExecuteReader() ) {
				while ( dr.Read() ) {
					Guid channelId = dr.GetGuid( 0 );
					var value = dr.GetNullableInt( 2 );
					var row = new Row() {
						ChannelId = dr.GetGuid( 0 ),
						PlayDate = dr.GetDateTime( 1 ),
						Value = value.HasValue ? value.Value : 0
					};

					_Rows.Add( row );
				}
			}

			while ( _dateFrom < _dateTo ) {
				foreach ( var id in _ChannelIds ) {
					ChannelActivity ca;
					var row = _Rows.FirstOrDefault(r => r.ChannelId == id && r.PlayDate == _dateFrom.Date);

					var isNew = false;
					ca = _Activities.FirstOrDefault(a => a.ChannelId == id);
					if ( ca == null ) {
						isNew = true;
						ca = new ChannelActivity( id );
					}

					ca.Values.Add( new PointValue {
						Key = _dateFrom.ToString( "yyyy-MM-dd" ),
						Date = _dateFrom,
						Value = row != null ? row.Value : 0
					} );

					if ( isNew )
						_Activities.Add( ca );
				}
				_dateFrom = _dateFrom.AddDays( 1 );
			}
		}
	}
}