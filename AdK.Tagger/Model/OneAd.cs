using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace AdK.Tagger.Model
{
	public enum AdEntryStatus : byte
	{
		None = 0,
		SavePartial = 1,
		SaveComplete = 2,
		Skip = 3
	}
	public enum AdItemType
	{
		Exclusive,
		CoBrand,
		CoopBrand,
		OtherBrand,
		SponsorshipTitle,
		SponsorshipSecondary,
		StationJointPromoTitle,
		StationJointPromoSecondary,
	}
	public enum AdTypeEnum
	{
		SpotAdvertiser,
		SpotStationAdvertiserJointPromotion,
		AdlibLiveReadAdvertiser,
		AdlibLiveReadStationAdvertiserJointPromotion,
		PromoCorporateFamily,
		PromoStation,
		StationID,
		SponsoredProgramPromotion,
		PSA,
	}
	public enum MusicTypeEnum
	{
		None,
		ProductionMusic,
		Jingle,
		Song,
	}
	public class OneAdItem
	{
		public string Brand { get; set; }
		public string Category { get; set; }
		public string ProductOrItem { get; set; }
		public AdItemType ItemType { get; set; }
		public string Advertiser { get; set; }
		public string Industry { get; set; }
		public string MediaType { get; set; }

		public bool IsEmpty()
		{
			return
				string.IsNullOrEmpty(Brand) &&
				string.IsNullOrEmpty(Category) &&
				string.IsNullOrEmpty(ProductOrItem) &&
				string.IsNullOrEmpty(Advertiser) &&
				string.IsNullOrEmpty(Industry) &&
				string.IsNullOrEmpty(MediaType);
		}
	}
	public class AdVoice
	{
		public bool Male { get; set; }
		public bool Female { get; set; }
		public bool Child { get; set; }
	}
	public class OneAd
	{
		public AdTypeEnum AdType { get; set; }
		public string ExpirationDate { get; set; } // ISO date format
		public string Campaign { get; set; }
		public string AdTitle { get; set; }
		public string Transcript { get; set; }
		public AdVoice Voice { get; set; }
		public MusicTypeEnum MusicBed { get; set; }
		public string SongTitle { get; set; }
		public string SongArtist { get; set; }
		public List<OneAdItem> AdFacts { get; set; }
		decimal Duration { get; set; }

		public const byte SchemaVersion = 2;

		public OneAd()
		{
			AdFacts = Enumerable.Range(0, 1).Select(i => new OneAdItem()).ToList();
		}

		public List<Tuple<string, string>> GetLabels()
		{
			var labels = new List<Tuple<string, string>>();
			foreach (var fact in AdFacts)
			{
				labels.Add(new Tuple<string, string>("brand", fact.Brand));
				labels.Add(new Tuple<string, string>("product", fact.ProductOrItem));
				labels.Add(new Tuple<string, string>("category", fact.Category));
				labels.Add(new Tuple<string, string>("advertiser", fact.Advertiser));
				labels.Add(new Tuple<string, string>("industry", fact.Industry));
			}
			labels.Add(new Tuple<string, string>("campaign", Campaign));
			labels.Add(new Tuple<string, string>("song title", SongTitle));
			labels.Add(new Tuple<string, string>("song artist", SongArtist));
			return labels;
		}
		public string Serialize()
		{
			using (var stringWriter = new StringWriter())
			using (var writer = XmlWriter.Create(stringWriter))
			{
				var xmlSerializer = new XmlSerializer(typeof(OneAd));
				xmlSerializer.Serialize(writer, this);
				return stringWriter.ToString();
			}
		}
		public static OneAd Deserialize(string xml)
		{
			return xml.DeserializeXml<OneAd>();
		}
	}
}