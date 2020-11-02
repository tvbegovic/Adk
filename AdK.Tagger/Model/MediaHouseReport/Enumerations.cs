using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public enum GroupingValue
	{
		Count,
		Duration,
		Spend,
		Space
	}

	public enum IncludeSet
	{
		None,
		Competitors,
		GroupProperties,
		MediaType
	}

	public enum DayOfWeekRange
	{
		All = 10,
		M_F = 9,
		Weekends = 8,
		Monday = DayOfWeek.Monday,
		Tuesday = DayOfWeek.Tuesday,
		Wednesday = DayOfWeek.Wednesday,
		Thursday = DayOfWeek.Thursday,
		Friday = DayOfWeek.Friday,
		Saturday = DayOfWeek.Saturday,
		Sunday = DayOfWeek.Sunday
	}

	public enum DayPartType
	{
		AllDay,
		MyDayParts
	}

	public enum BrandOrAdvertiser
	{
		Brand = 0,
		Advertiser = 1
	}

	public enum BrandAdvertiserOrChannel
	{
		Brand = 0,
        Advertiser = 1,
		Channel = 2
	}

    public enum Media
    {
        All = 0,
        Radio = 1,  
        Tv = 2
    }

    public enum ChannelOrDate
    {
        Channel = 0,
        Date = 1
    }
}