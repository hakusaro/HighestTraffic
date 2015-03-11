using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Timers;
using MySql.Data.MySqlClient;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace HighestTraffic
{
	[ApiVersion(1, 17)]
    public class Plugin : TerrariaPlugin
    {
		private Timer _updateTimer;
		private Database _database;
		private Config _config;

		private int _realJoins;
		private string _timestamp;
		private int _maxPlayerCount;
		private readonly List<string> _joinedIps = new List<string>();

		public override string Author
		{
			get { return "White"; }
		}

		public override string Description
		{
			get { return "Tracks player count statistics"; }
		}

		public override string Name
		{
			get { return "Highest Traffic"; }
		}

		public override Version Version
		{
			get { return Assembly.GetExecutingAssembly().GetName().Version; }
		}

		public Plugin(Main game) : base(game)
		{
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
			ServerApi.Hooks.ServerLeave.Register(this, OnPlayerLeave);
		}

		private void OnGameInitialize(EventArgs args)
		{
			var path = Path.Combine(TShock.SavePath, "TrafficStats.json");
			(_config = Config.Read(path)).Write(path);

			_database = Database.InitDb("TrafficStats");
			//value*3600000 in order to convert to hours
			_updateTimer = new Timer(_config.interval*3600000);
			_updateTimer.Elapsed += UpdateTimerOnElapsed;

			var table = new SqlTable("Traffic",
				new SqlColumn("ID", MySqlDbType.Int32) {AutoIncrement = true, Primary = true},
				new SqlColumn("Timestamp", MySqlDbType.Text),
				new SqlColumn("UniquePlayers", MySqlDbType.Int32),
				new SqlColumn("AllJoins", MySqlDbType.Int32),
				new SqlColumn("MaxPlayers", MySqlDbType.Int32));

			_database.EnsureTableStructure(table);

			//last entry
			using (var reader =
				_database.QueryReader("SELECT MaxPlayers FROM traffic WHERE ID = (SELECT Max(ID) FROM traffic)"))
			{
				if (reader.Read())
				{
					_maxPlayerCount = reader.Get<int>("MaxPlayers");
				}
				else
				{
					_maxPlayerCount = 0;
				}
			}

			_updateTimer.Start();
			_timestamp = DateTime.UtcNow.ToString("G");
		}

		private void OnGreetPlayer(GreetPlayerEventArgs args)
		{
			var player = TShock.Players[args.Who];
			if (player == null)
			{
				return;
			}

			_realJoins++;

			if (!_joinedIps.Contains(player.IP))
			{
				_joinedIps.Add(player.IP);
			}

			var count = TShock.Utils.ActivePlayers();
			if (count > _maxPlayerCount)
			{
				_maxPlayerCount = count;

				TSPlayer.All.SendSuccessMessage("New highest player count! {0}/{1}", _maxPlayerCount,
					TShock.Config.MaxSlots);
			}
		}

		private void OnPlayerLeave(LeaveEventArgs args)
		{
			var player = TShock.Players[args.Who];
			if (player == null)
			{
				return;
			}

			if (_joinedIps.Contains(player.IP))
			{
				_joinedIps.Remove(player.IP);
			}
		}

		private void UpdateTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
		{
			_database.Query(
				"INSERT INTO traffic (Timestamp, UniquePlayers, AllJoins, MaxPlayers) VALUES (@0, @1, @2, @3)",
				_timestamp, _joinedIps.Count, _realJoins, _maxPlayerCount);
			_realJoins = 0;
			_timestamp = DateTime.UtcNow.ToString("G");
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
			}
			base.Dispose(disposing);
		}
    }
}