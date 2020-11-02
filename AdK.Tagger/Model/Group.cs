using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
    public class Group
    {
        public int Id;
        public string UserId;
        public string Name;
        public int? HoldingId;
        public List<Guid> ChannelIds = new List<Guid>();

        public static List<Group> GetByUser(string userId)
        {
            var groups = Database.ListFetcher<Group>("SELECT id, name, holding_id FROM `group` WHERE user_id = @userId ORDER BY name",
                dr => new Group
                {
                    Id = dr.GetInt32(0),
                    UserId = userId,
                    Name = dr.GetString(1),
                    HoldingId = dr.GetNullableInt(2)
                },
                "@userId", userId
            );

            foreach (var group in groups)
                group.GetChannels();

            return groups;
        }
        public static List<Group> GetByHolding(int holdingId)
        {
            var groups = Database.ListFetcher<Group>("SELECT id, user_id, name FROM `group` WHERE holding_id = @holdingId",
                dr => new Group
                {
                    Id = dr.GetInt32(0),
                    UserId = dr.GetString(1),
                    Name = dr.GetString(2),
                    HoldingId = holdingId
                },
                "@holdingId", holdingId
            );

            foreach (var group in groups)
                group.GetChannels();

            return groups;
        }

        private void GetChannels()
        {
            ChannelIds = Database.ListFetcher<Guid>("SELECT channel_id FROM channel_group WHERE group_id = @groupId",
                dr => dr.GetGuid(0),
                "@groupId", Id
            );
        }
        public static Group Get(string userId, int groupId)
        {
            var group = Database.ItemFetcher<Group>("SELECT name, holding_id FROM `group` WHERE id = @groupId AND user_id = @userId",
                dr => new Group
                {
                    Id = groupId,
                    UserId = userId,
                    Name = dr.GetString(0),
                    HoldingId = dr.GetNullableInt(1)
                },
                "@groupId", groupId,
                "@userId", userId
            );

            group.GetChannels();

            return group;
        }
        public static Group GetByChannel(string userId, Guid channelId)
        {
            int? groupId = Database.ItemFetcher<int?>(@"
SELECT g.id
FROM `group` g
JOIN channel_group cg ON g.id = cg.group_id
WHERE g.user_id = @userId AND cg.channel_id = @channelId",
                dr => dr.GetInt32(0),
                "@userId", userId,
                "@channelId", channelId
            );

            if (groupId.HasValue)
                return Get(userId, groupId.Value);
            return null;
        }

        public void Insert()
        {
            Id = (int)Database.Insert("INSERT INTO `group` (user_id, name, holding_id) VALUES (@userId, @name, @holdingId)",
                "@userId", UserId,
                "@name", Name,
                "@holdingId", HoldingId);
        }
        public bool Update(Group dbGroup)
        {
            using (var conn = Database.Get())
            using (var tran = conn.BeginTransaction())
            {
                if (dbGroup.Name != this.Name || dbGroup.HoldingId != this.HoldingId)
                    Database.ExecuteNonQuery(conn, tran, @"UPDATE `group` SET name = @name, holding_id = @holdingId WHERE id = @id",
                        "@name", this.Name,
                        "@holdingId", this.HoldingId,
                        "@id", this.Id
                    );

                var removedChannels = dbGroup.ChannelIds.Where(c => !this.ChannelIds.Any(c1 => c1 == c)).ToList();
                foreach (var dh in removedChannels)
                    DeleteChannel(conn, tran, dh);

                var insertedChannels = this.ChannelIds.Where(c => !dbGroup.ChannelIds.Any(c1 => c1 == c)).ToList();
                foreach (var dh in insertedChannels)
                    InsertChannel(conn, tran, dh);

                tran.Commit();
                return true;
            }
        }
        public long InsertChannel(MySqlConnection conn, MySqlTransaction tran, Guid channelId)
        {
            return Database.Insert(conn, tran, "INSERT INTO channel_group (channel_id, group_id) VALUES (@channelId, @groupId)",
                "@channelId", channelId,
                "@groupId", this.Id);
        }
        public bool DeleteChannel(MySqlConnection conn, MySqlTransaction tran, Guid channelId)
        {
            return Database.Delete(conn, tran, "DELETE FROM channel_group WHERE channel_id = @channelId AND group_id = @groupId",
                "@channelId", channelId,
                "@groupId", this.Id);
        }
        public bool Delete()
        {
            return Database.Delete("DELETE FROM `group` WHERE id = @id", "@id", Id);
        }

        public void AddChannel(Guid channelId)
        {
            using (var conn = Database.Get())
            using (var tran = conn.BeginTransaction())
            {
                var existsCmd = new MySqlCommand("SELECT COUNT(1) FROM channel_group WHERE channel_id = @channelId AND group_id = @groupId", conn, tran);
                existsCmd.Parameters.AddWithValue("@channelId", channelId.ToString());
                existsCmd.Parameters.AddWithValue("@groupId", Id);
                if ((int)(long)existsCmd.ExecuteScalar() != 1)
                {
                    Database.Insert(conn, tran, "INSERT INTO channel_group (channel_id, group_id) VALUES (@channelId, @groupId)",
                        "@channelId", channelId.ToString(),
                        "@groupId", Id
                    );
                    tran.Commit();
                }
            }
        }
        public bool DeleteChannel(Guid channelId)
        {
            return Database.Delete("DELETE channel_group WHERE  channel_id = @channelId AND group_id = @groupId",
                "@channelId", channelId.ToString(),
                "@groupId", Id
            );
        }
    }
}