using Newtonsoft.Json.Linq;
using SplatNet.Stream.Api.Models;
using SplatNet.Stream.Extensions;
using SplatNet.Stream.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace SplatNet.Stream.Api.Core
{
	public static class SplatApiFetch
	{

		public static async Task<bool> PrefetchAsync()
		{
			await SplatApiAuth.GetWebViewVersionAsync().SafeAsync();

			string sha = SplatApiShared.TranslateRid["HomeQuery"];
			HttpRequestMessage request = new()
			{
				RequestUri = new Uri(SplatApiShared.GRAPHQL_URL),
				Method = HttpMethod.Post,
				Content = SplatApiShared.GenGraphQLBody(sha, "naCountry", SplatApiShared.Credentials.Country)
			};
			
			await SplatApiShared.HeadbuttAsync(request).SafeAsync();

			HttpResponseMessage response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
			if (response.StatusCode != HttpStatusCode.OK)
			{
				return await GenNewTokensAsync().SafeAsync();
			}

			return true;
		}

		private static async Task<bool> GenNewTokensAsync()
		{
			try
			{
				(string newGToken, string accountName, string accountLang, string accountCountry) =
					await SplatApiAuth.GetGTokenAsync(
						SplatApiShared.F_GEN_URL,
						SplatApiShared.Credentials.SessionToken,
						SplatApiShared.A_VERSION
					).SafeAsync();
				string newBulletToken = 
					await SplatApiAuth.GetBulletAsync(
						newGToken,
						SplatApiShared.USER_AGENT,
						accountLang,
						accountCountry
					).SafeAsync();

				SplatApiShared.Credentials.GToken = newGToken;
				SplatApiShared.Credentials.BulletToken = newBulletToken;
				SplatApiShared.Credentials.Country = accountCountry;
				SplatApiShared.Credentials.Language = accountLang;

				SplatApiShared.Credentials.Save();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return false;
			}

			return true;
		}

		public static async Task<Match> FetchJsonAsync()
		{
			string sha = SplatApiShared.TranslateRid["LatestBattleHistoriesQuery"];
			List<string> battleIds = new();

			HttpRequestMessage request1 = new()
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(SplatApiShared.GRAPHQL_URL),
				Content = SplatApiShared.GenGraphQLBody(sha)
			};
			await SplatApiShared.HeadbuttAsync(request1).SafeAsync();

			HttpResponseMessage response1 = await SplatApiShared.HttpClient.SendAsync(request1).SafeAsync();
			JToken response1Json = JToken.Parse(await response1.Content.ReadAsStringAsync().SafeAsync());

			if (response1Json["data"]?.Value<JToken>() == null)
			{
				throw new Exception("Something's wrong with one of the query hashes.");
			}

			foreach (var battleGroup in response1Json["data"]["latestBattleHistories"]["historyGroups"]["nodes"])
			{
				foreach (var battle in battleGroup["historyDetails"]["nodes"])
				{
					if (SplatApiShared.OldBattles.Any(x => x.Id == battle["id"].Value<string>()))
					{
						continue;
					}

					battleIds.Add(battle["id"].Value<string>());
				}
			}

			SplatApiShared.OldBattles.AddRange(await FetchDetailedResultsAsync(battleIds).SafeAsync());

			return SplatApiShared.OldBattles.OrderByDescending(x => x.Time).First();
		}

		private static async Task<List<Match>> FetchDetailedResultsAsync(IEnumerable<string> battleIds)
		{
			List<Match> results = new();

			foreach(var battleId in battleIds) 
			{
				string sha = "VsHistoryDetailQuery";
				string varName = "vsResultId";

				HttpRequestMessage request = new()
				{
					RequestUri = new Uri(SplatApiShared.GRAPHQL_URL),
					Method = HttpMethod.Post,
					Content = SplatApiShared.GenGraphQLBody(SplatApiShared.TranslateRid[sha], varName, battleId)
				};

				await SplatApiShared.HeadbuttAsync(request).SafeAsync();
				HttpResponseMessage response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
				if (response.IsSuccessStatusCode)
				{
					string content = await response.Content.ReadAsStringAsync().SafeAsync();
					string cacheDir = Directory.CreateDirectory(Path.Combine(IOUtil.AppDirectory, "cache")).FullName;
					File.WriteAllText(Path.Combine(cacheDir, $"{battleId}.json"), content);
					results.Add(Match.Load(content));
				}
			}

			return results;
		}
	}
}
