using AdK.Tagger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Serialization;
//using static AdKontrol.Tagger.Service;

namespace AdK.Tagger
{
	public static class StringExtensions
	{
		public static T ToEnum<T>(this string s)
		{
			return (T)Enum.Parse(typeof(T), s);
		}

		public static T DeserializeXml<T>(this string xml) where T : class, new()
		{
			if (string.IsNullOrEmpty(xml))
				return new T();

			using (TextReader reader = new StringReader(xml))
			{
				var xmlSerializer = new XmlSerializer(typeof(T));
				return xmlSerializer.Deserialize(reader) as T;
			}
		}
	}

	public static class LinqExtensions
	{
		public static IOrderedEnumerable<TSource> OrderByWithDirection<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			bool descending)
		{
			return descending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
		}

		public static IOrderedQueryable<TSource> OrderByWithDirection<TSource, TKey>(
			this IQueryable<TSource> source,
			Expression<Func<TSource, TKey>> keySelector,
			bool descending)
		{
			return descending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
		}

		public static T RandomOrDefault<T>(this IEnumerable<T> items)
		{
			var count = items.Count();
			if (count != 0)
			{
				var random = new Random();
				var index = random.Next(count - 1);
				return items.ElementAt(index);
			}
			return default(T);
		}
		public static IEnumerable<T> Randomize<T>(this IEnumerable<T> items)
		{
			var random = new Random();
			return items
				.Select(i => new { Item = i, Random = random.NextDouble() })
				.OrderBy(a => a.Random)
				.Select(a => a.Item)
				.ToList();
		}
	}

    public static class FeedExtensions
    {
        public static List<FeedFilterRule> ToFeedFilterRule(this List<AjaxDomain> list)
        {
            return list.Select(x => new FeedFilterRule() { Id = x.Domain, DisplayName = x.DisplayName }).ToList();
        }

        public static List<FeedFilterRule> ToFeedFilterRule(this List<Market> list)
        {
            return list.Select(x => new FeedFilterRule() { Id = x.Id.ToString(), DisplayName = x.Name }).ToList();
        }
    }
}