using AdK.Tagger.Model.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml.FormulaParsing.Utilities;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class ReportBase
	{
		public List<AdvertiserRowBase> Rows;
		protected Guid _focusChannelId;
		public List<Channel> Channels;
		protected List<Guid> _ChannelIds;

		protected Period _Period;
		public string PeriodStart;
		public string PeriodEnd;
		public string PreviousStart;
		public string PreviousEnd;

		protected string _UserId;

		protected GroupingValue _GroupingValue;
		protected Dictionary<Guid, AdvertiserRowBase> _AdvertiserRows;

		public ReportBase( string userId )
		{
			_UserId = userId;
		}

		public ReportBase( string userId, Guid focusChannelId, IncludeSet include, PeriodInfo period = null )
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = Localization.GetCulture();
			if ( period != null ) {
				FillPeriodData( period );
			}
			FillReportBaseData( userId, focusChannelId, include );
		}

		public ReportBase( string userId, Guid focusChannelId, IncludeSet include, PeriodKind period )
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = Localization.GetCulture();
			FillPeriodData( period );
			FillReportBaseData( userId, focusChannelId, include );
		}

		public ReportBase( string userId, Guid focusChannelId, IncludeSet include, PeriodInfo period, GroupingValue value )
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = Localization.GetCulture();
			_GroupingValue = value;
			FillPeriodData( period );
			FillReportBaseData( userId, focusChannelId, include );
		}

		public ReportBase( string userId, Guid focusChannelId, IncludeSet include, GroupingValue value )
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = Localization.GetCulture();
			_GroupingValue = value;
			FillReportBaseData( userId, focusChannelId, include );
		}

		public ReportBase( string userId, Guid focusChannelId, IncludeSet include, PeriodKind periodKind, GroupingValue value )
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = Localization.GetCulture();
			_GroupingValue = value;
			FillPeriodData( periodKind );
			FillReportBaseData( userId, focusChannelId, include );
		}


		public ReportBase( string userId, Guid focusChannelId, IncludeSet include, GroupingValue value, DateTime dateFrom, DateTime dateTo )
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = Localization.GetCulture();
			_GroupingValue = value;
			_Period = new Period( dateFrom, dateTo );
			FillReportBaseData( userId, focusChannelId, include );
		}

		public ReportBase( string userId, GroupingValue value, PeriodInfo period )
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = Localization.GetCulture();
			_GroupingValue = value;
			FillPeriodData( period );
		}

		public ReportBase( string userId, Guid focusChannelId, IncludeSet include )
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = Localization.GetCulture();
			FillReportBaseData( userId, focusChannelId, include );
		}

		private void FillPeriodData( PeriodInfo period )
		{
			_Period = new Period( period );
			PeriodStart = _Period.CurrentStart.ToShortDateString();
			PeriodEnd = _Period.CurrentEnd.AddDays( -1 ).ToShortDateString();
			PreviousStart = _Period.PreviousStart.ToShortDateString();
			PreviousEnd = _Period.PreviousEnd.AddDays( -1 ).ToShortDateString();
		}

		private void FillPeriodData( PeriodKind periodKind )
		{
			_Period = new Period( periodKind, DateTime.UtcNow );
			PeriodStart = _Period.CurrentStart.ToShortDateString();
			PeriodEnd = _Period.CurrentEnd.AddDays( -1 ).ToShortDateString();
			PreviousStart = _Period.PreviousStart.ToShortDateString();
			PreviousEnd = _Period.PreviousEnd.AddDays( -1 ).ToShortDateString();
		}

		private void FillReportBaseData( string userId, Guid focusChannelId, IncludeSet include )
		{
			_UserId = userId;
			_focusChannelId = focusChannelId;
			_getChannelIds( userId, focusChannelId, include );
			_AdvertiserRows = new Dictionary<Guid, AdvertiserRowBase>();
		}

		protected void _getChannelIds( string userId, Guid focusChannelId, IncludeSet include )
		{
			switch ( include ) {
				case IncludeSet.GroupProperties: {
						var group = Group.GetByChannel( userId, focusChannelId );
						if ( group != null ) {
							Channels = Channel.Get( group.ChannelIds );
							var focusChannel = Channels.Single( c => c.Id == focusChannelId );

							// Only keep channels with the same media type as the focus channel
							Channels = Channels.Where( c => c != focusChannel && c.MediaType == focusChannel.MediaType ).ToList();
							Channels.Insert( 0, focusChannel ); // Have focus channel at first position

							_ChannelIds = Channels.Select( c => c.Id ).ToList();
						}
						break;
					}
				case IncludeSet.Competitors: {
						_ChannelIds = Competitor.GetFor( userId, focusChannelId );
						_ChannelIds.Insert( 0, focusChannelId );

						Channels = Channel.Get( _ChannelIds );
						Channels = _ChannelIds.Select( channelId => Channels.First( c => c.Id == channelId ) ).ToList(); // Keep channel order
						break;
					}
				case IncludeSet.None: {
					_ChannelIds = new List<Guid> { _focusChannelId };
					Channels = Channel.Get( _ChannelIds );
					break;

				}
			}
		}

		protected string _valueColumn()
		{
			switch ( _GroupingValue ) {
				case GroupingValue.Count:
					return "play_count";
				case GroupingValue.Duration:
					return "duration";
				case GroupingValue.Spend:
					return "earns";
				default:
					throw new ArgumentOutOfRangeException( "Unexpected grouping value '" + _GroupingValue + "'" );
			}
		}

		/// <summary>
		/// Returns a lambda expression taking rows and doing the computation described by the ValueEnum
		/// </summary>
		/// <returns></returns>
		protected Func<decimal, ComputationResult> _GetValueAdapter()
		{
			switch ( _GroupingValue ) {
				case GroupingValue.Count:
					return ( decimal value ) => new Quantity( (int)value );
				case GroupingValue.Duration:
					return ( decimal value ) => new Duration( value );
				case GroupingValue.Spend:
					return ( decimal value ) => new Currency( value );
				default:
					throw new ArgumentOutOfRangeException( "_GroupingValue" );
			}
		}

		protected void _getAdvertiserNames( string userId )
		{
			var keyAccounts = KeyAccount.GetByUser( userId );
			var advertiserDic = Company.Get( _AdvertiserRows.Keys ).ToDictionary( a => a.Id );

			foreach ( var advertiserRow in _AdvertiserRows ) {
				advertiserRow.Value.AdvertiserName = advertiserDic[advertiserRow.Key].Name;
				advertiserRow.Value.AdvertiserId = advertiserRow.Key.ToString();
				advertiserRow.Value.IsKeyAccount = keyAccounts.Any( ba => !ba.IsBrand && ba.Id == advertiserRow.Key );
			}
		}
	}
	public class AdvertiserRowBase
	{
		public string AdvertiserName;
		public string AdvertiserId;
		public bool IsKeyAccount;
		public int CurrentRank;
		public decimal CurrentTotal;
		public ChannelValueBase[] ChannelValues;
	}
	public class ChannelValueBase
	{
		public decimal Total;
	}

	public class ReportDbRecord
	{
		public ReportDbRecord()
		{
		}

		public ReportDbRecord( Guid id, string name, decimal value )
		{
			Id = id;
			Name = name;
			Value = value;
		}

		public ReportDbRecord( Guid id, string name, decimal value, int playHour )
		{
			Id = id;
			Name = name;
			Value = value;
			PlayHour = playHour;
		}

		public ReportDbRecord( Guid id, string name, decimal value, DateTime playDate, int playHour )
		{
			Id = id;
			Name = name;
			Value = value;
			PlayDate = playDate;
			PlayHour = playHour;
		}

		public Guid Id { get; set; }
		public string Name { get; set; }
		public decimal Value { get; set; }

		public DateTime PlayDate { get; set; }
		public int PlayHour { get; set; }
	}

	public class Chart<T> where T : class
	{
		public Chart()
		{
			Data = new List<T>();
		}

		public Chart(string name, decimal maxValue)
		{
			Name = name;
			MaxChartValue = maxValue;
			Data = new List<T>();
		}

		public string Name { get; set; }
		public decimal MaxChartValue { get; set; }
		public List<T> Data { get; set; }
	}

	public class ChartRecord
	{
		public ChartRecord( Guid id, string name, decimal value )
		{
			Id = id;
			Name = name;
			Value = value;
		}

		public Guid Id { get; set; }
		public string Name { get; set; }
		public decimal Value { get; set; }
	}

	public class GenericReportTable
	{
		public GenericReportTable( string name = "" )
		{
			Headers = new List<List<string>>();
			Rows = new List<List<string>>();
			Name = name;
		}

		public string Name { get; set; }
		public List<List<string>> Headers { get; set; }
		public List<List<string>> Rows { get; set; }

	}

	public class PointValue
	{
		public string Key;
		public DateTime Date;
		public decimal Value;
	}

	public class LineChartModel
	{
		public LineChartModel()
		{
			Values = new List<PointValue>();
		}

		public LineChartModel( string key )
		{
			Values = new List<PointValue>();
			Key = key;
		}

		public string Key { get; set; }

		public List<PointValue> Values;

		public decimal Total { get; set; }

	}

}