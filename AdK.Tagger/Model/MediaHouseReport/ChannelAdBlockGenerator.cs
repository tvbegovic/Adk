using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model.MediaHouseReport
{

	public class ChannelBlock
	{
		public ChannelBlock()
		{
			Items = new List<ChannelBlockItem>();
		}

		public Guid ChannelId { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public List<ChannelBlockItem> Items { get; set; }
	}

	public class ChannelBlockItem
	{
		public double DurationInSeconds { get; set; }
		public DateTime MatchOccured { get; set; }
		public string BrandId { get; set; }
		public string BrandName { get; set; }
		public string Title { get; set; }
		public string Advertiser { get; set; }
		public string Industry { get; set; }
		public string Category { get; set; }
        public Guid SongId { get; set; }
        public string PksId { get; set; }
    }

	public class RawDbMatchItem
	{
		public Guid ChannelId { get; set; }
		public double MatchStart { get; set; }
		public double MatchEnd { get; set; }
		public DateTime MatchOccured { get; set; }
		public string BrandId { get; set; }
		public string BrandName { get; set; }
		public string Title { get; set; }
		public string Advertiser { get; set; }
		public string Industry { get; set; }
		public string Category { get; set; }
        public Guid SongId { get; set; }
        public string PksId { get; set; }
    }

	public class ChannelAdBlockGenerator
	{
		static readonly ChannelAdBlockGenerator _instance = new ChannelAdBlockGenerator();
		public static ChannelAdBlockGenerator Instance
		{
			get
			{
				return _instance;
			}
		}

		private readonly int AD_BLOCK_THRESHOLD_IN_MS;
		ChannelAdBlockGenerator()
		{
			AD_BLOCK_THRESHOLD_IN_MS = Settings.Get( "Report", "AdBlockThresholdInMs", 5000 );
		}

		public Dictionary<Guid, List<ChannelBlock>> GroupByChannelBlocks( List<RawDbMatchItem> dbRows )
		{
			var channelBlocks = new Dictionary<Guid, List<ChannelBlock>>();
			foreach ( var dbRow in dbRows ) {
				DateTime startDate = dbRow.MatchOccured.AddSeconds( dbRow.MatchStart );
				DateTime endDate = dbRow.MatchOccured.AddSeconds( dbRow.MatchEnd );

				if ( !channelBlocks.ContainsKey( dbRow.ChannelId ) ) {
					channelBlocks[dbRow.ChannelId] = new List<ChannelBlock>();
				}

				var blockValues = channelBlocks[dbRow.ChannelId];

				var adBlock =
					 blockValues.FirstOrDefault(
						  cb => ( startDate > cb.StartDate.AddMilliseconds( -AD_BLOCK_THRESHOLD_IN_MS ) && startDate < cb.EndDate.AddMilliseconds( AD_BLOCK_THRESHOLD_IN_MS ) )
									 || (endDate > cb.StartDate.AddMilliseconds( -AD_BLOCK_THRESHOLD_IN_MS ) && endDate < cb.EndDate.AddMilliseconds( AD_BLOCK_THRESHOLD_IN_MS )) );

				if ( adBlock == null ) {
					adBlock = new ChannelBlock {
						ChannelId = dbRow.ChannelId,
						StartDate = startDate,
						EndDate = endDate
					};


					blockValues.Add( adBlock );
				}
				else {
					if ( startDate < adBlock.StartDate ) {
						adBlock.StartDate = startDate;
					}
					else if ( endDate > adBlock.EndDate ) {
						adBlock.EndDate = endDate;
					}
				}


				adBlock.Items.Add( new ChannelBlockItem {
					DurationInSeconds = dbRow.MatchEnd - dbRow.MatchStart,
					MatchOccured = dbRow.MatchOccured,
					BrandId = dbRow.BrandId,
					BrandName = dbRow.BrandName,
					Title = dbRow.Title,
					Advertiser = dbRow.Advertiser,
					Industry = dbRow.Industry,
					Category = dbRow.Category,
                    SongId = dbRow.SongId,
                    PksId = dbRow.PksId
				} );
			}

			return channelBlocks;
		}
	}
}