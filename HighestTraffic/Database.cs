using System;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace HighestTraffic
{
	public class Database
	{
		private static IDbConnection _db;

		private Database(IDbConnection db)
		{
			_db = db;
		}

		internal QueryResult QueryReader(string query, params object[] args)
		{
			return _db.QueryReader(query, args);
		}

		internal int Query(string query, params object[] args)
		{
			return _db.Query(query, args);
		}

		internal void EnsureTableStructure(SqlTable table)
		{
			var creator = new SqlTableCreator(_db,
				_db.GetSqlType() == SqlType.Sqlite
					? (IQueryBuilder)new SqliteQueryCreator()
					: new MysqlQueryCreator());

			creator.EnsureTableStructure(table);
		}

		internal void EnsureTableStructure(params SqlTable[] tables)
		{
			foreach (var table in tables)
			{
				EnsureTableStructure(table);
			}
		}

		public static Database InitDb(string name)
		{
			IDbConnection idb;

			if (TShock.Config.StorageType.ToLower() == "sqlite")
			{
				idb =
					new SqliteConnection(string.Format("uri=file://{0},Version=3",
						Path.Combine(TShock.SavePath, name + ".sqlite")));
			}

			else if (TShock.Config.StorageType.ToLower() == "mysql")
			{
				try
				{
					var host = TShock.Config.MySqlHost.Split(':');
					idb = new MySqlConnection
					{
						ConnectionString = String.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
							host[0],
							host.Length == 1 ? "3306" : host[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword
							)
					};
				}
				catch (MySqlException x)
				{
					TShock.Log.Error(x.ToString());
					throw new Exception("MySQL not setup correctly.");
				}
			}
			else
			{
				throw new Exception("Invalid storage type.");
			}

			var db = new Database(idb);
			return db;
		}
	}
}