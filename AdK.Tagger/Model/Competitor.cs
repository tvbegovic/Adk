using DatabaseCommon;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class Competitor
	{
		public Guid MyChannelId;
		public Guid OtherChannelId;

		public static List<Competitor> GetAll(string userId)
		{
			return Database.ListFetcher<Competitor>(
				@"SELECT my_channel_id, other_channel_id FROM competitor WHERE user_id = @userId",
				dr => new Competitor
				{
					MyChannelId = dr.GetGuid(0),
					OtherChannelId = dr.GetGuid(1)
				},
				"@userId", userId
			);
		}

		public bool Add(string userId)
		{
			return Database.ExecuteNonQuery(
				@"INSERT INTO competitor (user_id, my_channel_id, other_channel_id) VALUES (@userId, @myChannelId, @otherChannelId)",
				"@userId", userId,
				"@myChannelId", this.MyChannelId,
				"@otherChannelId", this.OtherChannelId
			) == 1;
		}
		public bool Remove(string userId)
		{
			return Database.Delete(
				@"DELETE FROM competitor WHERE user_id = @userId AND my_channel_id = @myChannelId AND other_channel_id = @otherChannelId",
				"@userId", userId,
				"@myChannelId", this.MyChannelId,
				"@otherChannelId", this.OtherChannelId
			);
		}

		public static List<Guid> GetFor(string userId, Guid myChannelId)
		{
			return Database.ListFetcher<Guid>(
				@"SELECT other_channel_id FROM competitor WHERE user_id = @userId AND my_channel_id = @myChannelId",
				dr => dr.GetGuid(0),
				"@userId", userId,
				"@myChannelId", myChannelId
			);
		}
	}
}