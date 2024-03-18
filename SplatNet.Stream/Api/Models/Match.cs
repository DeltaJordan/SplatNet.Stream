using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SplatNet.Stream.Api.Models
{
	public class Match
	{
		public string Id { get; set; }

		public Team MyTeam { get; set; } = new()
		{
			InkColor = Brushes.Purple
		};
		public Team EnemyTeam { get; set; } = new()
		{
			InkColor = Brushes.Green
		};

		public TimeSpan Duration { get; set; }
		public DateTime Time { get; set; }

		public string Map { get; set; }
		public string MapUrl { get; set; }
		public string Mode { get; set; }
		public string ModeShortHand => GetModeShortHand(this.Mode);
		public string ModeUrl { get; set; }

		public static Match Load(string json)
		{
			Match result = new();

			JToken token = JToken.Parse(json);
			JToken historyDetail = token["data"]["vsHistoryDetail"];
			result.Id = historyDetail["id"].Value<string>();
			result.Time = historyDetail["playedTime"].Value<DateTime>();
			result.Duration = TimeSpan.FromSeconds(historyDetail["duration"].Value<int>());
			result.Mode = historyDetail["vsRule"]["name"].Value<string>();
			result.Map = historyDetail["vsStage"]["name"].Value<string>();
			result.MapUrl = historyDetail["vsStage"]["image"]["url"].Value<string>();

			JToken myTeamColorToken = historyDetail["myTeam"]["color"];
			Color myTeamInkColor = Color.FromScRgb(
					1.0F,
					myTeamColorToken["r"].Value<float>(), 
					myTeamColorToken["g"].Value<float>(), 
					myTeamColorToken["b"].Value<float>()
				);
			Brush brush = new SolidColorBrush(myTeamInkColor);
			brush.Freeze();
			result.MyTeam.InkColor = brush;
			result.MyTeam.Judgement = historyDetail["myTeam"]["judgement"].Value<string>();
			if (historyDetail["myTeam"]["result"].Value<JToken>().HasValues)
			{
				result.MyTeam.Score = historyDetail["myTeam"]["result"]["score"].Value<int?>() ?? 0;
			}

			int i = 0;
			foreach (var playerToken in historyDetail["myTeam"]["players"])
			{
				Player player = new()
				{
					Name = playerToken["name"].Value<string>(),
					Tag = playerToken["nameId"].Value<string>(),
					Paint = playerToken["paint"].Value<int>(),
					WeaponName = playerToken["weapon"]["name"].Value<string>(),
					WeaponUrl = playerToken["weapon"]["image"]["url"].Value<string>()
				};
				player.IsAlias = string.IsNullOrWhiteSpace(player.Tag);

				JToken resultToken = playerToken["result"];
				if (resultToken.HasValues) // This object is empty during a disconnect.
				{
					player.Assists = resultToken["assist"].Value<int>();
					player.Kills = resultToken["kill"].Value<int>() - player.Assists;
					player.Deaths = resultToken["death"].Value<int>();
					player.Specials = resultToken["special"].Value<int>();
				}

				result.MyTeam.Players[i++] = player;

				player.Team = result.MyTeam;
			}

			JToken enemyTeamColorToken = historyDetail["otherTeams"][0]["color"];
			Color enemyTeamInkColor = Color.FromScRgb(
					1.0F,
					enemyTeamColorToken["r"].Value<float>(),
					enemyTeamColorToken["g"].Value<float>(),
					enemyTeamColorToken["b"].Value<float>()
				);
			Brush brush2 = new SolidColorBrush(enemyTeamInkColor);
			brush2.Freeze();
			result.EnemyTeam.InkColor = brush2;
			result.EnemyTeam.Judgement = historyDetail["otherTeams"][0]["judgement"].Value<string>();
			if (historyDetail["otherTeams"][0]["result"].Value<JToken>().HasValues)
			{
				result.EnemyTeam.Score = historyDetail["otherTeams"][0]["result"]["score"].Value<int?>() ?? 0;
			}

			i = 0;
			foreach (var playerToken in historyDetail["otherTeams"][0]["players"])
			{
				Player player = new()
				{
					Name = playerToken["name"].Value<string>(),
					Tag = playerToken["nameId"].Value<string>(),
					Paint = playerToken["paint"].Value<int>(),
					WeaponName = playerToken["weapon"]["name"].Value<string>(),
					WeaponUrl = playerToken["weapon"]["image"]["url"].Value<string>()
				};
				player.IsAlias = string.IsNullOrWhiteSpace(player.Tag);

				JToken resultToken = playerToken["result"];
				if (resultToken.HasValues) // This object is empty during a disconnect.
				{
					player.Assists = resultToken["assist"].Value<int>();
					player.Kills = resultToken["kill"].Value<int>() - player.Assists;
					player.Deaths = resultToken["death"].Value<int>();
					player.Specials = resultToken["special"].Value<int>();
				}

				result.EnemyTeam.Players[i++] = player;

				player.Team = result.EnemyTeam;
			}

			return result;
		}

		private static string GetModeShortHand(string mode) => mode switch
		{
			"Splat Zones" => "SZ",
			"Rainmaker" => "RM",
			"Clam Blitz" => "CB",
			"Tower Control" => "TC",
			_ => "UNKNOWN",
		};
	}
}
