using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class Brandvertiser : ReportBase
	{
		public Guid Id;
		public bool IsBrand;
		public string Name;
		public List<Brand> Brands;
		public List<Guid> _BrandvertiserCategoryIds;
		public List<MediaTotal> TotalByMedia;
		public string FocusTotal;
		public decimal ShareOfIncludeGroup;
		public decimal ShareOfMediaGroup;
		public decimal ShareOfAllMedia;
		public CompetitivePositionSet CompetitivePosition;
        public PeriodToPeriod Shifts;
		public MediaMixSet MediaMix;

		public class MediaTotal
		{
			public string Media;
			private Func<decimal, Reporting.ComputationResult> _DisplayAdapter;

			public decimal Value;
			public string Display
			{
				get { return _DisplayAdapter(Value).ToString(); }
			}
			public MediaTotal(Func<decimal, Reporting.ComputationResult> displayAdapter, string media, decimal value)
			{
				_DisplayAdapter = displayAdapter;
				Media = media;
				Value = value;
			}
		}
		public class PeriodToPeriod
		{
			private List<Guid> _ChannelIds;
			private List<decimal> _CurrentPeriod;
			private List<decimal> _PreviousPeriod;
			private Func<decimal, Reporting.ComputationResult> _ValueAdapter;

			public List<string> Values
			{
				get
				{
					return Enumerable.Range(0, _ChannelIds.Count)
						.Select(i => _ValueAdapter(_CurrentPeriod[i] - _PreviousPeriod[i]).ToString())
						.ToList();
				}
			}
			public List<decimal?> Percent
			{
				get
				{
					return Enumerable.Range(0, _ChannelIds.Count)
						.Select(i => _PreviousPeriod[i] != 0 ?
							(_CurrentPeriod[i] - _PreviousPeriod[i]) / _PreviousPeriod[i] :
							(decimal?)null)
						.ToList();
				}
			}

			public PeriodToPeriod(List<Channel> channels, Func<decimal, Reporting.ComputationResult> valueAdapter)
			{
				_ChannelIds = channels.Select(c => c.Id).ToList();
				_CurrentPeriod = Enumerable.Range(0, _ChannelIds.Count).Select(i => 0M).ToList();
				_PreviousPeriod = Enumerable.Range(0, _ChannelIds.Count).Select(i => 0M).ToList();
				_ValueAdapter = valueAdapter;
			}

			public void SetCurrent(Guid channelId, decimal value)
			{
				int channelIndex = _ChannelIds.IndexOf(channelId);
				_CurrentPeriod[channelIndex] = value;
			}
			public void SetPrevious(Guid channelId, decimal value)
			{
				int channelIndex = _ChannelIds.IndexOf(channelId);
				_PreviousPeriod[channelIndex] = value;
			}
		}

		public Brandvertiser(Model.Brandvertiser bv, string userId, Guid focusChannelId, IncludeSet include, PeriodInfo period, GroupingValue value)
            : base(userId, focusChannelId, include, period, value)
		{
			Id = bv.Id;
			IsBrand = bv.IsBrand;
			Name = bv.Name;

			Brands = bv.GetBrands();
			_BrandvertiserCategoryIds = _GetBrandvertiserCategoryIds();
		}

		private List<Guid> _GetBrandvertiserCategoryIds()
		{
			return Database.ListFetcher(
				"SELECT DISTINCT category_id FROM songs WHERE brand_id " + Database.InClause(Brands.Select(b => b.Id)),
				dr => dr.GetGuid(0)
			);
		}
		private List<Guid> _GetCategoryBrands()
		{
			return Database.ListFetcher(
				"SELECT DISTINCT brand_id FROM songs WHERE category_id " + Database.InClause(_BrandvertiserCategoryIds),
				dr => dr.GetGuid(0)
			);
		}

		public Brandvertiser LoadDashboard()
		{
			Shifts = new PeriodToPeriod(Channels, _GetValueAdapter());
			_loadTotalByMedia();
			_loadCompetitivePosition();
			_loadPeriodToPeriod();
			_loadMediaMix();
			return this;
		}

		private void _loadTotalByMedia()
		{
			using (var conn = Database.Get())
			{
				var cmd = conn.CreateCommand();
				cmd.CommandText = string.Format(@"
SELECT channel_id, SUM({0}) as total
FROM report_base_cache
WHERE
	play_date >= @start AND play_date < @end AND
	channel_id " + Database.InClause(_ChannelIds) + @"
GROUP BY channel_id", _valueColumn());
				cmd.Parameters.AddWithValue("@start", _Period.CurrentStart);
				cmd.Parameters.AddWithValue("@end", _Period.CurrentEnd);

				var valueByMedia = new Dictionary<string, decimal>();
				decimal focusTotal = 0;
				decimal includeTotal = 0;
				var valueAdapter = _GetValueAdapter();

				using (var dr = cmd.ExecuteReader())
				{
					while (dr.Read())
					{
						Guid channelId = dr.GetGuid(0);
						string media = Channels.First(c => c.Id == channelId).MediaType;
						decimal value = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);

						Shifts.SetCurrent(channelId, value);

						if (!valueByMedia.ContainsKey(media))
							valueByMedia[media] = 0;
						valueByMedia[media] += value;

						if (channelId == _focusChannelId)
							focusTotal = value;
						if (_ChannelIds.Contains(channelId))
							includeTotal += value;
					}
				}
				FocusTotal = valueAdapter(focusTotal).ToString();
				string mediaType = Channels.First(c => c.Id == _focusChannelId).MediaType;
				decimal focusMediaTotal = valueByMedia.ContainsKey(mediaType) ? valueByMedia[mediaType] : 0;
				TotalByMedia = valueByMedia.Select(mediaValue => new MediaTotal(valueAdapter, mediaValue.Key, mediaValue.Value)).ToList();

				decimal allMediaValue = valueByMedia.DefaultIfEmpty().Sum(mediaValue => mediaValue.Value);
				TotalByMedia.Insert(0, new MediaTotal(valueAdapter, "All", allMediaValue));

				if (includeTotal != 0)
					ShareOfIncludeGroup = focusTotal / includeTotal;
				if (focusMediaTotal != 0)
					ShareOfMediaGroup = focusTotal / focusMediaTotal;
				if (allMediaValue != 0)
					ShareOfAllMedia = focusTotal / allMediaValue;
			}
		}

		public class CompetitivePositionSet
		{
			public List<MediaTotal> CategoryShare
			{
				get
				{
					return Medias.Select(media => new MediaTotal(_PercentageDisplayAdapter, media, _MediaCompetitivePositions[media].CategoryShare)).ToList();
				}
			}
			public List<MediaTotal> GapWithLeader
			{
				get
				{
					return Medias.Select(media => new MediaTotal(_ValueAdapter, media, _MediaCompetitivePositions[media].GapWithLeader)).ToList();
				}
			}
			public List<MediaTotal> UniverseShare
			{
				get
				{
					return Medias.Select(media => new MediaTotal(_PercentageDisplayAdapter, media, _MediaCompetitivePositions[media].UniverseShare)).ToList();
				}
			}

			private Period _Period;
			private string _ValueColumn;
			public List<string> Medias;
			private List<Guid> _BrandvertiserBrandIds;
			private List<Guid> _CategoryBrandIds;
			private Guid LeaderBrandId;
			public Brand Leader { get { return Brand.Get(LeaderBrandId); } }
			private Dictionary<string, MediaCompetitivePosition> _MediaCompetitivePositions;
			private Func<decimal, Reporting.ComputationResult> _PercentageDisplayAdapter = v => new Reporting.Percentage(v);
			private Func<decimal, Reporting.ComputationResult> _ValueAdapter;

			private class MediaCompetitivePosition
			{
				public decimal CategoryShare;
				public decimal GapWithLeader;
				public decimal UniverseShare;

				public decimal BrandvertiserTotal;
				public decimal LeaderTotal;
				public decimal CategoryTotal;
				public decimal UniverseTotal;

				public void Compute()
				{
					CategoryShare = CategoryTotal != 0 ? BrandvertiserTotal / CategoryTotal : 0;
					GapWithLeader = BrandvertiserTotal - LeaderTotal;
					UniverseShare = UniverseTotal != 0 ? BrandvertiserTotal / UniverseTotal : 0;
				}
			}

			public void FindCategoryLeader(List<Guid> categoryIds)
			{
				string selectLeader = string.Format(@"
SELECT r.brand_id
FROM report_base_cache r
WHERE
	r.play_date >= @start AND r.play_date < @end AND
	r.brand_id " + Database.InClause(_CategoryBrandIds) + @"
GROUP BY r.brand_id
ORDER BY SUM(r.{0}) DESC
LIMIT 1", _ValueColumn);
				LeaderBrandId = Database.ItemFetcher(
					selectLeader,
					dr => dr.GetGuid(0),
					"@start", _Period.CurrentStart,
					"@end", _Period.CurrentEnd
				);
			}
			public CompetitivePositionSet(Period period, List<Guid> brandvertiserBrandIds, List<Guid> categoryBrandIds, string valueColumn, Func<decimal, Reporting.ComputationResult> valueAdapter)
			{
				_Period = period;
				_BrandvertiserBrandIds = brandvertiserBrandIds;
				_CategoryBrandIds = categoryBrandIds;
				_ValueColumn = valueColumn;
				_ValueAdapter = valueAdapter;

				Medias = Database.ListFetcher("SELECT DISTINCT media_type FROM channels WHERE media_type IS NOT NULL", dr => dr.GetString(0));
				Medias.Insert(0, "All");

				_MediaCompetitivePositions = Medias.ToDictionary(media => media, media => new MediaCompetitivePosition());
			}
			public void GetUniverseTotals()
			{
				string universeTotal = string.Format(@"
SELECT media_type, SUM({0}) as total
FROM report_base_cache
WHERE
	media_type IS NOT NULL AND
	play_date >= @start AND play_date < @end
GROUP BY media_type", _ValueColumn);
				Database.ForEach(universeTotal,
					dr => {
						string media = dr.GetNullableString(0);
						decimal value = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);
						SetUniverseTotal(media, value);
					},
					"@start", _Period.CurrentStart,
					"@end", _Period.CurrentEnd
				);
			}
			public void GetCategoryBrandTotals()
			{
				string categoryBrandTotal = string.Format(@"
SELECT media_type, SUM({0}) as total
FROM report_base_cache
WHERE
	media_type IS NOT NULL AND
	play_date >= @start AND play_date < @end AND
	brand_id " + Database.InClause(_CategoryBrandIds) + @"
GROUP BY media_type", _ValueColumn);
				Database.ForEach(categoryBrandTotal,
					dr => {
						string media = dr.GetNullableString(0);
						decimal value = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);
						SetCategoryTotal(media, value);
					},
					"@start", _Period.CurrentStart,
					"@end", _Period.CurrentEnd
				);
			}
			public void GetLeaderTotals()
			{
				string leaderBrandTotal = string.Format(@"
SELECT media_type, SUM({0}) as total
FROM report_base_cache
WHERE
	media_type IS NOT NULL AND
	play_date >= @start AND play_date < @end AND
	brand_id = @brandId
GROUP BY media_type", _ValueColumn);
				Database.ForEach(leaderBrandTotal,
					dr => {
						string media = dr.GetNullableString(0);
						decimal value = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);
						SetLeaderTotal(media, value);
					},
					"@start", _Period.CurrentStart,
					"@end", _Period.CurrentEnd,
					"@brandId", LeaderBrandId
				);
			}
			public void GetBrandvertiserTotals()
			{
				string brandvertiserTotal = string.Format(@"
SELECT media_type, SUM({0}) as total
FROM report_base_cache
WHERE
	media_type IS NOT NULL AND
	play_date >= @start AND play_date < @end AND
	brand_id " + Database.InClause(_BrandvertiserBrandIds) + @"
GROUP BY media_type", _ValueColumn);
				Database.ForEach(brandvertiserTotal,
					dr => {
						string media = dr.GetNullableString(0);
						decimal value = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);
						SetBrandvertiserTotal(media, value);
					},
					"@start", _Period.CurrentStart,
					"@end", _Period.CurrentEnd
				);
			}

			public void SetBrandvertiserTotal(string media, decimal brandvertiserTotal)
			{
				_MediaCompetitivePositions[media].BrandvertiserTotal = brandvertiserTotal;
			}
			public void SetLeaderTotal(string media, decimal leaderTotal)
			{
				_MediaCompetitivePositions[media].LeaderTotal = leaderTotal;
			}
			public void SetCategoryTotal(string media, decimal categoryTotal)
			{
				_MediaCompetitivePositions[media].CategoryTotal = categoryTotal;
			}
			public void SetUniverseTotal(string media, decimal universeTotal)
			{
				_MediaCompetitivePositions[media].UniverseTotal = universeTotal;
			}

			public void Compute()
			{
				var all = _MediaCompetitivePositions["All"];
				all.BrandvertiserTotal = _MediaCompetitivePositions.Select(p => p.Value.BrandvertiserTotal).DefaultIfEmpty().Sum();
				all.LeaderTotal = _MediaCompetitivePositions.Select(p => p.Value.LeaderTotal).DefaultIfEmpty().Sum();
				all.CategoryTotal = _MediaCompetitivePositions.Select(p => p.Value.CategoryTotal).DefaultIfEmpty().Sum();
				all.UniverseTotal = _MediaCompetitivePositions.Select(p => p.Value.UniverseTotal).DefaultIfEmpty().Sum();

				foreach (string media in Medias)
					_MediaCompetitivePositions[media].Compute();
			}
		}
		private void _loadCompetitivePosition()
		{
			CompetitivePosition = new CompetitivePositionSet(_Period, Brands.Select(b => b.Id).ToList(), _GetCategoryBrands(), _valueColumn(), _GetValueAdapter());
			CompetitivePosition.FindCategoryLeader(_BrandvertiserCategoryIds);

			CompetitivePosition.GetUniverseTotals();
			CompetitivePosition.GetCategoryBrandTotals();
			CompetitivePosition.GetLeaderTotals();
			CompetitivePosition.GetBrandvertiserTotals();

			CompetitivePosition.Compute();
		}

		private void _loadPeriodToPeriod()
		{
			using (var conn = Database.Get())
			{
				var cmd = conn.CreateCommand();
				cmd.CommandText = string.Format(@"
SELECT channel_id, SUM({0}) as total
FROM report_base_cache
WHERE
	play_date >= @start AND play_date < @end AND
	channel_id " + Database.InClause(_ChannelIds) + @"
GROUP BY channel_id", _valueColumn());
				cmd.Parameters.AddWithValue("@start", _Period.PreviousStart);
				cmd.Parameters.AddWithValue("@end", _Period.PreviousEnd);

				var valueAdapter = _GetValueAdapter();

				using (var dr = cmd.ExecuteReader())
				{
					while (dr.Read())
					{
						Guid channelId = dr.GetGuid(0);
						decimal value = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);

						Shifts.SetPrevious(channelId, value);
					}
				}
			}
		}

		public class MediaMixSet
		{
			public MediaChart CurrentQuarter;
			public MediaChart PreviousQuarter;
			public MediaChart YearToDate;
			public MediaChart LastYear;

			public class MediaChart
			{
				public string Total;
				public List<MediaMixItem> ByMedia;
			}
		}
		public class MediaMixItem
		{
			public string Media;
			public decimal Total;
		}

		private void _loadMediaMix()
		{
			MediaMix = new MediaMixSet();
			var date = DateTime.UtcNow;
			MediaMix.CurrentQuarter = _loadMediaMixPeriod(new Period(PeriodKind.QuarterToDate, DateTime.UtcNow));
			MediaMix.PreviousQuarter = _loadMediaMixPeriod(new Period(PeriodKind.LastFullQuarter, DateTime.UtcNow));
			MediaMix.YearToDate = _loadMediaMixPeriod(new Period(PeriodKind.YearToDate, DateTime.UtcNow));
			MediaMix.LastYear = _loadMediaMixPeriod(new Period(PeriodKind.LastFullYear, DateTime.UtcNow));
		}
		private MediaMixSet.MediaChart _loadMediaMixPeriod(Period period)
		{
			using (var conn = Database.Get())
			{
				var cmd = conn.CreateCommand();
				cmd.CommandText = string.Format(@"
SELECT channel_id, SUM({0}) as total
FROM report_base_cache
WHERE
	play_date >= @start AND play_date < @end AND
	channel_id " + Database.InClause(_ChannelIds) + @"
GROUP BY channel_id", _valueColumn());
				cmd.Parameters.AddWithValue("@start", period.CurrentStart);
				cmd.Parameters.AddWithValue("@end", period.CurrentEnd);

				var valueByMedia = new Dictionary<string, decimal>();
				var valueAdapter = _GetValueAdapter();

				using (var dr = cmd.ExecuteReader())
				{
					while (dr.Read())
					{
						Guid channelId = dr.GetGuid(0);
						string media = Channels.First(c => c.Id == channelId).MediaType;
						decimal value = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);

						if (!valueByMedia.ContainsKey(media))
							valueByMedia[media] = 0;
						valueByMedia[media] += value;
					}
				}

				decimal total = valueByMedia.DefaultIfEmpty().Sum(mediaValue => mediaValue.Value);
				return new MediaMixSet.MediaChart
				{
					Total = valueAdapter(total).ToString(),
					ByMedia = valueByMedia.Select(mediaValue => new MediaMixItem
					{
						Media = mediaValue.Key,
						Total = mediaValue.Value //string.Format("{0:0}%", mediaValue.Value * 100 / total)
					}).ToList()
				};
			}
		}

		public List<string> LoadAdsInRotation()
		{
			return Database.ListFetcher(@"
				SELECT DISTINCT s.id, s.title
				FROM matches AS m
				JOIN songs AS s ON m.song_id = s.id
				JOIN accounts a ON a.user_id = s.user_id
				WHERE
					s.duration > 0 AND
					m.match_end - m.match_start >= s.duration * 0.7 AND
					s.deleted = 0 AND
					s.suppress_chart = 0 AND
					a.suppress_chart = 0 AND
					match_occurred >= @start AND match_occurred < @end AND
					s.brand_id " + Database.InClause(Brands.Select(b => b.Id)) + @" AND
					m.channel_id " + Database.InClause(_ChannelIds),
				dr => dr.GetNullableString(1),
				"@start", _Period.CurrentStart,
				"@end", _Period.CurrentEnd
			);
        }
	}
}