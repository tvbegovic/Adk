using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Model
{
	public class WordCut
	{
		public int Id;
		public int TranscriptId;
		public Guid SongId;
		public string PksId;
		public decimal PartStart;
		public decimal PartEnd;
		public string PartText;
		public decimal Duration;
		public string Word;
		public decimal WordStart;
		public decimal WordDuration;

		public static WordCut PickNext(string userId)
		{
			return new WordCut
			{
				TranscriptId = 2111,
				SongId = Guid.Parse("2aa0cebf-933f-477f-80b7-b455fbbef1fb"),
				PksId = "9d6ea1fcc0c1464ab19bc22ed233248f",
				Word = "drink",
				PartStart = 14.595000M,
				PartEnd = 17.514000M,
				PartText = "And a large drink to satisfy you",
				Duration = 29.191000M
			};
		}
	}
}