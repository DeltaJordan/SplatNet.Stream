using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SplatNet.Stream.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SplatNet.Stream.Api.Core
{
	public static class SplatApiAuth
	{
		private static bool useOldNsoVersion = false;

		private static string s3sVersion = null;
		private static string nsoVersion = null;
		private static string webViewVersion = null;

		/// <summary>
		/// Fetches the current Nintendo Switch Online app version from the Apple App Store and sets it globally.
		/// </summary>
		/// <returns></returns>
		public static async Task<string> GetNsoVersionAsync()
		{
			if (useOldNsoVersion)
			{
				return SplatApiShared.NSO_VERSION_FALLBACK;
			}

			if (!string.IsNullOrWhiteSpace(nsoVersion))
			{
				return nsoVersion;
			}

			try
			{
				HtmlWeb web = new();
				HtmlDocument page = await web.LoadFromWebAsync("https://apps.apple.com/us/app/nintendo-switch-online/id1234806557").SafeAsync();
				HtmlNodeCollection searchVersion = page.DocumentNode.SelectNodes("//p[contains(@class, 'whats-new__latest__version')]");
				string version = searchVersion.First().InnerText.Replace("Version ", "").Trim();

				nsoVersion = version;

				return nsoVersion;
			}
			catch
			{
				return SplatApiShared.NSO_VERSION_FALLBACK;
			}
		}

		public static async Task<string> GetWebViewVersionAsync(HttpRequestHeaders bhead = null, string gtoken = "")
		{
			if (!string.IsNullOrWhiteSpace(webViewVersion))
			{
				return webViewVersion;
			}

			HttpRequestMessage request = new()
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri(SplatApiShared.SPLATNET3_URL),
				Headers =
				{
					{ "Upgrade-Insecure-Requests", "1" },
					{ "Accept", "*/*" },
					{ "DNT", "1" },
					{ "X-AppColorScheme", "DARK" },
					{ "X-Requested-With", "com.nintendo.znca" },
					{ "Sec-Fetch-Site", "none" },
					{ "Sec-Fetch-Mode", "navigate" },
					{ "Sec-Fetch-User", "?1" },
					{ "Sec-Fetch-Dest", "document" }
				},

			};

			if (bhead != null)
			{
				foreach (KeyValuePair<string, IEnumerable<string>> header in bhead)
				{
					request.Headers.Add(header.Key, header.Value);
				}
			}

			string cookie = "_dnt=1";

			if (!string.IsNullOrWhiteSpace(gtoken))
			{
				cookie += $"; _gtoken={gtoken}";
			}

			request.Headers.Add("Cookie", cookie);

			HttpResponseMessage response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
			if (!response.IsSuccessStatusCode)
			{
				return SplatApiShared.WEB_VIEW_VERSION_FALLBACK;
			}

			HtmlDocument document = new();
			document.LoadHtml(await response.Content.ReadAsStringAsync().SafeAsync());
			HtmlNode searchJs = document.DocumentNode.SelectSingleNode("//script[contains(@src, 'static')]");
			string mainJsPath = searchJs.Attributes["src"].Value;

			string mainJsFullUrl = SplatApiShared.SPLATNET3_URL + mainJsPath;

			request = new HttpRequestMessage
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri(mainJsFullUrl),
				Headers =
				{
					{ "Accept", "*/*" },
					{ "X-Requested-With", "com.nintendo.znca" },
					{ "Sec-Fetch-Site", "same-origin" },
					{ "Sec-Fetch-Mode", "no-cors" },
					{ "Sec-Fetch-Dest", "script" },
					{ "Referer", SplatApiShared.SPLATNET3_URL }, // sending w/o lang, na_country, na_lang params
				}
			};

			if (bhead != null)
			{
				foreach (KeyValuePair<string, IEnumerable<string>> header in bhead)
				{
					request.Headers.Add(header.Key, header.Value);
				}
			}

			cookie = "_dnt=1";

			if (!string.IsNullOrWhiteSpace(gtoken))
			{
				cookie += $"; _gtoken={gtoken}";
			}

			request.Headers.Add("Cookie", cookie);

			response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
			if (!response.IsSuccessStatusCode)
			{
				return SplatApiShared.WEB_VIEW_VERSION_FALLBACK;
			}

			Regex pattern = new(@"\b(?<revision>[0-9a-f]{40})\b[\S]*?void 0[\S]*?""revision_info_not_set""\}`,.*?=`(?<version>\d+\.\d+\.\d+)-");
			Match match = pattern.Match(await response.Content.ReadAsStringAsync().SafeAsync());
			if (!match.Success)
			{
				return SplatApiShared.WEB_VIEW_VERSION_FALLBACK;
			}

			string version = match.Groups["version"].Value;
			string revision = match.Groups["revision"].Value;
			string resultVersion = $"{version}-{revision[..8]}";

			webViewVersion = resultVersion;
			return resultVersion;
		}

		/// <summary>
		/// Logs in to a Nintendo Account and returns a session_token.
		/// </summary>
		/// <param name="version"></param>
		/// <returns></returns>
		public static string GetLogInUrl(string version, string authCodeVerifier)
		{
			s3sVersion = version;

			byte[] randomState = new byte[36];
			Random.Shared.NextBytes(randomState);
			string authState = Convert.ToBase64String(randomState).TrimEnd('=').Replace('+', '-').Replace('/', '_');

			byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(authCodeVerifier));
			string authCodeChallenge = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');

			return $"https://accounts.nintendo.com/connect/1.0.0/authorize?" +
				$"state={authState}&" +
				$"redirect_uri=npf71b963c1b7b6d119://auth&" +
				$"client_id=71b963c1b7b6d119&" +
				$"scope=openid user user.birthday user.mii user.screenName&" +
				$"response_type=session_token_code&" +
				$"session_token_code_challenge={authCodeChallenge}&" +
				$"session_token_code_challenge_method=S256&" +
				$"theme=login_form";
		}

		public static async Task<string> LogInAsync(string accountUrl, string authCodeVerifier)
		{
			Regex regex = new(@"de=(.*)&st");
			string sessionTokenCode = regex.Match(accountUrl).Groups[1].Value;
			return await GetSessionTokenAsync(sessionTokenCode, authCodeVerifier).SafeAsync();
		}

		/// <summary>
		/// Retrieves token from session token code.
		/// </summary>
		/// <param name="sessionTokenCode"></param>
		/// <param name="authCodeVerifier"></param>
		/// <returns></returns>
		private static async Task<string> GetSessionTokenAsync(string sessionTokenCode, string authCodeVerifier)
		{
			nsoVersion = await GetNsoVersionAsync().SafeAsync();

			HttpRequestMessage request = new()
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri("https://accounts.nintendo.com/connect/1.0.0/api/session_token"),
				Headers =
				{
					{ "User-Agent", $"OnlineLounge/{nsoVersion} NASDKAPI Android" },
					{ "Accept-Language", "en-US" },
					{ "Accept", "application/json" },
					{ "Host", "accounts.nintendo.com" },
					{ "Connection", "Keep-Alive" },
					{ "Accept-Encoding", "gzip" }
				},
				Content = new FormUrlEncodedContent(new Dictionary<string, string> {
					{ "client_id", "71b963c1b7b6d119" },
					{ "session_token_code", sessionTokenCode },
					{ "session_token_code_verifier", authCodeVerifier }
				})
			};

			HttpResponseMessage response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
			return JToken.Parse(await response.Content.ReadAsStringAsync().SafeAsync())["session_token"].Value<string>();
		}

		/// <summary>
		/// Provided the sessionToken, returns a GameWebToken JWT and account info.
		/// </summary>
		/// <param name="fGenUrl"></param>
		/// <param name="sessionToken"></param>
		/// <param name="version"></param>
		/// <returns></returns>
		public static async Task<(string gToken, string nickname, string language, string country)> GetGTokenAsync(string fGenUrl, string sessionToken, string version)
		{
			nsoVersion = await GetNsoVersionAsync().SafeAsync();

			s3sVersion = version;

			HttpRequestMessage request = new()
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri("https://accounts.nintendo.com/connect/1.0.0/api/token"),
				Headers =
				{
					{ "Host", "accounts.nintendo.com" },
					{ "Accept-Encoding", "gzip" },
					{ "Accept", "application/json" },
					{ "Connection", "Keep-Alive" },
					{ "User-Agent", "Dalvik/2.1.0 (Linux; U; Android 14; Pixel 7a Build/UQ1A.240105.004)" }
				},
				Content = new StringContent(JsonConvert.SerializeObject(new
				{
					client_id = "71b963c1b7b6d119",
					session_token = sessionToken,
					grant_type = "urn:ietf:params:oauth:grant-type:jwt-bearer-session-token"
				}), Encoding.UTF8, "application/json")
			};

			HttpResponseMessage response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
			JToken idResponse = JToken.Parse(await response.Content.ReadAsStringAsync().SafeAsync());

			request = new HttpRequestMessage
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri("https://api.accounts.nintendo.com/2.0.0/users/me"),
				Headers =
				{
					{ "Accept", "application/json" },
					{ "Authorization", $"Bearer {idResponse["access_token"].Value<string>()}" },
					{ "Host", "api.accounts.nintendo.com" },
					{ "Connection", "Keep-Alive" },
					{ "Accept-Encoding", "gzip" }
				}
			};

			request.Headers.TryAddWithoutValidation("User-Agent", "NASDKAPI; Android");
			request.Headers.TryAddWithoutValidation("Content-Type", "application/json");

			response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
			string responseJson = await response.Content.ReadAsStringAsync().SafeAsync();
			JToken userInfo = JToken.Parse(responseJson);

			string userNickname = userInfo["nickname"].Value<string>();
			string userLang = userInfo["language"].Value<string>();
			string userCountry = userInfo["country"].Value<string>();
			string userId = userInfo["id"].Value<string>();

			string idToken = idResponse["id_token"].Value<string>();
			var (f, uuid, timestamp) = await CallFApi(idToken, 1, fGenUrl, userId).SafeAsync();
			object body = new
			{
				parameter = new 
				{
					f,
					language = userLang,
					naBirthday = userInfo["birthday"].Value<string>(),
					naCountry = userCountry,
					naIdToken = idToken,
					requestId = uuid,
					timestamp
				}
			};

			request = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri("https://api-lp1.znc.srv.nintendo.net/v3/Account/Login"),
				Headers =
				{
					{ "X-Platform", "Android" },
					{ "X-ProductVersion", nsoVersion },
					{ "Connection", "Keep-Alive" },
					{ "Accept-Encoding", "gzip" },
				},
				Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
			};
			request.Headers.TryAddWithoutValidation("User-Agent", $"com.nintendo.znca/{nsoVersion}(Android/14)");


			response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
			JToken splatoonJToken = JToken.Parse(await response.Content.ReadAsStringAsync().SafeAsync());
			string accessToken;
			string coralUserId;

			try
			{
				accessToken = splatoonJToken["result"]["webApiServerCredential"]["accessToken"].Value<string>();
				coralUserId = splatoonJToken["result"]["user"]["id"].Value<string>();
			}
			catch
			{
				(f, uuid, timestamp) = await CallFApi(idToken, 1, fGenUrl, userId).SafeAsync();
				body = new
				{
					parameter = new
					{
						f,
						language = userLang,
						naBirthday = userInfo["birthday"].Value<string>(),
						naCountry = userCountry,
						naIdToken = idToken,
						requestId = uuid,
						timestamp
					}
				};

				request = new HttpRequestMessage
				{
					Method = HttpMethod.Post,
					RequestUri = new Uri("https://api-lp1.znc.srv.nintendo.net/v3/Account/Login"),
					Headers =
				{
					{ "X-Platform", "Android" },
					{ "X-ProductVersion", nsoVersion },
					{ "Connection", "Keep-Alive" },
					{ "Accept-Encoding", "gzip" },
				},
					Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
				};
				request.Headers.TryAddWithoutValidation("User-Agent", $"com.nintendo.znca/{nsoVersion}(Android/14)");

				response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
				splatoonJToken = JToken.Parse(await response.Content.ReadAsStringAsync().SafeAsync());
				accessToken = splatoonJToken["result"]["webApiServerCredential"]["accessToken"].Value<string>();
				coralUserId = splatoonJToken["result"]["user"]["id"].Value<string>();
			}

			(f, uuid, timestamp) = await CallFApi(accessToken, 2, fGenUrl, userId, coralUserId).SafeAsync();

			body = new
			{
				parameter = new
				{
					f,
					id = 4834290508791808,
					registrationToken = accessToken,
					requestId = uuid,
					timestamp
				}
			};

			request = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri("https://api-lp1.znc.srv.nintendo.net/v2/Game/GetWebServiceToken"),
				Headers =
				{
					{ "X-Platform", "Android" },
					{ "X-ProductVersion", nsoVersion },
					{ "Authorization", $"Bearer {accessToken}" },
					{ "Accept-Encoding", "gzip" }
				},
				Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
			};
			request.Headers.TryAddWithoutValidation("User-Agent", $"com.nintendo.znca/{nsoVersion}(Android/14)");

			response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
			JToken webServiceJson = JToken.Parse(await response.Content.ReadAsStringAsync().SafeAsync());
			string webServiceToken;

			try
			{
				webServiceToken = webServiceJson["result"]["accessToken"].Value<string>();
			}
			catch
			{
				(f, uuid, timestamp) = await CallFApi(accessToken, 2, fGenUrl, userId, coralUserId).SafeAsync();

				body = new
				{
					parameter = new
					{
						f,
						id = 4834290508791808,
						registrationToken = accessToken,
						requestId = uuid,
						timestamp
					}
				};

				request = new HttpRequestMessage
				{
					Method = HttpMethod.Post,
					RequestUri = new Uri("https://api-lp1.znc.srv.nintendo.net/v2/Game/GetWebServiceToken"),
					Headers =
					{
						{ "X-Platform", "Android" },
						{ "X-ProductVersion", nsoVersion },
						{ "Authorization", $"Bearer {accessToken}" },
						{ "Content-Type", "application/json; charset=utf-8" },
						{ "Content-Length", "391" },
						{ "Accept-Encoding", "gzip" }
					},
					Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
				};
				request.Headers.TryAddWithoutValidation("User-Agent", $"com.nintendo.znca/{nsoVersion}(Android/14)");

				response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
				webServiceJson = JToken.Parse(await response.Content.ReadAsStringAsync().SafeAsync());
				webServiceToken = webServiceJson["result"]["accessToken"].Value<string>();
			}

			return (webServiceToken, userNickname, userLang, userCountry);
		}

		public static async Task<string> GetBulletAsync(string webServiceToken, string appUserAgent, string userLang, string userCountry)
		{
			HttpRequestMessage request = new()
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri($"{SplatApiShared.SPLATNET3_URL}/api/bullet_tokens"),
				Headers =
				{
					{ "Accept-Language", userLang },
					{ "User-Agent", appUserAgent },
					{ "X-Web-View-Ver", await GetWebViewVersionAsync().SafeAsync() },
					{ "X-NACOUNTRY", userCountry },
					{ "Accept", "*/*" },
					{ "Origin", SplatApiShared.SPLATNET3_URL },
					{ "X-Requested-With", "com.nintendo.znca" },
					{ "Cookie", $"_gtoken={webServiceToken}; _dnt=1" }
				},
				Content = new StringContent("", Encoding.UTF8, "application/json")
			};

			HttpResponseMessage response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
			JToken bulletJson = JToken.Parse(await response.Content.ReadAsStringAsync().SafeAsync());
			string bulletToken = bulletJson["bulletToken"].Value<string>();

			return bulletToken;
		}

		private static async Task<(string f, string uuid, string timestamp)> CallFApi(
			string accessToken,
			int step,
			string fGenUrl,
			string userId,
			string coralUserId = null
		) {
			HttpRequestMessage request = new()
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(fGenUrl),
				Content = new StringContent(JsonConvert.SerializeObject(new
				{
					token = accessToken,
					hash_method = step,
					na_id = userId,
					coral_user_id = step == 2 ? coralUserId : null
				}, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }), Encoding.UTF8, "application/json")
			};

			request.Headers.TryAddWithoutValidation("User-Agent", $"s3s/{s3sVersion}");

			HttpResponseMessage response = await SplatApiShared.HttpClient.SendAsync(request).SafeAsync();
			JToken responseToken = JToken.Parse(await response.Content.ReadAsStringAsync().SafeAsync());

			return (responseToken["f"].Value<string>(), responseToken["request_id"].Value<string>(), responseToken["timestamp"].Value<string>());
		}
	}
}
