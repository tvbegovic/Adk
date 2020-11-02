using MySql.Data.MySqlClient;

namespace AdK.Tagger.Model
{
	public class TaggerMergeLog
	{
		public static void Log(MySqlConnection connection, MySqlTransaction transaction, TaggerUser user, TaggerTag master, TaggerTag slave, bool isSplit)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"INSERT INTO tagger_merge_log (merging_user_id, is_split, master_tag_id, master_tag_name, slave_tag_id, slave_tag_name)
				VALUES (@merging_user_id, @is_split, @master_tag_id, @master_tag_name, @slave_tag_id, @slave_tag_name)";
			command.Parameters.AddWithValue("@merging_user_id", user.Id);
			command.Parameters.AddWithValue("@is_split", isSplit);
			command.Parameters.AddWithValue("@master_tag_id", master.Id);
			command.Parameters.AddWithValue("@master_tag_name", master.Name);
			command.Parameters.AddWithValue("@slave_tag_id", slave.Id);
			command.Parameters.AddWithValue("@slave_tag_name", slave.Name);
			command.ExecuteNonQuery();
		}
	}
}