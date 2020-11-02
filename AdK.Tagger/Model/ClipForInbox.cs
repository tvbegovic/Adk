namespace AdK.Tagger.Model
{
	public class ClipForInbox
	{
		public string SongID { get; set; } // 00000000-0000-0000-0000-000000000000
		public string FileName { get; set; } // 8528a745a349412992edf1346a4765c7.mp3
		public string ExportDateTime { get; set; } // 2015-08-22T09:37:05.2-04:00
		public string Tagger { get; set; } // Tara
		public string FirstWords { get; set; } // Looking for the very best
		public string StationName { get; set; } // HOTT 93 FM
		public string StationID { get; set; } // 706a4343-7990-4d9a-944f-59443f2f327e
		public string ClipDateTime { get; set; } // 2015-08-21T16:22:24.313
		public decimal ClipDuration { get; set; } // 32.613
		public string BrandID { get; set; } // b97ec73e-658b-11e3-a0f2-00155d03d309
		public string Brand { get; set; } // Standard
		public string AdCategoryID { get; set; } // 99cdbd77-d011-4f99-a9de-1937dbf0d787
		public string AdCategory { get; set; } // Furniture and Appliances
		public string ProductID { get; set; } // 3c81b8b8-e432-4c61-a189-6ecd727a79bf
		public string Product { get; set; } // Spot
		public string CreativeTitle { get; set; } // Looking for the very best
		public string MessageType { get; set; } // Standard
		public string Campaign { get; set; } // General
		public string Advertiser { get; set; } // Standard Distributors Ltd
		public string Industry { get; set; } // Retail and E-Tail Stores
		public string PksID { get; set; } // 9897a40909c8482396a7a74f1c453d7f
		public string MediaType { get; set; } // Radio
		public string PromotionDateTime { get; set; } // 0001-01-01T00:00:00
		public bool ScanExpires { get; set; } // false
		public string ScanExpiresOn { get; set; } // 0001-01-01T00:00:00
		// public object[] Transcripts // Unknown content

		public static ClipForInbox Deserialize(string xml)
		{
			return xml.DeserializeXml<ClipForInbox>();
		}
	}
}