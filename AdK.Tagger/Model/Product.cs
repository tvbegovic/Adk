using DatabaseCommon;
using System;
using System.Collections.Generic;


namespace AdK.Tagger.Model
{
	public class Product
	{
		public Guid Id;
		public string Name;

		public static List<Product> GetAll()
		{
			using (var connection = Database.Get())
			{
				var command = connection.CreateCommand();
				command.CommandText = @"SELECT id, product_name FROM products";

				var products = new List<Product>();

				using (var dr = command.ExecuteReader())
				{
					while (dr.Read())
						products.Add(new Product
						{
							Id = dr.GetGuid(0),
							Name = dr.GetString(1)
						});
				}

				return products;
			}
		}

		public static List<Product> GetByIds(IList<string> ids)
		{
			return Database.ListFetcher<Product>("SELECT id,product_name FROM products WHERE id " + Database.InClause(ids),
				dr=> new Product
				{
					Id = dr.GetGuid(0),
					Name = dr.GetString(1)
				}
				);
		}


        public static void AddProduct(string name)
        {
            var id = Guid.NewGuid().ToString();
            string query = "INSERT INTO products(id, product_name)  VALUES(@id, @name)";
            Database.Insert(query, "@id", id, "@name", name);
        }
    }
}