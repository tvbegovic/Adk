using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class TaggerCompany
	{
		public Guid Id;
		public string Name;

		public static List<TaggerCompany> Find(string q)
		{
			using (var connection = Database.Get())
			{
				var command = connection.CreateCommand();
				command.CommandText = @"
					SELECT id, company_name FROM advertisers WHERE company_name LIKE @q OR company_name LIKE @q2";
				command.Parameters.AddWithValue("@q", q + '%');
				command.Parameters.AddWithValue("@q2", "% " + q + '%');

				var companies = new List<TaggerCompany>();

				using (var dr = command.ExecuteReader())
				{
					while (dr.Read())
						companies.Add(_FromDataReader(dr));
				}

				return companies;
			}
		}

		public static void LoadOrCreate(Service.IdName company)
		{
			if (company == null || company.Id.HasValue)
				return;

			using (var connection = Database.Get())
			using (var transaction = connection.BeginTransaction())
			{
				_GetIt(transaction, company);

				if (!company.Id.HasValue)
				{
					_Insert(transaction, company);
					transaction.Commit();
				}
			}
		}
		private static void _GetIt(MySqlTransaction transaction, Service.IdName company)
		{
			var command = transaction.Connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT id FROM tagger_company WHERE name = @company_name";
			command.Parameters.AddWithValue("@company_name", company.Name);

			using (var reader = command.ExecuteReader())
			{
				if (reader.Read())
					company.Id = reader.GetInt32(0);
			}
		}
		private static void _Insert(MySqlTransaction transaction, Service.IdName company)
		{
			var command = transaction.Connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"INSERT INTO tagger_company (name) VALUES (@name)";
			command.Parameters.AddWithValue("@name", company.Name);
			command.ExecuteNonQuery();
			company.Id = (int)command.LastInsertedId;
		}

		public static TaggerCompany Create(TaggerUser user, string name)
		{
			using (var connection = Database.Get())
			using (var transaction = connection.BeginTransaction())
			{
				TaggerCompany company = _GetByName(connection, transaction, name);
				if (company == null)
				{
					company = _Create(connection, transaction, user, name);
					transaction.Commit();
				}
				return company;
			}
		}
		private static TaggerCompany _GetByName(MySqlConnection connection, MySqlTransaction transaction, string name)
		{
			TaggerCompany company = null;
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"SELECT id, company_name FROM advertisers WHERE company_name = @name";
			command.Parameters.AddWithValue("@name", name);
			using (var dr = command.ExecuteReader())
			{
				if (dr.Read())
					company = _FromDataReader(dr);
			}
			return company;
		}
		private static TaggerCompany _Create(MySqlConnection connection, MySqlTransaction transaction, TaggerUser user, string name)
		{
			var company = new TaggerCompany
			{
				Id = Guid.NewGuid(),
				Name = name
			};
			var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"INSERT INTO advertisers (id, company_name, created, user_id, status) VALUES (@id, @name, @created, @userId, @status)";
			command.Parameters.AddWithValue("@id", company.Id.ToString());
			command.Parameters.AddWithValue("@name", company.Name);
			command.Parameters.AddWithValue("@created", DateTime.UtcNow);
			command.Parameters.AddWithValue("@userId", user.Id);
			command.Parameters.AddWithValue("@status", Status.New);

			command.ExecuteNonQuery();
			return company;
		}
		private static TaggerCompany _FromDataReader(MySqlDataReader dr)
		{
			return new TaggerCompany
			{
				Id = dr.GetGuid(0),
				Name = dr.GetString(1)
			};
		}
	}
}