using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class PeriodInfo
	{
		public PeriodKind PeriodKind;
		public DateTime DateFrom;
		public DateTime DateTo;
	}

	public enum PeriodKind
	{
		Last7Days,
		MonthToDate,
		Last30Days,
		LastFullMonth,
		QuarterToDate,
		LastFullQuarter,
		YearToDate,
		LastFullYear,
		Last2Weeks,
		Last3Weeks,
		Last4Weeks,
		Last5Weeks,
		Last6Weeks,
		Last8Weeks,
		WeekToDate,
		LastFullWeek,
		Last2Quarters,
		Last3Quarters,
		Last4Quarters,
		CustomRange
	}


	public class Period
	{
		public DateTime CurrentStart;
		public DateTime CurrentEnd;
		public DateTime PreviousStart;
		public DateTime PreviousEnd;

		public Period( PeriodInfo info )
			: this( info.PeriodKind, DateTime.UtcNow )
		{
			if ( info.PeriodKind == PeriodKind.CustomRange ) {
				CurrentStart = info.DateFrom;
				CurrentEnd = info.DateTo.AddDays( 1 ); //Include last day

				var timeSpan = CurrentEnd - CurrentStart;

				PreviousEnd = CurrentEnd.AddDays( -timeSpan.Days );
				PreviousStart = CurrentStart.AddDays( -timeSpan.Days );
			}
		}

		public Period( DateTime dateFrom, DateTime dateTo )
		{
			CurrentStart = dateFrom;
			CurrentEnd = dateTo;
		}

		public Period( PeriodKind kind, DateTime date )
		{
			switch ( kind ) {
				case PeriodKind.Last7Days:
					_LastWeeks( 1, date );
					break;
				case PeriodKind.Last2Weeks:
					_LastWeeks( 2, date );
					break;
				case PeriodKind.Last3Weeks:
					_LastWeeks( 3, date );
					break;
				case PeriodKind.Last4Weeks:
					_LastWeeks( 4, date );
					break;
				case PeriodKind.Last5Weeks:
					_LastWeeks( 5, date );
					break;
				case PeriodKind.Last6Weeks:
					_LastWeeks( 6, date );
					break;
				case PeriodKind.Last8Weeks:
					_LastWeeks( 8, date );
					break;
				case PeriodKind.Last30Days:
					CurrentEnd = date.Date;
					CurrentStart = CurrentEnd.AddDays( -30 );
					PreviousEnd = CurrentEnd.AddDays( -30 );
					PreviousStart = CurrentStart.AddDays( -30 );
					break;
				case PeriodKind.MonthToDate:
					CurrentEnd = date.Date;
					CurrentStart = new DateTime(CurrentEnd.Year, CurrentEnd.Month, 1);
					CurrentEnd = CurrentEnd.AddDays(1); //To include this day in filter
					PreviousEnd = CurrentEnd.AddMonths( -1 );
					PreviousStart = CurrentStart.AddMonths( -1 );
					break;
				case PeriodKind.LastFullMonth:
					CurrentEnd = date.Date.AddDays( 1 - date.Date.Day );
					CurrentStart = CurrentEnd.AddMonths( -1 );
					PreviousEnd = CurrentEnd.AddMonths( -1 );
					PreviousStart = CurrentStart.AddMonths( -1 );
					break;
				case PeriodKind.QuarterToDate:
					CurrentEnd = date.Date;
					CurrentStart = CurrentEnd.AddDays( 1 - CurrentEnd.Day ).AddMonths( -((CurrentEnd.Month - 1) % 3) );
					PreviousEnd = CurrentEnd.AddMonths( -3 );
					PreviousStart = CurrentStart.AddMonths( -3 );
					break;
				case PeriodKind.LastFullQuarter:
					CurrentEnd = date.Date.AddDays( 1 - date.Date.Day ).AddMonths( -((date.Date.Month - 1) % 3) );
					CurrentStart = CurrentEnd.AddMonths( -3 );
					PreviousEnd = CurrentEnd.AddMonths( -3 );
					PreviousStart = PreviousEnd.AddMonths( -3 );
					break;
				case PeriodKind.YearToDate:
					CurrentEnd = date.Date;
					CurrentStart = CurrentEnd.AddDays( 1 - CurrentEnd.Day ).AddMonths( 1 - CurrentEnd.Month );
					PreviousEnd = CurrentEnd.AddYears( -1 );
					PreviousStart = CurrentStart.AddYears( -1 );
					break;
				case PeriodKind.LastFullYear:
					CurrentEnd = date.Date.AddDays( 1 - date.Date.Day ).AddMonths( 1 - date.Date.Month );
					CurrentStart = CurrentEnd.AddYears( -1 );
					PreviousEnd = CurrentEnd.AddYears( -1 );
					PreviousStart = PreviousEnd.AddYears( -1 );
					break;
				case PeriodKind.WeekToDate:
					_FullWeek( date );
					CurrentEnd = date.Date;
					break;
				case PeriodKind.LastFullWeek:
					_FullWeek( date.AddDays( -7 ) );
					break;
				case PeriodKind.Last2Quarters:
					_Quarters( date, 2 );
					break;
				case PeriodKind.Last3Quarters:
					_Quarters( date, 3 );
					break;
				case PeriodKind.Last4Quarters:
					_Quarters( date, 4 );
					break;
				case PeriodKind.CustomRange:
					break;
				default:
					throw new ArgumentOutOfRangeException( "Period Kind '" + kind + "' not supported for Periods." );
			}
		}

		private void _LastWeeks( int n, DateTime date )
		{
			CurrentEnd = date.Date;
			CurrentStart = CurrentEnd.AddDays( -7 * n );
			PreviousEnd = CurrentEnd.AddDays( -7 * n );
			PreviousStart = CurrentStart.AddDays( -7 * n );
		}
		private void _FullWeek( DateTime date )
		{
			while ( date.DayOfWeek != DayOfWeek.Monday )
				date = date.AddDays( -1 );
			CurrentStart = date.Date;
			CurrentEnd = date.Date.AddDays( 7 );
			PreviousEnd = CurrentStart;
			PreviousStart = PreviousEnd.AddDays( -7 );
		}

		public void _Quarters( DateTime date, int quarters )
		{
			int monthsToSubstract = (quarters*-3) + 1;
			IEnumerable<DateTime> candidates = QuartersInYear( date.Year ).Union( QuartersInYear( date.Year - 1 ) );
			CurrentEnd = candidates.Where( d => d < date.Date ).OrderBy( d => d ).Last();
			DateTime begin = CurrentEnd.AddMonths( monthsToSubstract );
			CurrentEnd = CurrentEnd.AddDays( 1 ); //To include last day of month in query
			CurrentStart = new DateTime( begin.Year, begin.Month, 1 );
			PreviousEnd = CurrentStart.AddDays( -1 );
			DateTime previous = CurrentEnd.AddMonths( monthsToSubstract );
			PreviousStart = new DateTime( previous.Year, previous.Month, 1 );
		}

		static IEnumerable<DateTime> QuartersInYear( int year )
		{
			return new List<DateTime>()
            {
                new DateTime(year, 3, 31),
                new DateTime(year, 6, 30),
                new DateTime(year, 9, 30),
                new DateTime(year, 12, 31),
            };
		}
	}
}