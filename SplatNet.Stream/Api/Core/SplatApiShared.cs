using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SplatNet.Stream.Security;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using SplatNet.Stream.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SplatNet.Stream.Api.Models;

namespace SplatNet.Stream.Api.Core
{
	public static class SplatApiShared
	{
		public const string SPLATNET3_URL = "https://api.lp1.av5ja.srv.nintendo.net";
		public const string GRAPHQL_URL = $"{SPLATNET3_URL}/api/graphql";
		public const string NSO_VERSION_FALLBACK = "2.9.0";
		public const string WEB_VIEW_VERSION_FALLBACK = "6.0.0-eb33aadc"; // fallback for current splatnet 3 ver
		public const string S3S_NAMESPACE = "b3a2dbf5-2c09-4792-b78c-00b548b70aeb";
		public const string SALMON_NAMESPACE = "f1911910-605e-11ed-a622-7085c2057a9d";
		public const string F_GEN_URL = "https://api.imink.app/f";
		public const string A_VERSION = "0.6.3";
		public const string USER_AGENT = "Mozilla/5.0 (Linux; Android 14; Pixel 7a) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.6099.230 Mobile Safari/537.36";

		public static readonly IReadOnlyDictionary<string, string> TranslateRid = new Dictionary<string, string>
		{
			{ "HomeQuery", "51fc56bbf006caf37728914aa8bc0e2c86a80cf195b4d4027d6822a3623098a8" },						 // blank vars
			{ "LatestBattleHistoriesQuery", "b24d22fd6cb251c515c2b90044039698aa27bc1fab15801d83014d919cd45780" },		 // INK / blank vars - query1
			{ "RegularBattleHistoriesQuery", "2fe6ea7a2de1d6a888b7bd3dbeb6acc8e3246f055ca39b80c4531bbcd0727bba" },		 // INK / blank vars - query1
			{ "BankaraBattleHistoriesQuery", "9863ea4744730743268e2940396e21b891104ed40e2286789f05100b45a0b0fd" },		 // INK / blank vars - query1
			{ "PrivateBattleHistoriesQuery", "fef94f39b9eeac6b2fac4de43bc0442c16a9f2df95f4d367dd8a79d7c5ed5ce7" },		 // INK / blank vars - query1
			{ "XBattleHistoriesQuery", "eb5996a12705c2e94813a62e05c0dc419aad2811b8d49d53e5732290105559cb" },			 // INK / blank vars - query1
			{ "EventBattleHistoriesQuery", "e47f9aac5599f75c842335ef0ab8f4c640e8bf2afe588a3b1d4b480ee79198ac" },		 // INK / blank vars - query1
			{ "VsHistoryDetailQuery", "f893e1ddcfb8a4fd645fd75ced173f18b2750e5cfba41d2669b9814f6ceaec46" },				 // INK / req "vsResultId" - query2
			{ "CoopHistoryQuery", "0f8c33970a425683bb1bdecca50a0ca4fb3c3641c0b2a1237aedfde9c0cb2b8f" },					 // SR  / blank vars - query1
			{ "CoopHistoryDetailQuery", "824a1e22c4ad4eece7ad94a9a0343ecd76784be4f77d8f6f563c165afc8cf602" },			 // SR  / req "coopHistoryDetailId" - query2
			{ "MyOutfitCommonDataEquipmentsQuery", "45a4c343d973864f7bb9e9efac404182be1d48cf2181619505e9b7cd3b56a6e8" }  // for Lean's seed checker
		};

		public static readonly List<Match> OldBattles = new();

		public static readonly HttpClient HttpClient;

		public static ApiCredentials Credentials { get; set; }

		static SplatApiShared()
		{
			HttpClientHandler handler = new()
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
			};
			HttpClient = new HttpClient(handler);
		}

		public static async Task HeadbuttAsync(HttpRequestMessage requestMessage)
		{
			requestMessage.Headers.Add("Authorization", $"Bearer {Credentials.BulletToken}"); // update every time it's called with current global var
			requestMessage.Headers.Add("Accept-Language", Credentials.Language);
			requestMessage.Headers.Add("User-Agent", USER_AGENT);
			requestMessage.Headers.Add("X-Web-View-Ver", await SplatApiAuth.GetWebViewVersionAsync().SafeAsync());
			requestMessage.Headers.TryAddWithoutValidation("Content-Type", "application/json");
			requestMessage.Headers.Add("Accept", "*/*");
			requestMessage.Headers.Add("Origin", SPLATNET3_URL);
			requestMessage.Headers.Add("X-Requested-With", "com.nintendo.znca");
			requestMessage.Headers.Add("Referer", $"{SPLATNET3_URL}?lang={Credentials.Language}&na_country={Credentials.Country}&na_lang={Credentials.Language}");
			requestMessage.Headers.Add("Accept-Encoding", "gzip, deflate");
			requestMessage.Headers.Add("Cookie", $"_gtoken={Credentials.GToken}");
		}

		public static HttpContent GenGraphQLBody(string sha256Hash, string varName = null, string varValue = null)
		{
			JObject greatPassage = new(
				new JProperty("extensions",
					new JObject(
						new JProperty("persistedQuery",
							new JObject(
								new JProperty("sha256Hash", sha256Hash),
								new JProperty("version", 1)
							)
						)
					)
				),
				new JProperty("variables", new JObject())
			);
			
			if ( varName != null && varValue != null ) 
			{
				greatPassage["variables"][varName] = varValue;
			}

			return new StringContent(greatPassage.ToString(Formatting.None), Encoding.UTF8, "application/json");
		}
	}
}
