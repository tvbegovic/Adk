using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model
{
    public class PriceDefinition11
    {
        public Guid Id { get; set; }
        public Guid ChannelId { get; set; }
        public int? DayPartId { get; set; }
        public int Hour { get; set; }
        public int Day { get; set; }

        public DayPart DayPart {get;set;}
        


        public static List<PriceDefinition11> Get(Guid channelId, string userId)
        {
            string query = String.Format(@"SELECT pricedefs2.id, pricedefs2.channel_id, pricedefs2.daypart_id, pricedefs2.hour, pricedefs2.day, day_part.name, COALESCE(day_part.short_code,'')
				  FROM pricedefs2 INNER JOIN day_part ON pricedefs2.daypart_id = day_part.id INNER JOIN day_part_set ON day_part.day_part_set_id = day_part_set.id
				 Where channel_id = '{0}' AND day_part_set.user_id = '{1}'", channelId,userId);

            return Database.ListFetcher(query,
                dr => new PriceDefinition11
                {
                    Id = dr.GetGuid(0),
                    ChannelId = dr.GetGuid(1),
                    DayPartId = dr.GetIntOrNull(2),
                    Hour = dr.GetInt32(3),
                    Day = dr.GetIntOrDefault(4),
                    DayPart = new DayPart { Id = dr.GetInt32(2), Name = dr.GetString(5), Short_code = dr.GetString(6)}               
                }
            );
        }

        public static List<PriceDefinition11> AddOrUpdate(List<PriceDefinition11> priceDefs)
        {

            using (var db = Database.Get())
            {
                using (var tran = db.BeginTransaction())
                {
                    foreach (var priceDef in priceDefs)
                    {
                        if (priceDef.Id == Guid.Empty)
                        {
                            priceDef.Id = Guid.NewGuid();
                            Database.ExecuteNonQuery("INSERT INTO pricedefs2(id, channel_id, daypart_id, hour, day) VALUES (@id, @channel_id, @daypart_id, @hour, @day)",
                            "@id", priceDef.Id,
                            "@channel_id", priceDef.ChannelId,
                            "@daypart_id", priceDef.DayPartId,
                            "@hour", priceDef.Hour,
                            "@day", priceDef.Day);

                        }
                        else
                        {
                            Database.ExecuteNonQuery(@"UPDATE pricedefs2 set daypart_id = @daypart_id 
                                WHERE id = @id",
                            "@id", priceDef.Id,
                            "@daypart_id", priceDef.DayPartId);
                        }
                    }

                    tran.Commit();
                }
            }

            return priceDefs;
        }


    }
}
