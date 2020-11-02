using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Model
{
    public class ProductDayPart
    {
        public Guid Id { get; set; }
        public Guid ChannelId { get; set; }
        public Guid ProductId { get; set; }
        public int? DayPartId { get; set; }
        public string ProductCode { get; set; }
        public int? DurationMax { get; set; }
        public int PriceMode { get; set; }
        public double? Price { get; set; }

        public static List<ProductDayPart> Get(Guid channelId, string userId)
        {
            string query = String.Format(@"SELECT * FROM product_daypart WHERE channel_id = '{0}' 
                    AND daypart_id IN (SELECT day_part.id FROM day_part INNER JOIN day_part_set ON day_part.day_part_set_id = 
                    day_part_set.id WHERE day_part_set.user_id = '{1}');", channelId,userId);

            return Database.ListFetcher<ProductDayPart>(query,
                dr => new ProductDayPart
                {
                    Id = dr.GetGuid(0),
                    ProductCode = dr.GetNullableString(1),
                    DurationMax = dr.GetNullableInt(2),
                    PriceMode = dr.GetInt32(3),
                    Price = dr.GetNullableDouble(4),
                    ProductId = dr.GetGuid(5),
                    ChannelId = dr.GetGuid(6),
                    DayPartId = dr.GetNullableInt(7)
                }
            );
        }

        public static List<Product> GetProducts(Guid channelId, string userId)
        {
            string query = String.Format(@"SELECT DISTINCT products.id,products.product_name 
                                        FROM product_daypart INNER JOIN products 
                                        ON product_daypart.product_id = products.id
				                        Where channel_id = '{0}' AND daypart_id IN 
                                        (SELECT day_part.id FROM day_part INNER JOIN day_part_set ON day_part.day_part_set_id = 
                                         day_part_set.id WHERE day_part_set.user_id = '{1}')", channelId,userId);

            return Database.ListFetcher<Product>(query, dr => new Product
            {
                Id = dr.GetGuid(0),
                Name = dr.GetString(1)
            });
        }

        public static List<ProductDayPart> AddOrUpdate(List<ProductDayPart> productDayParts, List<string> deletedIds)
        {

            using (var db = Database.Get())
            {
                using (var tran = db.BeginTransaction())
                {
                    foreach (var productDayPart in productDayParts)
                    {
                        if (productDayPart.Id == Guid.Empty)
                        {
                            productDayPart.Id = Guid.NewGuid();
                            Database.ExecuteNonQuery(@"INSERT INTO product_daypart(id,product_code, duration_max, price_mode, price, product_id, channel_id, daypart_id ) 
                                                       VALUES (@id,@product_code, @duration_max, @price_mode, @price,@product_id, @channel_id, @daypart_id)",
                            "@id", productDayPart.Id,
                            "@product_code", productDayPart.ProductCode,
                            "@duration_max", productDayPart.DurationMax,
                            "@price_mode", productDayPart.PriceMode,
                            "@price", productDayPart.Price,
                            "@product_id", productDayPart.ProductId,
                            "@channel_id", productDayPart.ChannelId,
                            "@daypart_id", productDayPart.DayPartId
                            );

                        }
                        else
                        {
                           Database.ExecuteNonQuery(
                           @"UPDATE `product_daypart`  SET
                            `id` = @id,
                            `product_code` = @product_code,
                            `duration_max` = @duration_max,
                            `price_mode` = @price_mode,
                            `price` = @price,
                            `product_id` = @product_id,
                            `channel_id` = @channel_id,
                            `daypart_id` = @daypart_id
                            WHERE `id` = @id",
                            "@id", productDayPart.Id,
                            "@product_code", productDayPart.ProductCode,
                            "@duration_max", productDayPart.DurationMax,
                            "@price_mode", productDayPart.PriceMode,
                            "@price", productDayPart.Price,
                            "@product_id", productDayPart.ProductId,
                            "@channel_id", productDayPart.ChannelId,
                            "@daypart_id", productDayPart.DayPartId);
                        }
                    }
					if(deletedIds.Count > 0)
					{
					  Database.Delete(string.Format("DELETE FROM `product_daypart` WHERE Id {0}", Database.InClause(deletedIds)));
					}

                    tran.Commit();
                }
            }

            return productDayParts;
        }
    }
}
