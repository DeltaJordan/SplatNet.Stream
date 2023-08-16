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
		public const string NSO_VERSION_FALLBACK = "2.6.0";
		public const string WEB_VIEW_VERSION_FALLBACK = "4.0.0-22ddb0fd"; // fallback for current splatnet 3 ver
		public const string S3S_NAMESPACE = "b3a2dbf5-2c09-4792-b78c-00b548b70aeb";
		public const string SALMON_NAMESPACE = "f1911910-605e-11ed-a622-7085c2057a9d";
		public const string F_GEN_URL = "https://api.imink.app/f";
		public const string A_VERSION = "0.5.2";
		public const string USER_AGENT = "Mozilla/5.0 (Linux; Android 11; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.61 Mobile Safari/537.36";

		public static readonly IReadOnlyDictionary<string, string> TranslateRid = new Dictionary<string, string>
		{
			{ "HomeQuery", "7dcc64ea27a08e70919893a0d3f70871" },						 // blank vars
			{ "LatestBattleHistoriesQuery", "0d90c7576f1916469b2ae69f64292c02" },		 // INK / blank vars - query1
			{ "RegularBattleHistoriesQuery", "3baef04b095ad8975ea679d722bc17de" },		 // INK / blank vars - query1
			{ "BankaraBattleHistoriesQuery", "0438ea6978ae8bd77c5d1250f4f84803" },		 // INK / blank vars - query1
			{ "PrivateBattleHistoriesQuery", "8e5ae78b194264a6c230e262d069bd28" },		 // INK / blank vars - query1
			{ "XBattleHistoriesQuery", "6796e3cd5dc3ebd51864dc709d899fc5" },			 // INK / blank vars - query1
			{ "EventBattleHistoriesQuery", "e7bbaf1fa255305d607351da434b2d0f" },		 // INK / blank vars - query1
			{ "VsHistoryDetailQuery", "9ee0099fbe3d8db2a838a75cf42856dd" },				 // INK / req "vsResultId" - query2
			{ "CoopHistoryQuery", "01fb9793ad92f91892ea713410173260" },					 // SR  / blank vars - query1
			{ "CoopHistoryDetailQuery", "379f0d9b78b531be53044bcac031b34b" },			 // SR  / req "coopHistoryDetailId" - query2
			{ "MyOutfitCommonDataEquipmentsQuery", "d29cd0c2b5e6bac90dd5b817914832f8" }  // for Lean's seed checker
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
