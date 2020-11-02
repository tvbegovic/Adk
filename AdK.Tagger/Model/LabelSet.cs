using DatabaseCommon;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class LabelSet
	{
		public string SetName;
		public LabelSet(string setName)
		{
			SetName = setName;
		}
		public List<string> GetMatches(string search)
		{
			var matches = new List<string>();
			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = @"SELECT label FROM ad_label_set WHERE setName = @setName AND (label like @search OR label like @search2)";
				command.Parameters.AddWithValue("@setName", SetName);
				command.Parameters.AddWithValue("@search", search + "%"); // Starting with
				command.Parameters.AddWithValue("@search2", "% " + search + "%"); // or word starting with

				using (var dr = command.ExecuteReader())
				{
					while (dr.Read())
						matches.Add(dr.GetString(0));
				}
			}
			return matches;
		}
		public void InsertIfNew(string label)
		{
			if (string.IsNullOrEmpty(label)) return;

			using (var db = Database.Get())
			using (var transaction = db.BeginTransaction())
			{
				var existsCommand = db.CreateCommand();
				existsCommand.Transaction = transaction;
				existsCommand.CommandText = @"SELECT COUNT(*) FROM ad_label_set WHERE setName = @setName AND label = @label";
				existsCommand.Parameters.AddWithValue("@setName", SetName);
				existsCommand.Parameters.AddWithValue("@label", label);
				bool exists = (long)existsCommand.ExecuteScalar() > 0;

				if (!exists)
				{
					var insertCommand = db.CreateCommand();
					insertCommand.Transaction = transaction;
					insertCommand.CommandText = @"INSERT INTO ad_label_set (setName, label) VALUES (@setName, @label)";
					insertCommand.Parameters.AddWithValue("@setName", SetName);
					insertCommand.Parameters.AddWithValue("@label", label);
					insertCommand.ExecuteNonQuery();

					transaction.Commit();
				}
			}
		}
	}
}