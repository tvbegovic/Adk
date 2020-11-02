using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseCommon;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class ShareOfAdvertisingActivity : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public List<Chart<ChartRecord>> Charts;
		public List<Chart<ChartRecord>> PercentageCharts { get; set; }

		public ShareOfAdvertisingActivity( string userId, Guid focusChannelId, IncludeSet include, GroupingValue value, PeriodInfo period, DayOfWeekRange dayOfWeekRange, DayPartType dayPart )
			: base(userId, focusChannelId, include, period, value)
		{
			var dateFrom = _Period.CurrentStart;
			var dateTo = _Period.CurrentEnd;

			Charts = new List<Chart<ChartRecord>>();
			PercentageCharts = new List<Chart<ChartRecord>>();

			var datesToInclude = new List<DateTime>();
			while ( dateFrom.Date < dateTo.Date ) {
				if ( IsInDayRange( dateFrom, dayOfWeekRange ) ) {
					datesToInclude.Add( dateFrom );
				}

				dateFrom = dateFrom.AddDays( 1 );
			}

			if ( datesToInclude.Any() ) {
				_loadData( userId, datesToInclude, dayPart );
			}
		}

		private void _loadData( string userId, IEnumerable<DateTime> datesToInclude, DayPartType dayPartSet )
		{
			using ( var conn = Database.Get() ) {
				var cmd = conn.CreateCommand();
				cmd.CommandText = string.Format( @"
				SELECT channel_id, SUM({0}) as total, play_date, play_hour
				FROM report_base_cache
				WHERE
					play_date {1} AND
					channel_id {2} 
				GROUP BY channel_id, play_date, play_hour", _valueColumn(), Database.InClause( datesToInclude ), Database.InClause( _ChannelIds ) );

                Log.Info( cmd.CommandText );

                var dbRecords = new List<ReportDbRecord>();

				using ( var dr = cmd.ExecuteReader() ) {
					while ( dr.Read() ) {
						Guid channelId = dr.GetGuid( 0 );
						var channel = Channels.First( c => c.Id == channelId );
						decimal value = dr.IsDBNull( 1 ) ? 0 : dr.GetDecimal( 1 );
						var playDate = dr.GetDateTime( 2 );
						int playHour = dr.GetInt32( 3 );

						dbRecords.Add( new ReportDbRecord( channelId, channel.Name, value, playDate, playHour ) );

					}
				}


				if ( dayPartSet == DayPartType.MyDayParts ) {
					List<DayPartSet> userDayPartSets = DayPartSet.GetForUser( userId );
					foreach ( var dayPartSets in userDayPartSets ) {
						foreach ( var dayPart in dayPartSets.Parts ) {

							var filteredDbRecords = new List<ReportDbRecord>();
							var chart = new Chart<ChartRecord>();
							chart.Name = dayPart.Name;

							foreach ( var channelValue in dbRecords ) {
								if ( dayPart.Hours.Any( dp => dp.Day == channelValue.PlayDate.DayOfWeek && dp.Hour == channelValue.PlayHour ) ) {
									filteredDbRecords.Add( channelValue );
								}
							}

							chart.Data = SumDbRecordsByChannel( filteredDbRecords );
							AddChartToCharts( chart );

						}
					}
				}
				else {
					var chart = new Chart<ChartRecord>();
					chart.Data = SumDbRecordsByChannel( dbRecords );
					AddChartToCharts( chart );
				}


			}

			//CALCULATE GRAPH PERCENTAGE
			foreach (var chart in Charts)
			{
				var percentageChart = new Chart<ChartRecord>(chart.Name, chart.MaxChartValue);
				foreach (var data in chart.Data)
				{
					decimal percentage = (data.Value / chart.MaxChartValue) * 100;
					percentageChart.Data.Add(new ChartRecord(data.Id, data.Name, percentage));
				}

				PercentageCharts.Add( percentageChart );
			}
		}

		private void AddChartToCharts( Chart<ChartRecord> chart )
		{
			if ( chart.Data == null || !chart.Data.Any() ) return;
			chart.MaxChartValue = chart.Data.Sum( cd => cd.Value );
			Charts.Add( chart );
		}

		private List<ChartRecord> SumDbRecordsByChannel( List<ReportDbRecord> records )
		{
			List<ChartRecord> chartRecords = new List<ChartRecord>();
			foreach ( var record in records ) {
				var cr = chartRecords.FirstOrDefault( c => c.Id == record.Id );
				if ( cr == null ) {
					chartRecords.Add( new ChartRecord( record.Id, record.Name, record.Value ) );
				}
				else {
					cr.Value += record.Value;
				}
			}
			return chartRecords;
		}

		private bool IsInDayRange( DateTime date, DayOfWeekRange dayRange )
		{
			switch ( dayRange ) {
				case DayOfWeekRange.All:
					return true;
				case DayOfWeekRange.M_F:
					return !IsWeekend( date );
				case DayOfWeekRange.Weekends:
					return IsWeekend( date );
				default:
					return (int)dayRange == (int)date.DayOfWeek;
			}
		}

		private bool IsWeekend( DateTime date )
		{
			return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
		}
	}
}