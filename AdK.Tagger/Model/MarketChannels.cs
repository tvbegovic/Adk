using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model
{
	public class MarketChannelsGroup
	{
		public int MarketId { get; set; }
		public Dictionary<string, string> MarketChannels { get; set; }
	}

	public class MarketChannels
	{
		public int Id { get; set; }
		[ColumnNameAttr( "market_id" )]
		public int MarketId { get; set; }
		[ColumnNameAttr( "channel_id" )]
		public string ChannelId { get; set; }


		public static List<MarketChannels> GetMarketChannels( string marketId )
		{
			string query = "SELECT id, market_id, channel_id  FROM market_channels WHERE market_id = @marketId";
			return Database.ListFetcher( query, dr => new MarketChannels {
				Id = dr.GetInt32( 0 ),
				MarketId = dr.GetInt32( 1 ),
				ChannelId = dr.GetString( 2 )
			}, "@marketId", marketId );
		}	

		public static List<MarketChannelsGroup> GetUserMarketChannelsGroup( string userId )
		{
			var marketChannelsGroup = new List<MarketChannelsGroup>();
			string query = @"SELECT mc.id, mc.market_id, mc.channel_id 
								FROM market_channels mc
								LEFT JOIN markets m ON m.id = mc.market_id 
								WHERE m.user_id = @userId";


			List<MarketChannels> marketChannels = Database.ListFetcher( query, dr => new MarketChannels
			{
				Id = dr.GetInt32(0),
				MarketId = dr.GetInt32(1),
				ChannelId = dr.GetString(2)
			}, "@userId", userId );
			
			marketChannels.ForEach( mc =>
			{
				var market = marketChannelsGroup.FirstOrDefault(mgroup => mgroup.MarketId == mc.MarketId);
				if (market == null)
				{
					market = new MarketChannelsGroup
					{
						MarketId = mc.MarketId,
						MarketChannels = new Dictionary<string, string>()
					};

					marketChannelsGroup.Add(market);
				}

				if (!market.MarketChannels.ContainsKey(mc.ChannelId))
				{
					market.MarketChannels[mc.ChannelId] = mc.ChannelId;
				}

			} );

			return marketChannelsGroup;

		}
	}

}