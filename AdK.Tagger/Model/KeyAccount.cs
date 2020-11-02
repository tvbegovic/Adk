using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
	public class KeyAccount
	{
		public int Id;
		public string UserId;
		public string BrandId;
		public string AdvertiserId;

		public static List<Brandvertiser> GetByUser(string userId)
		{
			return Database.ListFetcher<Brandvertiser>(@"
SELECT ka.brand_id, b.brand_name, ka.advertiser_id, a.company_name
FROM key_account ka
LEFT JOIN brands b ON ka.brand_id = b.id
LEFT JOIN advertisers a ON ka.advertiser_id = a.id
WHERE ka.user_id = @userId", dr =>
				new Brandvertiser
				{
					Id = dr.GetGuid(dr.IsDBNull(0) ? 2 : 0),
					IsBrand = !dr.IsDBNull(0),
					Name = dr.GetString(dr.IsDBNull(0) ? 3 : 1)
				}, "@userId", userId);
		}

		public static long Add(string userId, Brandvertiser brandvertiser)
		{
			try
			{
				return Database.Insert(@"INSERT INTO key_account (user_id, brand_id, advertiser_id) VALUES (@userId, @brandId, @advertiserId)",
					"@userId", userId,
					"@brandId", brandvertiser.IsBrand ? brandvertiser.Id.ToString() : null,
					"@advertiserId", !brandvertiser.IsBrand ? brandvertiser.Id.ToString() : null);
			} catch(Exception) // In case of a duplicate entry, unicity constraint prevents insertion
			{
				return 0;
			}
		}

		public static bool Remove(string userId, Brandvertiser brandvertiser)
		{
			if (brandvertiser.IsBrand)
				return Database.Delete(@"DELETE FROM key_account WHERE user_id = @userId AND brand_id = @brandId",
					"@userId", userId,
					"@brandId", brandvertiser.Id);
			else
				return Database.Delete(@"DELETE FROM key_account WHERE user_id = @userId AND advertiser_id = @advertiserId",
					"@userId", userId,
					"@advertiserId", brandvertiser.Id);
		}
	}
}