using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AdK.Tagger.Model.Reporting
{
	public class Reporter
	{
		public string Title;

		#region Filters
		public DateRange Range;
		public DateRange Range2;
		public RangeInterpolation Interpolation;
		public List<DateRange> GetRanges()
		{
			var ranges = new List<DateRange>();

			switch (Interpolation)
			{
				case RangeInterpolation.None:
					ranges.Add(Range);
					break;
				case RangeInterpolation.Compare:
					ranges.Add(Range);
					ranges.Add(Range2);
					break;
				case RangeInterpolation.ByWeek:
					DateTime week = Range.Start.Value;
					while (week < Range2.End)
					{
						ranges.Add(DateRange.Week(week));
						week = week.AddDays(7);
					}
					break;
				case RangeInterpolation.ByMonth:
					DateTime month = Range.Start.Value;
					while (month < Range2.End)
					{
						ranges.Add(DateRange.Month(month));
						month = month.AddMonths(1);
					}
					break;
			}

			return ranges;
		}

		public enum RangeInterpolation
		{
			None = 0,
			Compare = 1,
			ByWeek = 2,
			ByMonth = 3
		}

		public List<Industry> Industries;
		public List<Company> Companies;
		public List<Brand> Brands;
		public List<Channel> Channels;
		#endregion

		#region Criteria
		public List<CriteriaInfo> Criteria;
		public bool Pivot; // Should the last criteria be displayed as columns?
		#endregion

		#region Values
		public List<ValueEnum> Values;
		public ValueEnum OrderingValue;
		public bool OrderDescending;
		#endregion

		/// <summary>
		/// When multiple criteria are selected, they can have common info criteria.
		/// Remove duplicates as they would display the same info twice.
		/// </summary>
		public void RemoveDuplicateCriteriaInfo()
		{
			for (int i = 1; i < Criteria.Count; ++i)
			{
				var takenCriteria = Criteria.GetRange(0, i).SelectMany(c => c.Informative).Distinct();
				Criteria[i].Informative.RemoveAll(criteriaInfo => takenCriteria.Contains(criteriaInfo));
			}
		}
		/// <summary>
		/// The user may have selected ordering based on a value that is not selected.
		/// As this is not possible, select any value that is selected.
		/// </summary>
		public void CorrectWrongOrderValue()
		{
			if (!Values.Contains(OrderingValue))
				OrderingValue = Values.First();
		}

		public void Prepare()
		{
			if (Range2 == null)
				Interpolation = RangeInterpolation.None;
			if (Range == null)
				Range = DateRange.LastWeek();

			if (!Criteria.Any())
				Criteria.Add(new CriteriaInfo { Criteria = CriteriaEnum.Industry });
			RemoveDuplicateCriteriaInfo();

			if (Criteria.Count < 2 || Interpolation != RangeInterpolation.None)
				Pivot = false;

			if (!Values.Any())
				Values.Add(ValueEnum.SpotCount);
			CorrectWrongOrderValue();
		}
	}

	public class Report
	{
		public string Title;
		public Reporter Reporter;

		public List<ValueEnum> ValueEnums;
		// In case of a pivot, row PivotValues are indexed by PivotTitles to avoid repeating them
		public List<string> PivotTitles;

		public List<ReportRow> Rows;

		internal void Build(List<ReportQueryRow> rows)
		{
			if (Reporter.Pivot)
				PivotTitles = rows
					.Select(Model.Reporting.ReportQueryRow.GetKeySelector(Reporter.Criteria.Last().Criteria))
					.Distinct()
					.OrderBy(s => s.TrimStart())
					.ToList();
			else if (Reporter.Interpolation != Reporter.RangeInterpolation.None)
				PivotTitles = rows
					.Select(row => row.Range)
					.Distinct()
					.OrderBy(s => s.TrimStart())
					.ToList();

			Rows = ReportQueryRow.ForReport(this, rows);
		}

		public static Report GetReport(Model.Reporting.Reporter reporter)
		{
			reporter.Prepare();

			var rows = reporter
				.GetRanges()
				.SelectMany(range => _GetRows(reporter, range))
				.ToList();

			System.Threading.Thread.CurrentThread.CurrentCulture = Localization.GetCulture();

			var report = new Report
			{
				Reporter = reporter,
				ValueEnums = reporter.Values
			};
			report.Build(rows);

			return report;
		}
		private static List<ReportQueryRow> _GetRows(Reporter reporter, DateRange range)
		{
			var rows = new List<ReportQueryRow>();

			using (var conn = Database.Get())
			{
				MySqlCommand c = conn.CreateCommand();

				string query = @"
                select c.country, c.city, c.media_type, c.media_sub_type, 
                  m.match_occurred, 'daypart', 'day_of_week',
                  c.station_name,
                  a.company_name, b.brand_name, 
                  i.industry_name,  ac.category_name,
                  s.title, p.product_name, ROUND(s.duration) as duration,
                  cast((m.match_end - m.match_start) * pd.pps as decimal(19,4)) as earns,
                  m.earns_problem,
                  s.pksid as media_id,
                  s.campaign, s.message_type 
                  from matches m 
                  left join songs s on s.id = m.song_id
                  left join products p on p.id = s.product_id
                  left join channels c on c.id = m.channel_id
                  left join brands b on b.id = s.brand_id
                  left join advertisers a on a.id = b.advertiser_id
                  left join ad_categories ac on ac.id = s.category_id
                  left join industries i on i.id = b.industry_id
                  left join pricedefs pd 
                    on pd.product_id = s.product_id 
                        and c.id = pd.channel_id
                        and pd.hour = hour(m.match_occurred) 
                        and pd.dow = weekday(m.match_occurred)
                where s.duration*.7 < m.match_end-m.match_start and s.deleted=0 ";
				if (range.Start.HasValue)
				{
					query += "and m.match_occurred >= @start ";
				}
				if (range.End.HasValue)
				{
					query += "and m.match_occurred < @end ";
				}
				if (reporter.Industries != null && reporter.Industries.Any())
				{
					query += "and b.industry_id in (" + string.Join(", ", reporter.Industries.Select(i => string.Concat("'", i.Id, "'"))) + ") ";
				}
				if (reporter.Companies != null && reporter.Companies.Any())
				{
					query += "and a.id in (" + string.Join(", ", reporter.Companies.Select(i => string.Concat("'", i.Id, "'"))) + ") ";
				}
				if (reporter.Channels != null && reporter.Channels.Any())
				{
					query += "and c.id in (" + string.Join(", ", reporter.Channels.Select(i => string.Concat("'", i.Id, "'"))) + ") ";
				}
				query += "order by m.match_occurred";
				c.CommandText = query;

				if (range.Start.HasValue)
				{
					c.Parameters.AddWithValue("@start", range.Start.Value);
				}
				if (range.End.HasValue)
				{
					c.Parameters.AddWithValue("@end", range.End.Value);
				}

				using (MySqlDataReader dr = c.ExecuteReader())
				{
					while (dr.Read())
					{
						var row = AsReportQueryRow(dr);
						row.Range = range.Name;
						rows.Add(row);
					}
				}
			}

			return rows;
		}
		private static ReportQueryRow AsReportQueryRow(MySqlDataReader dr)
		{
			var ci = new CultureInfo("en-US"); // For date formatting
			var matchOccured = dr.GetDateTime("match_occurred");

			var sMediaId = (string)dr["media_id"];
			var mediaId = new Guid();
			Guid.TryParse(sMediaId, out mediaId);

			return new ReportQueryRow
			{
				MediaType = dr.IsDBNull(2) ? null : dr.GetString("media_type"),
				MediaSubtype = dr.IsDBNull(3) ? null : dr.GetString("media_sub_type"),
				MatchOccured = matchOccured,
				DayPart = DayPart.FromHour(matchOccured.Hour),
				DayOfWeek = matchOccured.ToString("dddd", ci),
				Channel = dr.IsDBNull(7) ? null : dr.GetString("station_name"),
				Company = dr.IsDBNull(8) ? null : dr.GetString("company_name"),
				Brand = dr.IsDBNull(9) ? null : dr.GetString("brand_name"),
				Industry = dr.IsDBNull(10) ? null : dr.GetString("industry_name"),
				Category = dr.IsDBNull(11) ? null : dr.GetString("category_name"),
				SampleTitle = dr.IsDBNull(12) ? null : dr.GetString("title"),
				Product = dr.IsDBNull(13) ? null : dr.GetString("product_name"),
				Duration = dr.GetInt32("duration"),
				Earns = dr.IsDBNull(15) ? 0 : dr.GetDecimal("earns"),
				EarnsProblem = dr.IsDBNull(16) ? null : dr.GetString("earns_problem"),
				MediaId = mediaId,
				Campaign = dr.IsDBNull(18) ? null : dr.GetString("campaign"),
				MessageType = dr.IsDBNull(19) ? null : dr.GetString("message_type")

				//0: c.country, c.city, c.media_type, c.media_sub_type, 
				//4: m.match_occurred, 'daypart', 'day_of_week',
				//7: c.station_name,
				//8: a.company_name, b.brand_name, 
				//10: i.industry_name,  ac.category_name,
				//12: s.title, p.product_name, ROUND(s.duration),
				//15: cast((m.match_end - m.match_start) * pd.pps as decimal(19,4)) as earns,
				//16: m.earns_problem,
				//17: s.pksid as media_id,
				//18: s.campaign, s.message_type 
			};
		}
	}

	public class ReportRow
	{
		public string Title;
		public List<string> InformativeValues;
		public List<object> Values;

		// In case of a pivot, SubRows is not used but PivotValues instead
		public List<PivotValue> PivotValues;

		public List<ReportRow> SubRows;
	}
	public class PivotValue
	{
		public int Index;
		public List<object> Values;
	}

	public class ReportQueryRow
	{
		#region Properties
		public string MediaType;
		public string MediaSubtype;
		public DateTime MatchOccured;
		public string DayPart;
		public string DayOfWeek;
		public string Channel;
		public string Company;
		public string Brand;
		public string Industry;
		public string Category;
		public string SampleTitle;
		public string Product;
		public int Duration;
		public decimal Earns;
		public string EarnsProblem;
		public Guid MediaId;
		public string Campaign;
		public string MessageType;
		/// <summary>
		/// In case of range compare or interpolation, contains the range name (Week N or Month)
		/// </summary>
		public string Range;
		#endregion

		internal static List<ReportRow> ForReport(Report report, List<ReportQueryRow> rows)
		{
			return Apply(report, rows, -1);
		}

		public static List<ReportRow> Apply(Report report, IEnumerable<ReportQueryRow> rows, int level)
		{
			level++;
			if (report.Reporter.Criteria.Count <= level)
				return new List<ReportRow>();

			var criteriaInfo = report.Reporter.Criteria[level];
			bool applyPivot = report.Reporter.Pivot && level + 2 == report.Reporter.Criteria.Count;
			bool applyInterpolation = report.Reporter.Interpolation != Reporter.RangeInterpolation.None;

			var unorderedRows = rows
				.GroupBy(ReportQueryRow.GetKeySelector(criteriaInfo.Criteria)) // Group by a field value determined by the selected criteria
				.Select(g =>
					{
						var reportRow = new ReportRow
						{
							Title = g.Key,
							InformativeValues = GetInformativeValues(g, criteriaInfo.Informative),
							Values = GetComputedValues(g, report.Reporter.Values)
						};
						if (applyPivot)
							reportRow.PivotValues = ApplyPivot(report, g);
						else
						{
							if (applyInterpolation)
								reportRow.PivotValues = ApplyPivot(report, g); // these values are used for sorting and not displayed
							reportRow.SubRows = Apply(report, g, level);
						}
						return reportRow;
					}
				);

			List<ReportRow> result;
			if (report.Reporter.Criteria[level].OrderDescending.HasValue)
			{
				var orderedRows = unorderedRows
					.OrderByWithDirection(row => row.Title, report.Reporter.Criteria[level].OrderDescending.Value);
				result = orderedRows.ToList();
			}
			else
			{
				int orderIndex = report.Reporter.Values.IndexOf(report.Reporter.OrderingValue);
				var orderedRows = unorderedRows
					.OrderByWithDirection(row => row.Values.ElementAt(orderIndex), report.Reporter.OrderDescending);
				result = orderedRows.ToList();
			}

			// Once the values have been sorted, convert them to strings
			result.ForEach(row => row.Values = row.Values.Select(v => (object)v.ToString()).ToList());

			return result;
		}
		public static List<PivotValue> ApplyPivot(Report report, IEnumerable<ReportQueryRow> rows)
		{
			// Title to index within the PivotTitles list dictionary
			var titleLookup = report.PivotTitles
				.Select((title, index) => new { Title = title, Index = index })
				.ToDictionary(a => a.Title, a => a.Index);

			var keySelector = report.Reporter.Pivot ?
				GetKeySelector(report.Reporter.Criteria.Last().Criteria) :
				(ReportQueryRow row) => row.Range;

			return rows
				.GroupBy(keySelector)
				.Select(g => new PivotValue
				{
					Index = titleLookup[g.Key],
					Values = GetComputedValues(g, report.Reporter.Values).Select(v => (object)v.ToString()).ToList()
				})
				.ToList();
		}

		private static List<string> GetInformativeValues(IEnumerable<ReportQueryRow> rows, List<CriteriaEnum> informativeCriteria)
		{
			if (informativeCriteria == null)
				return new List<string>();

			// As informative values are controlled to be identical for all the provided rows, we take the first on
			var row = rows.First();
			return informativeCriteria
				.Select(criteria => GetKeySelector(criteria)(row))
				.ToList();
		}

		private static List<object> GetComputedValues(IEnumerable<ReportQueryRow> rows, List<ValueEnum> valueEnums)
		{
			return valueEnums
				.Select(ve => GetComputationSelector(ve)(rows))
				.ToList();
		}

		/// <summary>
		/// Returns a lambda expression taking a row and returning the name of the grouping criteria
		/// </summary>
		/// <param name="criteria"></param>
		/// <returns></returns>
		public static Func<ReportQueryRow, string> GetKeySelector(CriteriaEnum criteria)
		{
			switch (criteria)
			{
				case CriteriaEnum.Media:
					return (ReportQueryRow row) => row.MediaType;
				case CriteriaEnum.Brand:
					return (ReportQueryRow row) => row.Brand;
				case CriteriaEnum.Company:
					return (ReportQueryRow row) => row.Company;
				case CriteriaEnum.Channel:
					return (ReportQueryRow row) => row.Channel;
				case CriteriaEnum.Industry:
					return (ReportQueryRow row) => row.Industry;
				case CriteriaEnum.Category:
					return (ReportQueryRow row) => row.Category;
				case CriteriaEnum.DayOfWeek:
					return (ReportQueryRow row) => row.DayOfWeek;
				case CriteriaEnum.DayPart:
					return (ReportQueryRow row) => row.DayPart;
				case CriteriaEnum.Campaign:
					return (ReportQueryRow row) => row.Campaign;
				case CriteriaEnum.SampleTitle:
					return (ReportQueryRow row) => row.SampleTitle;
				default:
					throw new ArgumentOutOfRangeException("criteria");
			}
		}

		/// <summary>
		/// Returns a lambda expression taking rows and doing the computation described by the ValueEnum
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static Func<IEnumerable<ReportQueryRow>, object> GetComputationSelector(ValueEnum value)
		{
			switch (value)
			{
				case ValueEnum.SpotCount:
					return (IEnumerable<ReportQueryRow> rows) => new Quantity(rows.Any() ? rows.Count() : 0);
				case ValueEnum.AirTime:
					return (IEnumerable<ReportQueryRow> rows) => new Duration(rows.Any() ? rows.Sum(row => row.Duration) : 0);
				case ValueEnum.AverageSpotLength:
					return (IEnumerable<ReportQueryRow> rows) => new Duration(rows.Any() ? Math.Round((decimal)rows.Average(row => row.Duration), 1) : 0);
				case ValueEnum.Expense:
					return (IEnumerable<ReportQueryRow> rows) => new Currency(rows.Any() ? rows.Sum(row => row.Earns) : 0);
				default:
					throw new ArgumentOutOfRangeException("value");
			}
		}
	}

	public class DateRange
	{
		public DateTime? Start;
		public DateTime? End;
		public string Name;

		public static DateRange LastWeek()
		{
			var now = DateTime.UtcNow.Date;
			return Week(now.AddDays(-((int)now.DayOfWeek + 7)));
		}
		public static DateRange LastMonth()
		{
			var now = DateTime.UtcNow.Date;
			var end = new DateTime(now.Year, now.Month, 1);
			return Month(end.AddMonths(-1));
		}
		public static DateRange Since(DateTime sinceDate)
		{
			return new DateRange
			{
				Start = sinceDate.Date,
				Name = string.Format("Since {0}", sinceDate.Date.ToShortDateString())
			};
		}

		public override string ToString()
		{
			return string.Concat(
				Start.HasValue ? Start.Value.ToShortDateString() : "",
				" -> ",
				End.HasValue ? End.Value.ToShortDateString() : ""
			);
		}
		public static DateRange Week(DateTime start)
		{
			return new DateRange
			{
				Start = start,
				End = start.AddDays(7),
				Name = string.Format("{0} Week {1}", start.Year, GetIso8601WeekOfYear(start))
			};
		}
		public static int GetIso8601WeekOfYear(DateTime time)
		{
			// Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
			// be the same week# as whatever Thursday, Friday or Saturday are,
			// and we always get those right
			DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
			if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
			{
				time = time.AddDays(3);
			}

			// Return the week of our adjusted day
			return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
		}

		public static DateRange Month(DateTime start)
		{
			return new DateRange
			{
				Start = start,
				End = start.AddMonths(1),
				Name = string.Format("{0} {1}", start.Year, CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(start.Month))
			};
		}

		public static DateRange GetDataTimeRange()
		{
			var range = new DateRange();

			using (var conn = Database.Get())
			{
				MySqlCommand c = conn.CreateCommand();
				c.CommandText = @"
				select min(date(m.match_occurred)) as from_date, max(date(m.match_occurred)) as to_date
                  from matches m 
                  left join songs s on s.id = m.song_id
                where s.duration*.7 < m.match_end-m.match_start and s.deleted=0";

				using (var dr = c.ExecuteReader())
				{
					if (dr.Read())
					{
						range.Start = dr.GetDateTime("from_date");
						range.End = dr.GetDateTime("to_date");
					}
				}
			}

			return range;
		}
	}

	public static class DayPart
	{
		public static string FromHour(int hour)
		{
			string datepart = "";
			switch (hour)
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
					datepart = "7 Overnight";
					break;
				case 6:
				case 7:
				case 8:
					datepart = "1 Morning Drive";
					break;
				case 9:
				case 10:
				case 11:
					datepart = "2 Mid Morning";
					break;
				case 12:
				case 13:
				case 14:
					datepart = "3 Afternoon";
					break;
				case 15:
				case 16:
				case 17:
					datepart = "4 Afternoon Drive";
					break;
				case 18:
				case 19:
				case 20:
					datepart = "5 Evening";
					break;
				case 21:
				case 22:
				case 23:
					datepart = "6 Late Night";
					break;
			}
			return datepart;
		}
	}

	public enum CriteriaEnum
	{
		Media = 0,
		Industry = 1,
		Company = 2,
		Brand = 3,
		Channel = 4,
		DayOfWeek = 5,
		DayPart = 6,
		Category = 7,
		Campaign = 8,
		SampleTitle = 9
	}
	public class CriteriaInfo
	{
		public CriteriaEnum Criteria;
		// Criteria that are parent of the main Criteria (can be deduced from it univocally)
		// Exemple: Criteria = Brand, Informative = Company, Industry
		public List<CriteriaEnum> Informative = new List<CriteriaEnum>();
		/// <summary>
		/// false: ascending order, true: descending order, null: leave ordering to value column
		/// </summary>
		public bool? OrderDescending;

		public static Guid GetCriteriaValueId(int criteriaId, string name)
		{
			switch ((CriteriaEnum)criteriaId)
			{
				case CriteriaEnum.Brand:
					return Brand.GetBrandId(name);
				case CriteriaEnum.Company:
					return Company.GetAdvertiserId(name);
				case CriteriaEnum.Industry:
					return Industry.GetIndustryId(name);
				default:
					return Guid.Empty;
			}
		}
	}

	public enum ValueEnum
	{
		SpotCount = 0,
		AirTime = 1,
		AverageSpotLength = 2,
		MediaMix = 3,
		Expense = 4
	}

	public class Industry
	{
		public Guid Id;
		public string Name;
		public List<Company> AllCompanies;

		public Industry() { }

		private static List<Industry> _All;
		internal static List<Industry> All()
		{
			if (_All == null)
				_All = GetAllIndustries();
			return _All;
		}

		public static Guid GetIndustryId(string industryName)
		{
			var foundIndustries = Database.ListFetcher<Guid>("SELECT id FROM industries WHERE industry_name = @industry_name",
			   dr => dr.GetNullableGuid("id"), new object[] { "@industry_name", industryName });
			return foundIndustries.FirstOrDefault();
		}
		public static List<Industry> GetAllIndustries()
		{
			return Database.ListFetcher<Industry>("SELECT id, industry_name FROM industries",
				dr => new Industry
				{
					Id = dr.GetNullableGuid("id"),
					Name = dr.GetNullableString("industry_name")
				});
		}
	}

	public class Company
	{
		public Guid Id;
		public string Name;
		public Industry Industry;
		public List<Brand> AllBrands;

		public static Guid GetAdvertiserId(string advertiserName)
		{
			var foundAdvertisers = Database.ListFetcher<Guid>("SELECT id FROM advertisers WHERE company_name = @company_name",
			   dr => dr.GetNullableGuid("id"), new object[] { "@company_name", advertiserName });
			return foundAdvertisers.FirstOrDefault();
		}
	}

	public class Brand
	{
		public Guid Id;
		public string Name;
		public Company Company;

		public static Guid GetBrandId(string brandName)
		{
			var foundBrands = Database.ListFetcher<Guid>("SELECT id FROM brands WHERE brand_name = @brand_name",
			   dr => dr.GetNullableGuid("id"), new object[] { "@brand_name", brandName });
			return foundBrands.FirstOrDefault();
		}
	}

	public class Channel
	{
		public Guid Id;
		public string Name;
	}
}