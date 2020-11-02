using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class TaggerIndustry
	{
		public Guid Id;
		public string Name;

		public static List<TaggerIndustry> Find(string q)
		{
			using (var connection = Database.Get())
			{
				var command = connection.CreateCommand();
				command.CommandText = @"
					SELECT id, industry_name FROM industries WHERE industry_name LIKE @q OR industry_name LIKE @q2";
				command.Parameters.AddWithValue("@q", q + '%');
				command.Parameters.AddWithValue("@q2", "% " + q + '%');

				var industries = new List<TaggerIndustry>();

				using (var dr = command.ExecuteReader())
				{
					while (dr.Read())
						industries.Add(new TaggerIndustry
						{
							Id = dr.GetGuid(0),
							Name = dr.GetString(1)
						});
				}

				return industries;
			}
		}
		public static List<TaggerIndustry> GetAll(IList<Guid> ids = null)
		{
			return Database.ListFetcher(string.Format("SELECT id, industry_name FROM industries {0}", ids != null ? "WHERE id " + Database.InClause(ids) : "") ,
			dr => new TaggerIndustry
			{
				Id = dr.GetGuid("id"),
				Name = dr.GetString("industry_name")
			});
		}

		public static void LoadOrCreate(Service.IdName industry)
		{
			if (industry == null || industry.Id.HasValue)
				return;

			using (var connection = Database.Get())
			{
				using (var transaction = connection.BeginTransaction())
				{
					var command = connection.CreateCommand();
					command.Transaction = transaction;
					command.CommandText = @"SELECT id FROM tagger_industry WHERE name = @industry_name";
					command.Parameters.AddWithValue("@industry_name", industry.Name);

					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
							industry.Id = reader.GetInt32(0);
					}

					if (!industry.Id.HasValue)
						_Insert(transaction, industry);
				}
			}
		}
		private static void _GetIt(MySqlTransaction transaction, Service.IdName industry)
		{
			var command = transaction.Connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT id FROM tagger_industry WHERE name = @industry_name";
			command.Parameters.AddWithValue("@industry_name", industry.Name);

			using (var reader = command.ExecuteReader())
			{
				if (reader.Read())
					industry.Id = reader.GetInt32(0);
			}
		}
		private static void _Insert(MySqlTransaction transaction, Service.IdName industry)
		{
			var command = transaction.Connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"INSERT INTO tagger_industry (name) VALUES (@name)";
			command.Parameters.AddWithValue("@name", industry.Name);
			command.ExecuteNonQuery();
			industry.Id = (int)command.LastInsertedId;
		}
	}
}
