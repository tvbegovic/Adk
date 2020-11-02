using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdK.Tagger.Model.Reporting
{
	public static class PredefinedReports
	{
		public static Reporter SpotCountByBrand()
		{
			return new Reporter
			{
				Title = "Spot Count by Brand",

				// Filters
				//Industries = Industry.All(),

				// Criteria
				Criteria = new List<CriteriaInfo>
				{
					new CriteriaInfo
					{
						Criteria = CriteriaEnum.Media
					},
					new CriteriaInfo
					{
						Criteria = CriteriaEnum.Industry
					},
					new CriteriaInfo
					{
						Criteria = CriteriaEnum.Brand,
						Informative = new List<CriteriaEnum> { CriteriaEnum.Company }
					}
				},

				// Values
				Values = new List<ValueEnum> { ValueEnum.SpotCount },
				OrderingValue = ValueEnum.SpotCount,
				OrderDescending = true
			};
		}
		public static Reporter CompanyShareOfRtvSpend()
		{
			return new Reporter
			{
				Title = "Company Share of RTV Spend",

				// Filters
				//Industries = Industry.All(),

				// Criteria
				Criteria = new List<CriteriaInfo>
				{
					new CriteriaInfo
					{
						Criteria = CriteriaEnum.Industry
					},
					new CriteriaInfo
					{
						Criteria = CriteriaEnum.Company,
						//Informative = new List<CriteriaEnum>(){CriteriaEnum.Industry}
					}
				},

				// Values
				Values = new List<ValueEnum> { ValueEnum.Expense },
				OrderingValue = ValueEnum.Expense,
				OrderDescending = true
			};
		}
		public static Reporter BrandShareOfRtvSpend()
		{
			return new Reporter
			{
				Title = "Brand Share of RTV Spend",

				// Filters
				//Industries = Industry.All(),

				// Criteria
				Criteria = new List<CriteriaInfo>
				{
					new CriteriaInfo
					{
						Criteria = CriteriaEnum.Brand,
						//Informative = new List<CriteriaEnum>(){CriteriaEnum.Company}
					}
				},

				// Values
				Values = new List<ValueEnum> { ValueEnum.Expense },
				OrderingValue = ValueEnum.Expense,
				OrderDescending = true
			};
		}
		public static Reporter RtvSpendByChannelByAdvertiser()
		{
			return new Reporter
			{
				Title = "RTV Spend by Channel by Advertiser",

				// Filters
				//Industries = Industry.All(),

				// Criteria
				Criteria = new List<CriteriaInfo>
				{
					new CriteriaInfo
					{
						Criteria = CriteriaEnum.Company,
						//Informative = new List<CriteriaEnum>(){CriteriaEnum.Company}
					},
					new CriteriaInfo
					{
						Criteria = CriteriaEnum.Channel
					}
				},
				Pivot = true,

				// Values
				Values = new List<ValueEnum> { ValueEnum.Expense },
				OrderingValue = ValueEnum.Expense,
				OrderDescending = true
			};
		}

		public static Reporter[] GetAll()
		{
			return new Reporter[] {
				SpotCountByBrand(),
				CompanyShareOfRtvSpend(),
				BrandShareOfRtvSpend(),
				RtvSpendByChannelByAdvertiser()
			};
		}
	}
}