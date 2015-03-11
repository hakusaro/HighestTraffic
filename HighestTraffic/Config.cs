using System.IO;
using Newtonsoft.Json;

namespace HighestTraffic
{
	public class Config
	{
		public double interval = 1.0;

		public void Write(string path)
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
		}

		public static Config Read(string path)
		{
			return !File.Exists(path)
				? new Config()
				: JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
		}
	}
}
