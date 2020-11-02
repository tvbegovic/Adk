using DatabaseCommon;
using NodaTime;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AdK.Tagger.Model
{
	public static class Localization
	{
		private const string _Module = "Localization";

		#region Time Zone
		private const string DefaultTimeZoneName = "Europe/Zagreb";
		public static string TimeZoneName
		{
			get
			{
				return Settings.Get(_Module, "TimeZoneName", DefaultTimeZoneName);
			}
			set
			{
				Settings.Set(_Module, "TimeZoneName", value);
			}
		}
		public static NodaTime.DateTimeZone TimeZone
		{
			get
			{
				NodaTime.DateTimeZone tz;
				try
				{
					tz = NodaTime.DateTimeZoneProviders.Tzdb[TimeZoneName];
				}
				catch (NodaTime.TimeZones.DateTimeZoneNotFoundException)
				{
					tz = NodaTime.DateTimeZoneProviders.Tzdb[DefaultTimeZoneName];
				}
				return tz;
			}
		}
		public static int TimeZoneOffset
		{
			get
			{
				var tz = DateTimeZoneProviders.Tzdb[TimeZoneName];
				return (int)tz.GetUtcOffset(SystemClock.Instance.Now).ToTimeSpan().TotalMinutes;
			}
		}
		#endregion

		#region Now in local time zone
		public static NodaTime.ZonedDateTime Now
		{
			get
			{
				return NodaTime.SystemClock.Instance.Now.InZone(TimeZone);
			}
		}
		public static NodaTime.LocalDateTime NowLocal
		{
			get
			{
				return Now.LocalDateTime;
			}
		}
		#endregion

		#region CultureInfo
		public static string GetCultureName()
		{
			return Settings.Get(_Module, "CultureName");
		}
		public static void SetCultureName(string cultureName)
		{
			Settings.Set(_Module, "CultureName", cultureName);
		}
		public static CultureInfo GetCulture()
		{
			var ci = CultureInfo.InvariantCulture;
			string cultureName = GetCultureName();
			if (!string.IsNullOrWhiteSpace(cultureName))
				ci = CultureInfo.GetCultureInfo(cultureName);
			return ci;
		}
		#endregion

		#region Countries
		public static List<RegionInfo> GetCountries()
		{
			var countries = new List<RegionInfo>();
			foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
			{
				RegionInfo country = new RegionInfo(culture.LCID);
				if (!countries.Any(p => p.Name == country.Name))
					countries.Add(country);
			}
			return countries.OrderBy(p => p.EnglishName).ToList();
		}
		#endregion
}
public class GlobalConfiguration
	{
		public string Locale;
		public string TimeZone;
        public string UserDateFormat;


		public static GlobalConfiguration Get(TaggerUser user)
		{
			return new GlobalConfiguration
			{
				Locale = Localization.GetCultureName(),
				TimeZone = Localization.TimeZoneName,
                UserDateFormat = UserSettings.GetUserDateFormat(user.Id)
            };
		}

		public void Save(TaggerUser user)
		{
			Localization.SetCultureName(Locale);
			Localization.TimeZoneName = TimeZone;
            UserSettings.UpdateUserDateFormat(user.Id, UserDateFormat);
        }
	}
}