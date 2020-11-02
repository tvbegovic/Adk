using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;
using MySql.Data.MySqlClient;

namespace AdK.Tagger.Model
{
	public class Market
	{
		public int Id { get; set; }

		[ColumnNameAttr( "user_id" )]
		public Guid UserId { get; set; }
		public string Name { get; set; }

		public static List<Market> GetUserMarkets( string userId )
		{
			string query = "SELECT id, user_id, name FROM markets WHERE user_id = @user_id";
			return Database.ListFetcher<Market>( query, "@user_id", userId );
		}

		public static List<Market> GetMarketByIds( IList<int?> ids )
		{
			string query = "SELECT id, user_id, name FROM markets WHERE id " + Database.InClause(ids);
			return Database.ListFetcher<Market>( query);
		}

		public static void AddMarket( string userId, string name )
		{
			string query = "INSERT INTO markets (user_id, name)  VALUES(@userId, @name)";
			Database.Insert( query, "@userId", userId, "@name", name );
		}

		public static void UpdateMarket( string userId, int marketId, string marketName )
		{
			using ( var db = Database.Get() ) {
				var command = db.CreateCommand();
				command.CommandText = @"UPDATE markets
												SET name = @name
												WHERE id = @id AND user_id = @userId";

				command.Parameters.AddWithValue( "@id", marketId );
				command.Parameters.AddWithValue( "@userId", userId );
				command.Parameters.AddWithValue( "@name", marketName );
				command.ExecuteNonQuery();
			}

		}

		public static void DeleteMarket( string userId, int marketId )
		{
			using ( var connection = Database.Get() ) {
				using ( var transaction = connection.BeginTransaction() ) {
					DeleteAllMarketChannels( marketId, connection, transaction );
					string query = "DELETE FROM markets WHERE id = @id AND user_id = @userId";
					Database.Delete( connection, transaction, query, "@id", marketId, "@userId", userId );
					transaction.Commit();
				}
			}
		}

		public static void AddMarketChannel( string channelId, int marketId )
		{
			string query = "INSERT IGNORE INTO market_channels (channel_id, market_id)  VALUES(@channelId, @marketId)";
			Database.Insert( query, "@channelId", channelId, "@marketId", marketId );
		}

		public static void DeleteMarketChannel( string channelId, int marketId )
		{
			string query = "DELETE FROM market_channels WHERE market_id = @marketId AND channel_id = @channelId";
			Database.Delete( query, "@channelId", channelId, "@marketId", marketId );
		}

		public static void DeleteAllMarketChannels( int marketId, MySqlConnection connection, MySqlTransaction transaction )
		{
			string query = "DELETE FROM market_channels WHERE market_id = @marketId";
			Database.Delete( connection, transaction, query, "@marketId", marketId );
		}

	}
}
