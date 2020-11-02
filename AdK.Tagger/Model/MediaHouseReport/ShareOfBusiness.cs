using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using AdK.Tagger.Model.MediaHouseReport;
using AdK.Tagger.Model.Reporting;
using DatabaseCommon;
using MySql.Data.MySqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class ShareOfBusiness : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private DayPartType _dayPart { get; set; }
		public List<GenericReportTable> ShareTables { get; set; }
		public GenericReportTable SummaryTable { get; set; }
		public Chart<ChartRecord> CountChart { get; set; }
		public Chart<ChartRecord> SpendChart { get; set; }
		public Chart<ChartRecord> DurationChart { get; set; }

        public ShareOfBusiness(string userId, Guid focusChannelId, IncludeSet include, PeriodInfo period, DayPartType dayPart)
            : base(userId, focusChannelId, include, period)
		{
			_dayPart = dayPart;
			ShareTables = new List<GenericReportTable>();
			SummaryTable = new GenericReportTable( "Summary" );
			CountChart = new Chart<ChartRecord>();
			SpendChart = new Chart<ChartRecord>();
			DurationChart = new Chart<ChartRecord>();

			_loadData();
		}

		private void _loadData()
		{
			decimal maxPlayCountValue;
			decimal maxSpendValue;
			decimal maxDurationValue;

			//GENERATE SHARE BY TABLES AND GET TOTAL VALUES
			var playCountChannelTotals = _loadDataForValueColumn( "play_count", "Share by Count", out maxPlayCountValue );
			var spendChannelTotals = _loadDataForValueColumn( "earns", "Share by Spend", out maxSpendValue );
			var durationChannelTotals = _loadDataForValueColumn( "duration", "Share by Air-Time", out maxDurationValue );

			decimal playCountTotal = 0;
			decimal spendTotal = 0;
			decimal durationTotal = 0;

			//GENERATE SUMMARY TABLEW
			SummaryTable.Headers.Add( new List<string>() { "Channel", "Count", "Spend", "Air-Time" } );
			foreach ( var channel in Channels ) {
				var playCount = playCountChannelTotals.ContainsKey( channel.Id ) ? playCountChannelTotals[channel.Id] : 0;
				var spend = spendChannelTotals.ContainsKey( channel.Id ) ? spendChannelTotals[channel.Id] : 0;
				var duration = durationChannelTotals.ContainsKey( channel.Id ) ? durationChannelTotals[channel.Id] : 0;
				playCountTotal += playCount;
				spendTotal += spend;
				durationTotal += duration;

				SummaryTable.Rows.Add( new List<string>() { channel.Name, decimalToString( playCount ), decimalToString( spend ), decimalToString( duration ) } );
			}

			SummaryTable.Rows.Add( new List<string>() { "Total", decimalToString( playCountTotal ), decimalToString( spendTotal ), decimalToString( durationTotal ) } );

			//GENERATE SUMMARY CHARTS
			foreach ( var channel in Channels ) {
				if ( maxPlayCountValue > 0 && playCountChannelTotals.ContainsKey( channel.Id ) && playCountChannelTotals[channel.Id] > 0 ) {
					decimal percentValue = (playCountChannelTotals[channel.Id] / maxPlayCountValue) * 100;
					CountChart.Data.Add( new ChartRecord( channel.Id, channel.Name, percentValue ) );
				}

				if ( maxSpendValue > 0 && spendChannelTotals.ContainsKey( channel.Id ) && spendChannelTotals[channel.Id] > 0 ) {
					decimal percentValue = (spendChannelTotals[channel.Id] / maxSpendValue) * 100;
					SpendChart.Data.Add( new ChartRecord( channel.Id, channel.Name, percentValue ) );
				}

				if ( maxDurationValue > 0 && durationChannelTotals.ContainsKey( channel.Id ) && durationChannelTotals[channel.Id] > 0 ) {
					decimal percentValue = (durationChannelTotals[channel.Id] / maxDurationValue) * 100;
					DurationChart.Data.Add( new ChartRecord( channel.Id, channel.Name, percentValue ) );
				}
			}
		}

		private Dictionary<Guid, decimal> _loadDataForValueColumn( string valueColumn, string shareTableName, out decimal maxValue )
		{
			List<ReportDbRecord> dbRecords = _getDbRecords( valueColumn );
			var shareTable = new GenericReportTable( shareTableName );
			return _fillReportTableData( shareTable, dbRecords, out maxValue );
		}


		private Dictionary<Guid, decimal> _fillReportTableData( GenericReportTable table, List<ReportDbRecord> dbRecords, out decimal maxValue )
		{
			//dbRecords.ForEach( r => r.Value = (r.Value / maxValue) * 100 );

			var channelsTotal = new Dictionary<Guid, decimal>();

			var header1 = new List<string>();
			var header2 = new List<string>();
			var rows = new Dictionary<Guid, List<string>>();

			var totalRow = new List<string>();
			totalRow.Add( "TOTAL" );

			foreach ( var channel in Channels ) {
				var row = new List<string>();
				row.Add( channel.Name );
				rows[channel.Id] = row;
			}

			if ( _dayPart == DayPartType.MyDayParts ) {
				header1.Add( "" );
				header2.Add( "Channel" );
				List<DayPartSet> userDayPartSets = DayPartSet.GetForUser( _UserId );
				var dayPartTotals = new List<Dictionary<Guid, decimal>>();

				foreach ( var dayPartSets in userDayPartSets ) {
					foreach ( var dayPart in dayPartSets.Parts ) {
						var dayPartChannelTotal = new Dictionary<Guid, decimal>();
						var dayPartHours = dayPart.Hours.Select(h => h.Hour);
						if (dayPartHours == null || !dayPartHours.Any())
						{
							continue;
						}

						var startHour = dayPartHours.Min();
						var endHour = dayPartHours.Max();

						header1.Add( dayPart.Name );
						header2.Add( String.Format( "{0}-{1}h", startHour, endHour ) );

						foreach ( var channelValue in dbRecords ) {
							if ( dayPart.Hours.Any( dp => dp.Day == channelValue.PlayDate.DayOfWeek && dp.Hour == channelValue.PlayHour ) ) {
								if ( !dayPartChannelTotal.ContainsKey( channelValue.Id ) ) {
									dayPartChannelTotal[channelValue.Id] = 0;
								}
								if ( !channelsTotal.ContainsKey( channelValue.Id ) ) {
									channelsTotal[channelValue.Id] = 0;
								}

								dayPartChannelTotal[channelValue.Id] += channelValue.Value;
								channelsTotal[channelValue.Id] += channelValue.Value;
							}
						}

						dayPartTotals.Add( dayPartChannelTotal );
					}
				}

				maxValue = channelsTotal.Sum( ct => ct.Value );
				foreach ( var dayPartTotal in dayPartTotals ) {
					decimal columnTotal = 0;
					foreach ( var row in rows ) {
						var channelId = row.Key;
						decimal value = 0;

						if ( dayPartTotal.ContainsKey( channelId ) ) {
							value = dayPartTotal[channelId];
						}

						decimal valueInPercent = maxValue > 0 ? (value / maxValue) * 100 : 0;
						row.Value.Add( decimalToString( valueInPercent ) );
						columnTotal += valueInPercent;
					}

					totalRow.Add( decimalToString( columnTotal ) );

				}

				AddTotalColumnToTable( rows, maxValue, channelsTotal );

			}
			else {
				maxValue = dbRecords.Sum( r => r.Value );
				header1.Add( "Channel" );

				var dbRecordsByHour = new Dictionary<int, List<ReportDbRecord>>();
				foreach ( var dbRecord in dbRecords ) {
					if ( !dbRecordsByHour.ContainsKey( dbRecord.PlayHour ) ) {
						dbRecordsByHour[dbRecord.PlayHour] = new List<ReportDbRecord>();
					}
					dbRecordsByHour[dbRecord.PlayHour].Add( dbRecord );
				}

				for ( int i = 0; i < 24; i++ ) {
					header1.Add( String.Format( "{0}h", i ) );

					decimal columnTotal = 0;

					foreach ( var row in rows ) {
						decimal value = 0;
						var channelId = row.Key;
						if ( dbRecordsByHour.ContainsKey( i ) ) {
							value += dbRecordsByHour[i].Where( record => record.Id == channelId ).Sum( record => record.Value );
						}

						decimal valueInPercent = maxValue > 0 ? (value / maxValue) * 100 : 0;

						row.Value.Add( decimalToString( valueInPercent ) );

						if ( !channelsTotal.ContainsKey( channelId ) ) {
							channelsTotal[channelId] = 0;
						}
						channelsTotal[channelId] += value;
						columnTotal += valueInPercent;
					}

					totalRow.Add( decimalToString( columnTotal ) );
				}

				AddTotalColumnToTable(rows, maxValue, channelsTotal);

			}

			if ( header2.Any() ) {
				header1.Add( "" );
				header2.Add( "TOTAL" );
				table.Headers.Add( header1 );
				table.Headers.Add( header2 );
			}
			else {
				table.Headers.Add( header1 );
				header1.Add( "TOTAL" );
			}

			totalRow.Add( "100%" );
			table.Rows.AddRange( rows.Select( r => r.Value ).ToList() );
			table.Rows.Add( totalRow );

			ShareTables.Add( table );

			return channelsTotal;

		}

		private void AddTotalColumnToTable( Dictionary<Guid, List<string>> rows, decimal maxValue, Dictionary<Guid, decimal> channelsTotal )
		{
			foreach ( var row in rows ) {
				decimal valueInPercent = maxValue > 0 && channelsTotal.ContainsKey( row.Key ) ? (channelsTotal[row.Key] / maxValue) * 100 : 0;
				row.Value.Add( decimalToString( valueInPercent ) );
			}
		}


		private List<ReportDbRecord> _getDbRecords( string valueColumn )
		{
			using ( var conn = Database.Get() ) {
				var cmd = conn.CreateCommand();
				cmd.CommandText = String.Format( @"
				SELECT channel_id, SUM({0}) as total, play_hour, play_date
				FROM report_base_cache
				WHERE
                play_date >= @start AND play_date < @end
					AND channel_id {1} 
				GROUP BY channel_id, play_hour, play_date", valueColumn,
						Database.InClause( _ChannelIds ) );

				cmd.Parameters.AddWithValue( "@start", _Period.CurrentStart );
				cmd.Parameters.AddWithValue( "@end", _Period.CurrentEnd );

                Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    {2}
                ", _Period.CurrentStart, _Period.CurrentEnd, cmd.CommandText));

                var dbRecords = new List<ReportDbRecord>();
				var dr = cmd.ExecuteReader();
				while ( dr.Read() ) {
					Guid channelId = dr.GetGuid( 0 );
					var channel = Channels.First( c => c.Id == channelId );
					decimal value = dr.IsDBNull( 1 ) ? 0 : dr.GetDecimal( 1 );
					int playHour = dr.GetInt32( 2 );
					var playDate = dr.GetDateTime( 3 );

					dbRecords.Add( new ReportDbRecord( channelId, channel.Name, value, playDate, playHour ) );

				}

				return dbRecords;

			}
		}

		private string decimalToString( decimal dec )
		{
			return Math.Round( dec, 1 ).ToString( new CultureInfo( "en-US" ) ).Replace( ".0", "" );
		}


	}
}