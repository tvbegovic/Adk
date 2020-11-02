using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class TaggerBrand
	{
		public Guid Id;
		public string Name;

		public static List<TaggerBrand> Find(string q)
		{
			using (var connection = Database.Get())
			{
				var command = connection.CreateCommand();
				command.CommandText = @"
					SELECT id, brand_name FROM brands WHERE brand_name LIKE @q OR brand_name LIKE @q2";
				command.Parameters.AddWithValue("@q", q + '%');
				command.Parameters.AddWithValue("@q2", "% " + q + '%');

				var brands = new List<TaggerBrand>();

				using (var dr = command.ExecuteReader())
				{
					while (dr.Read())
						brands.Add(_FromDataReader(dr));
				}

				return brands;
			}
		}
		public static TaggerBrand Create(TaggerUser user, string name)
		{
			using (var connection = Database.Get())
			using (var transaction = connection.BeginTransaction())
			{
				TaggerBrand brand = _GetByName(connection, transaction, name);
				if (brand == null)
				{
					brand = _Create(connection, transaction, user, name);
					transaction.Commit();
				}
				return brand;
			}
		}
		private static TaggerBrand _GetByName(MySqlConnection connection, MySqlTransaction transaction, string name)
		{
			TaggerBrand brand = null;
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT id, brand_name FROM brands WHERE brand_name = @name";
			command.Parameters.AddWithValue("@name", name);
			using (var dr = command.ExecuteReader())
			{
				if (dr.Read())
					brand = _FromDataReader(dr);
			}
			return brand;
		}
		private static TaggerBrand _Create(MySqlConnection connection, MySqlTransaction transaction, TaggerUser user, string name)
		{
			var brand = new TaggerBrand
			{
				Id = Guid.NewGuid(),
				Name = name
			};
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"INSERT INTO brands (id, brand_name, created, user_id, status) VALUES (@id, @name, @created, @userId, @status)";
			command.Parameters.AddWithValue("@id", brand.Id.ToString());
			command.Parameters.AddWithValue("@name", brand.Name);
			command.Parameters.AddWithValue("@created", DateTime.UtcNow);
			command.Parameters.AddWithValue("@userId", user.Id);
			command.Parameters.AddWithValue("@status", Status.New);

			command.ExecuteNonQuery();
			return brand;
		}
		private static TaggerBrand _FromDataReader(MySqlDataReader dr)
		{
			return new TaggerBrand
			{
				Id = dr.GetGuid(0),
				Name = dr.GetString(1)
			};
		}
	}
}