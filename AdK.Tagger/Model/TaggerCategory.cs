using DatabaseCommon;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class TaggerCategory
	{
		public int Id;
		public Guid? CreatorId;
		public Guid? CategoryId;
		public string Name;
		public int? TaggerIndustryId;

		public static int GetNew()
		{
			int newCategoryCount;

			using (var connection = Database.Get())
			{
				using (var transaction = connection.BeginTransaction())
				{
					var command = connection.CreateCommand();
					command.Transaction = transaction;
					command.CommandText = @"
						SELECT id, category_name FROM ad_categories
						WHERE
							NOT EXISTS (SELECT 1 FROM tagger_category WHERE tagger_category.category_id = ad_categories.id)";

					var categories = new List<TaggerCategory>();
					using (var dr = command.ExecuteReader())
					{
						while (dr.Read())
						{
							categories.Add(new TaggerCategory
							{
								CategoryId = dr.GetGuid(0),
								Name = dr.GetString(1)
							});
						}
					}
					newCategoryCount = categories.Count;

					foreach (var category in categories)
					{
						var insertCommand = connection.CreateCommand();
						insertCommand.Transaction = transaction;
						insertCommand.CommandText = @"INSERT INTO tagger_category (category_id, name) VALUES (@category_id, @name)";
						insertCommand.Parameters.AddWithValue("@category_id", category.CategoryId.ToString());
						insertCommand.Parameters.AddWithValue("@name", category.Name);
						insertCommand.ExecuteNonQuery();
						category.Id = (int)insertCommand.LastInsertedId;
					}

					transaction.Commit();
				}
			}

			return newCategoryCount;
		}

		public static void LoadOrCreate(Service.IdName category)
		{
			if (category == null || category.Id != 0)
				return;

			using (var connection = Database.Get())
			{
				using (var transaction = connection.BeginTransaction())
				{
					var command = connection.CreateCommand();
					command.Transaction = transaction;
					command.CommandText = @"SELECT id FROM tagger_category WHERE name = @category_name";
					command.Parameters.AddWithValue("@category_name", category.Name);

					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
							category.Id = reader.GetInt32(0);
					}

					if (category.Id == 0)
					{
						var insertCommand = connection.CreateCommand();
						insertCommand.Transaction = transaction;
						insertCommand.CommandText = @"INSERT INTO tagger_category (name) VALUES (@name)";
						insertCommand.Parameters.AddWithValue("@name", category.Name);
						insertCommand.ExecuteNonQuery();
						category.Id = (int)insertCommand.LastInsertedId;

						transaction.Commit();
					}
				}
			}
		}
	}
}