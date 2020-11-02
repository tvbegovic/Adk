using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model
{
    public class PriceDefinition2
    {
        public int Id { get; set; }
        public Guid ChannelId { get; set; }
        public int? DayPartId { get; set; }
        public TimeSpan? From { get; set; }
		public TimeSpan? To { get; set; }
		public int Day { get; set; }

        public DayPart DayPart {get;set;}
        


        public static List<PriceDefinition2> Get(Guid channelId, string userId)
        {
            string query = string.Format(@"SELECT pricedefinition.id, pricedefinition.channel_id, pricedefinition.day_part_id, pricedefinition.time_from,
					pricedefinition.time_to, pricedefinition.day, day_part.name, COALESCE(day_part.short_code,''), day_part.color, day_part.background_color
				  FROM pricedefinition INNER JOIN day_part ON pricedefinition.day_part_id = day_part.id INNER JOIN day_part_set
				  ON day_part.day_part_set_id = day_part_set.id
				  Where channel_id = '{0}' AND day_part_set.user_id = '{1}'", channelId,userId);

            return Database.ListFetcher(query,
                dr => new PriceDefinition2
                {
                    Id = dr.GetInt32(0),
                    ChannelId = dr.GetGuid(1),
                    DayPartId = dr.GetIntOrNull(2),
                    From = dr.GetTimeSpan(3),
					To = dr.GetTimeSpan(4),
                    Day = dr.GetIntOrDefault(5),
                    DayPart = new DayPart {
						Id = dr.GetInt32(2),
						Name = dr.GetString(6),
						Short_code = dr.GetString(7),
						Color = dr.GetStringOrDefault(8),
						BackgroundColor = dr.GetStringOrDefault(9)
					}               
                }
            );
        }

        public static List<PriceDefinition2> AddOrUpdate(List<PriceDefinition2> priceDefs)
        {

            using (var db = Database.Get())
            {
                using (var tran = db.BeginTransaction())
                {
                    foreach (var priceDef in priceDefs)
                    {
                        if (priceDef.Id <= 0)
                        {
                            priceDef.Id = Convert.ToInt32(Database.Insert(db, tran,
							@"INSERT INTO `pricedefinition`
								(`day_part_id`,`day`,`time_from`,`time_to`,`channel_id`)
								VALUES
								(@day_part_id, @day, @time_from,@time_to,@channel_id);",
                            
                            "@channel_id", priceDef.ChannelId,
                            "@day_part_id", priceDef.DayPartId,
                            "@day", priceDef.Day,
							"@time_from", priceDef.From,
							"@time_to" , priceDef.To
							));

                        }
                        else
                        {
                            Database.ExecuteNonQuery(db, tran,
								@"UPDATE `pricedefinition`
								SET
								`day_part_id` = @day_part_id,
								`day` = @day,
								`time_from` = @time_from,
								`time_to` = @time_to,
								`channel_id` = @channel_id
								WHERE `id` = @id",
                            "@id", priceDef.Id,
							"@channel_id", priceDef.ChannelId,
							"@day_part_id", priceDef.DayPartId,
							"@day", priceDef.Day,
							"@time_from", priceDef.From,
							"@time_to", priceDef.To
							);
                        }
                    }

                    tran.Commit();
                }
            }

            return priceDefs;
        }

		public static void DeleteByIds(IList<int> ids)
		{
			Database.Delete("DELETE FROM pricedefinition WHERE id " + Database.InClause(ids));
		}

    }
}
