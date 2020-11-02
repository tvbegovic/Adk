using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model
{
    public class PriceDefinition
    {
        public Guid Id { get; set; }
        public Guid ChannelId { get; set; }
        public Guid ProductId { get; set; }
        public int Hour { get; set; }
        public int Dow { get; set; }
        public decimal Pps { get; set; }


        public static List<PriceDefinition> Get(Guid channelId, Guid productId)
        {
            string query = String.Format(@"SELECT id, channel_id, product_id, hour, dow, pps
				  FROM pricedefs
				 Where channel_id = '{0}' AND product_id='{1}'", channelId, productId);

            return Database.ListFetcher<PriceDefinition>(query,
                dr => new PriceDefinition
                {
                    Id = dr.GetGuid(0),
                    ChannelId = dr.GetGuid(1),
                    ProductId = dr.GetGuid(2),
                    Hour = dr.GetInt32(3),
                    Dow = dr.GetIntOrDefault(4),
                    Pps = dr.GetDecimal(5)
                }
            );
        }

        public static bool AddOrUpdate(List<PriceDefinition> priceDefs)
        {

            using (var db = Database.Get())
            {
                using (var tran = db.BeginTransaction())
                {
                    foreach (var priceDef in priceDefs)
                    {
                        if (!Database.Exists(db, tran, "SELECT 1 FROM pricedefs WHERE channel_id = @channel_id AND product_id = @product_id AND hour = @hour AND dow = @dow",
                                    "@channel_id", priceDef.ChannelId,
                                    "@product_id", priceDef.ProductId,
                                    "@hour", priceDef.Hour,
                                    "@dow", priceDef.Dow))
                        {
                            Database.ExecuteNonQuery("INSERT INTO pricedefs(id, channel_id, product_id, hour, dow, pps) VALUES (@id, @channel_id, @product_id, @hour, @dow, @pps)",
                            "@id", Guid.NewGuid(),
                            "@channel_id", priceDef.ChannelId,
                            "@product_id", priceDef.ProductId,
                            "@hour", priceDef.Hour,
                            "@dow", priceDef.Dow,
                            "@pps", priceDef.Pps);

                        }
                        else
                        {
                            Database.ExecuteNonQuery("UPDATE pricedefs set pps = @pps WHERE channel_id = @channel_id AND product_id = @product_id AND hour = @hour AND dow = @dow",
                            "@channel_id", priceDef.ChannelId,
                            "@product_id", priceDef.ProductId,
                            "@hour", priceDef.Hour,
                            "@dow", priceDef.Dow,
                            "@pps", priceDef.Pps);
                        }
                    }

                    tran.Commit();
                }
            }

            return true;
        }

		public static byte ByteDOW(DayOfWeek dayOfWeek)
		{
			byte retval = (byte)dayOfWeek;
			if (retval == 0)
			{
				retval = 7;
			}
			retval--;
			return retval;
		}
	}

	
}
