using DatabaseCommon;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class Holding
	{
		public int Id;
		public string UserId;
		public string Name;
		public bool Mine;

		public static List<Holding> GetByUser(string userId)
		{
			return Database.ListFetcher<Holding>("SELECT id, name, mine FROM holding WHERE user_id = @userId",
				dr => new Holding
				{
					Id = dr.GetInt32(0),
					UserId = userId,
					Name = dr.GetString(1),
					Mine = dr.GetBoolean(2)
				},
				"@userId", userId
			);
		}

		public void Insert()
		{
			Id = (int)Database.Insert("INSERT INTO holding (user_id, name, mine) VALUES (@userId, @name, @mine)",
				"@userId", UserId,
				"@name", Name,
				"@mine", Mine);
		}
		public bool Update(Holding dbHolding)
		{
			if (dbHolding.Name != this.Name || dbHolding.Mine != this.Mine)
				return Database.ExecuteNonQuery(@"UPDATE holding SET name = @name, mine = @mine WHERE id = @id",
					"@name", this.Name,
					"@mine", this.Mine,
					"@id", this.Id
				) > 0;
			return false;
		}
		public bool Delete()
		{
			return Database.Delete("DELETE FROM holding WHERE id = @id", "@id", Id);
		}
		public static void SetMine(string userId, int? holdingId)
		{
			Database.ExecuteNonQuery(@"UPDATE holding SET mine = 0 WHERE user_id = @userId", "@userId", userId);
			if (holdingId.HasValue)
				Database.ExecuteNonQuery(@"UPDATE holding SET mine = 1 WHERE user_id = @userId AND id = @id", "@userId", userId, "@id", holdingId.Value);
		}
		public static Holding GetMine(string userId)
		{
			return Database.ItemFetcher<Holding>("SELECT id, name FROM holding WHERE user_id = @userId AND mine = 1",
				dr => new Holding
				{
					Id = dr.GetInt32(0),
					UserId = userId,
					Name = dr.GetString(1),
					Mine = true
				},
				"@userId", userId
			);
		}
		public List<Group> GetGroups()
		{
			return Group.GetByHolding(Id);
		}
	}
}