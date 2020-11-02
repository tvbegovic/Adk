using DatabaseCommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace AdK.Tagger.Model
{
	public class TaggerAttribute
	{
		public Guid Id;
		public string Name;
		public AttributeType Type;

		public enum AttributeType
		{
			Brand,
			Company
		}
		public static List<TaggerAttribute> Get(int tagId)
		{
			var attributes = new List<TaggerAttribute>();
			using (var connection = Database.Get())
			using (var transaction = connection.BeginTransaction())
			{
				attributes.AddRange(_getAttributes(connection, transaction, tagId, AttributeType.Brand));
				attributes.AddRange(_getAttributes(connection, transaction, tagId, AttributeType.Company));
			}
			return attributes;
		}
		public static void Add(int tagId, AttributeType attributeType, Guid attributeId)
		{
			using (var connection = Database.Get())
			using (var transaction = connection.BeginTransaction())
			{
				bool exists = _existsAttribute(connection, transaction, tagId, attributeId, attributeType);
				if (!exists)
				{
					_addAttribute(connection, transaction, tagId, attributeId, attributeType);
					transaction.Commit();
				}
			}
		}
		public static void Remove(int tagId, AttributeType attributeType, Guid attributeId)
		{
			using (var connection = Database.Get())
				_removeAttribute(connection, tagId, attributeId, attributeType);
		}

		private static List<TaggerAttribute> _getAttributes(MySqlConnection connection, MySqlTransaction transaction, int tagId, AttributeType attributeType)
		{
			var attributes = new List<TaggerAttribute>();
			var command = connection.CreateCommand();
			command.Transaction = transaction;

			switch (attributeType)
			{
				case AttributeType.Brand:
					command.CommandText = @"
						SELECT tagger_tag_brands.brand_id, brands.brand_name
						FROM tagger_tag_brands
						INNER JOIN brands ON tagger_tag_brands.brand_id = brands.id
						WHERE tag_id = @tag_id";
					break;
				case AttributeType.Company:
					command.CommandText = @"
						SELECT tagger_tag_companies.company_id, advertisers.company_name
						FROM tagger_tag_companies
						INNER JOIN advertisers ON tagger_tag_companies.company_id = advertisers.id
						WHERE tag_id = @tag_id";
					break;
			}
	
			command.Parameters.AddWithValue("@tag_id", tagId);
			using (var reader = command.ExecuteReader())
				while (reader.Read())
					attributes.Add(new TaggerAttribute
					{
						Id = reader.GetGuid(0),
						Name = reader.GetString(1),
						Type = attributeType
					});
			return attributes;
		}

		private static bool _existsAttribute(MySqlConnection connection, MySqlTransaction transaction, int tagId, Guid attributeId, AttributeType attributeType)
		{
			bool exists;
			var command = connection.CreateCommand();
			command.Transaction = transaction;

			switch (attributeType)
			{
				case AttributeType.Brand:
					command.CommandText = @"SELECT tag_id, brand_id FROM tagger_tag_brands WHERE tag_id = @tag_id AND brand_id = @attribute_id";
					break;
				case AttributeType.Company:
					command.CommandText = @"SELECT tag_id, company_id FROM tagger_tag_companies WHERE tag_id = @tag_id AND company_id = @attribute_id";
					break;
			}

			command.Parameters.AddWithValue("@tag_id", tagId);
			command.Parameters.AddWithValue("@attribute_id", attributeId.ToString());
			using (var reader = command.ExecuteReader())
				exists = reader.Read();
			return exists;
		}

		private static void _addAttribute(MySqlConnection connection, MySqlTransaction transaction, int tagId, Guid attributeId, AttributeType attributeType)
		{
			var command = connection.CreateCommand();
			command.Transaction = transaction;

			switch (attributeType)
			{
				case AttributeType.Brand:
					command.CommandText = @"INSERT INTO tagger_tag_brands (tag_id, brand_id) VALUES (@tag_id, @attribute_id)";
					break;
				case AttributeType.Company:
					command.CommandText = @"INSERT INTO tagger_tag_companies (tag_id, company_id) VALUES (@tag_id, @attribute_id)";
					break;
			}

			command.Parameters.AddWithValue("@tag_id", tagId);
			command.Parameters.AddWithValue("@attribute_id", attributeId.ToString());
			command.ExecuteNonQuery();
		}

		private static void _removeAttribute(MySqlConnection connection, int tagId, Guid attributeId, AttributeType attributeType)
		{
			var command = connection.CreateCommand();

			switch (attributeType)
			{
				case AttributeType.Brand:
					command.CommandText = @"DELETE FROM tagger_tag_brands WHERE tag_id = @tag_id AND brand_id = @attribute_id";
					break;
				case AttributeType.Company:
					command.CommandText = @"DELETE FROM tagger_tag_companies WHERE tag_id = @tag_id AND company_id = @attribute_id";
					break;
			}

			command.Parameters.AddWithValue("@tag_id", tagId);
			command.Parameters.AddWithValue("@attribute_id", attributeId.ToString());
			command.ExecuteNonQuery();
		}
	}
}