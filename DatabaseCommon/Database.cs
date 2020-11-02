using System.Globalization;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using NLog;
using System.Collections;

namespace DatabaseCommon
{
    public class Database
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public string ConnectionString;

        public static Database Instance = _CreateInstance();

        public static Database _CreateInstance()
        {
            var instance = new Database
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["pkdb"].ConnectionString
            };
            return instance;
        }

        public static MySqlConnection Get()
        {
            var db = new MySqlConnection(Instance.ConnectionString);
            db.Open();
            return db;
        }

        /// <summary>
        /// Returns a list of T using the filler method applied to the result of the provided query
        /// </summary>
        /// <example>
        /// Database.ListFetcher&lt;Guid&gt;("SELECT id FROM advertisers WHERE company_name = @company_name", dr => dr.GetNullableGuid("id"), new object[] { "@company_name", advertiserName });
        /// </example>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="fillerMethod"></param>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        public static List<T> ListFetcher<T>(string query, Func<MySqlDataReader, T> fillerMethod, params object[] queryParams)
        {
            using (var conn = Database.Get())
                return ListFetcher<T>(conn, null, query, fillerMethod, queryParams);
        }

        public static List<T> ListFetcher<T>(MySqlConnection conn, MySqlTransaction tran, string query, Func<MySqlDataReader, T> fillerMethod, params object[] queryParams)
        {
            var stopWatch = Stopwatch.StartNew();

            var c = new MySqlCommand(query, conn, tran);
            FillCommandParameters(queryParams, c);
            List<T> retval = new List<T>();
			try
			{
				using (MySqlDataReader dr = c.ExecuteReader())
				{
					while (dr.Read())
					{
						retval.Add(fillerMethod(dr));
					}
				}
			}
			finally
			{
				LogQuery(query, queryParams, stopWatch);
			}
            
            return retval;
        }

        public static List<T> ListFetcher<T>(string query, params object[] queryParams) where T : class
        {
            using (var conn = Database.Get())
            {
                return ListFetcher<T>(conn, null, query, queryParams);
            }
        }

        public static List<T> ListFetcher<T>(MySqlConnection conn, MySqlTransaction tran, string query, params object[] queryParams) where T : class
        {
            var stopWatch = Stopwatch.StartNew();

            var c = new MySqlCommand(query, conn, tran);
            FillCommandParameters(queryParams, c);
			List<T> list = null;
			try
			{
				using (MySqlDataReader dr = c.ExecuteReader())
				{
					list = dr.ReadList<T>();					
				}
			}
			finally
			{
				LogQuery(query, queryParams, stopWatch);
			}
			return list;
        }

        /// <summary>
        /// Executes method for each record returned by the query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="method"></param>
        /// <param name="queryParams"></param>
        public static void ForEach(string query, Action<MySqlDataReader> method, params object[] queryParams)
        {
            using (var conn = Database.Get())
                ForEach(conn, null, query, method, queryParams);
        }
        public static void ForEach(MySqlConnection conn, MySqlTransaction tran, string query, Action<MySqlDataReader> method, params object[] queryParams)
        {
            var c = new MySqlCommand(query, conn, tran);
            FillCommandParameters(queryParams, c);

            using (MySqlDataReader dr = c.ExecuteReader())
            {
                while (dr.Read())
                    method(dr);
            }
        }
        public static T ItemFetcher<T>(string query, Func<MySqlDataReader, T> fillerMethod, params object[] queryParams)
        {
            using (var conn = Database.Get())
                return ItemFetcher<T>(conn, null, query, fillerMethod, queryParams);
        }
        public static T ItemFetcher<T>(MySqlConnection conn, MySqlTransaction tran, string query, Func<MySqlDataReader, T> fillerMethod, params object[] queryParams)
        {
            var c = new MySqlCommand(query, conn, tran);
            FillCommandParameters(queryParams, c);

            using (MySqlDataReader dr = c.ExecuteReader())
            {
                if (dr.Read())
                    return fillerMethod(dr);
            }
            return default(T);
        }

        public static T ItemFetcher<T>(string query, params object[] queryParams) where T : class, new()
        {
            using (var conn = Database.Get())
            {
                return ItemFetcher<T>(conn, null, query, queryParams);
            }
        }
        public static T ItemFetcher<T>(MySqlConnection conn, MySqlTransaction tran, string query, params object[] queryParams) where T : class, new()
        {
            var c = new MySqlCommand(query, conn, tran);
            FillCommandParameters(queryParams, c);
            using (MySqlDataReader dr = c.ExecuteReader())
            {
                return dr.ReadSingleRow<T>();
            }

        }


        public static int Count(string query, params object[] queryParams)
        {
            using (var conn = Database.Get())
                return Count(conn, null, query, queryParams);
        }
        public static int Count(MySqlConnection conn, MySqlTransaction tran, string query, params object[] queryParams)
        {
            var c = new MySqlCommand(query, conn, tran);
            FillCommandParameters(queryParams, c);
            var o = c.ExecuteScalar();
            if (o is long)
                return (int)(long)o;
            if (o is int)
                return (int)o;
            return 0;
        }
        public static bool Exists(string query, params object[] queryParams)
        {
            using (var conn = Database.Get())
                return Exists(conn, null, query, queryParams);
        }
        public static bool Exists(MySqlConnection conn, MySqlTransaction tran, string query, params object[] queryParams)
        {
            var c = new MySqlCommand(query, conn, tran);
            FillCommandParameters(queryParams, c);
            using (var dr = c.ExecuteReader())
                return dr.Read();
        }
        public static long Insert(string query, params object[] queryParams)
        {
            using (var conn = Database.Get())
                return Insert(conn, null, query, queryParams);
        }
        public static long Insert(MySqlTransaction tr, string query, params object[] queryParams)
        {
            using (var conn = Database.Get())
                return Insert(conn, tr, query, queryParams);
        }
        public static long Insert(MySqlConnection conn, MySqlTransaction tran, string query, params object[] queryParams)
        {
            var c = new MySqlCommand(query, conn, tran);
            FillCommandParameters(queryParams, c);
            c.ExecuteNonQuery();
            return c.LastInsertedId;
        }
        public static int ExecuteNonQuery(string query, params object[] queryParams)
        {
            using (var conn = Get())
                return ExecuteNonQuery(conn, null, query, queryParams);
        }
        public static int ExecuteNonQuery(MySqlConnection conn, MySqlTransaction tran, string query, params object[] queryParams)
        {
            var c = new MySqlCommand(query, conn, tran);
            FillCommandParameters(queryParams, c);
			var stopWatch = Stopwatch.StartNew();
            var result = c.ExecuteNonQuery();
			LogCommand(c, stopWatch);
			return result;
        }
        public static bool Delete(string query, params object[] queryParams)
        {
            using (var conn = Database.Get())
                return Delete(conn, null, query, queryParams);
        }
        public static bool Delete(MySqlConnection conn, MySqlTransaction tran, string query, params object[] queryParams)
        {
            var c = new MySqlCommand(query, conn, tran);
            FillCommandParameters(queryParams, c);
            int rowsDeleted = c.ExecuteNonQuery();
            return rowsDeleted > 0;
        }
        private static void FillCommandParameters(object[] parameters, MySqlCommand c)
        {
            if (parameters != null)
            {
                for (int n = 0; n < parameters.Length; n += 2)
                {
                    if (parameters[n + 1] is Guid)
                    {
                        c.Parameters.AddWithValue((string)parameters[n], parameters[n + 1].ToString());
                    }
                    else
                    {
                        c.Parameters.AddWithValue((string)parameters[n], parameters[n + 1]);
                    }
                }
            }
        }

        public static string InClause(IEnumerable<string> items)
        {
            Debug.Assert(items.Any(), "SQL IN clause must not be empty");

            //return "IN (" + string.Join( ",", items ) + ")";
            var joinItems = items.Select(i => String.Format("'{0}'", i));

            return String.Format("IN ({0})", string.Join(",", joinItems));
        }
        public static string InClause(IEnumerable<Guid> items)
        {
            return InClause(items.Select(id => id.ToString()));
        }

        public static string InClause(IEnumerable<DateTime> dates)
        {
            return InClause(dates.Select(date => date.ToString("yyy-MM-dd")));
        }

		public static string InClause<T>(IEnumerable<T> items)
		{
			return String.Format("IN ({0})", string.Join(",", items.Where(x=> x != null)));
		}

		public static void LogQuery(string query, object[] parameters, Stopwatch stopWatch)
        {
            stopWatch.Stop();
            var elapsedMs = stopWatch.ElapsedMilliseconds;

            Log.Info<string>(string.Format("sql (time: {0}ms): {1}", elapsedMs, query));

            if (parameters != null)
            {
                for (int n = 0; n < parameters.Length; n += 2)
                    Log.Info<object, object>("{0}={1}", parameters[n], parameters[n + 1]);
            }

        }

		public static void LogCommand(MySqlCommand command, Stopwatch stopWatch)
        {
            stopWatch.Stop();
            var elapsedMs = stopWatch.ElapsedMilliseconds;

            Log.Info<string>(string.Format("sql (time: {0}ms): {1}", elapsedMs, command.CommandText));

            if (command.Parameters != null)
            {
                for (int n = 0; n < command.Parameters.Count; n ++)
                    Log.Info<object, object>("{0}={1}", command.Parameters[n].ParameterName, command.Parameters[n].Value);
            }

        }
    }

    public static class DataReaderExtensions
    {
        public static int? GetNullableInt(this MySqlDataReader reader, string columnName)
        {
            return
                reader[columnName] == DBNull.Value ?
                (int?)null :
                reader.GetInt32(columnName);
        }
        public static int? GetNullableInt(this MySqlDataReader reader, int iColumn)
        {
            return
                reader.IsDBNull(iColumn) ?
                (int?)null :
                reader.GetInt32(iColumn);
        }

        public static string GetNullableString(this MySqlDataReader reader, string columnName)
        {
            return
                reader[columnName] == DBNull.Value ?
                null :
                reader.GetString(columnName);
        }
        public static string GetNullableString(this MySqlDataReader reader, int iColumn)
        {
            return
                reader.IsDBNull(iColumn) ?
                null :
                reader.GetString(iColumn);
        }

        public static Guid GetNullableGuid(this MySqlDataReader reader, string columnName)
        {
            return
                reader[columnName] == DBNull.Value ?
                Guid.Empty :
                reader.GetGuid(columnName);
        }
        public static Guid GetNullableGuid(this MySqlDataReader reader, int iColumn)
        {
            return
                reader.IsDBNull(iColumn) ?
                Guid.Empty :
                reader.GetGuid(iColumn);
        }
        public static DateTime? GetNullableDate(this MySqlDataReader reader, int iColumn, DateTimeKind kind = DateTimeKind.Utc)
        {
            return
                reader.IsDBNull(iColumn) ?
                (DateTime?)null :
                DateTime.SpecifyKind(reader.GetDateTime(iColumn), kind);
        }
        public static double? GetNullableDouble(this MySqlDataReader reader, int iColumn)
        {
            return
                reader.IsDBNull(iColumn) ?
                (double?)null :
                reader.GetDouble(iColumn);
        }
    }

    public static class MySqlConnectionExtensions
    {
        public static bool RecordExists(this MySqlConnection db, MySqlTransaction transaction, string table, string keyColumn, object keyValue)
        {
            string sql = string.Format("SELECT COUNT(1) FROM {0} where {1} = @value", table, keyColumn);
            var command = new MySqlCommand(sql, db, transaction);
            command.Parameters.AddWithValue("@value", keyValue);
            return (int)(long)command.ExecuteScalar() == 1;
        }
    }

    public enum Status : byte
    {
        None = 0,
        New = 1,
        Validated = 2
    };
}