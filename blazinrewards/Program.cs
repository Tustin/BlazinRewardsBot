using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net.Http.Headers;

namespace blazinrewards
{
	class Program
	{
		const string REGISTER_URL = "https://blazinrewards.com/Enrollment/Enrollment/ProfileRegistration";
		const string SURVEY_POST_URL = "https://blazinrewards.com/Profile/ProfileSurveys/SaveProfileSurvey";
		const string SURVEY_URL = "https://blazinrewards.com/Profile/ProfileSurveys/Survey";
		const string REDEEM_URL = "https://blazinrewards.com/Reward/Reward/RedeemReward";

		static readonly Uri baseAddress = new Uri("https://blazinrewards.com");

		enum MenuSelection
		{
			Generate,
			Fetch,
			Exit
		};

		static void ClearRestore()
		{
			Console.Clear();
			Console.WriteLine(@"///////////////////////////////////////////////////////////");
			Console.WriteLine($"//         WWII Double XP code generator by Tustin       //");
			Console.WriteLine($"//                  Twitter: @Tusticles                  //");
			Console.WriteLine("//          This program is FREE and open source!        //");
			Console.WriteLine("//              If you paid, request a refund!           //");
			Console.WriteLine("//      https://github.com/Tustin/BlazinRewardsBot       //");
			Console.WriteLine(@"///////////////////////////////////////////////////////////");
			Console.WriteLine();
		}

		static MenuSelection Menu()
		{
			Console.WriteLine("1) Generate double xp codes");
			Console.WriteLine("2) Fetch double xp codes from emails.txt");
			Console.WriteLine("3) Quit");
			while (true)
			{
				Console.Write("Choose an option: ");
				var selection = Console.ReadKey().Key;
				switch (selection)
				{
					case ConsoleKey.D1:
					case ConsoleKey.NumPad1:
					return MenuSelection.Generate;
					case ConsoleKey.D2:
					case ConsoleKey.NumPad2:
					return MenuSelection.Fetch;
					case ConsoleKey.D3:
					case ConsoleKey.NumPad3:
					return MenuSelection.Exit;
				}
			}
		}

		static List<string> Generate()
		{
			Console.Write("\r\nHow many codes do you want to generate? ");

			if (!int.TryParse(Console.ReadLine(), out int amount) || amount <= 0)
			{
				Console.WriteLine("Invalid amount");
				return null;
			}

			var emails = new List<string>();
			var message = $"Generating {amount} code";
			message += amount > 1 ? "s..." : "...";
			Console.WriteLine(message);

			using (var handle = File.Open("emails.txt", FileMode.OpenOrCreate | FileMode.Append))
			using (var writer = new StreamWriter(handle))
			{
				for (int i = 1; i <= amount; i++)
				{
					Console.WriteLine();
					var account = RegisterAccount();

					if (account is null)
					{
						Console.WriteLine("Failed making account");
						continue;
					}

					Console.WriteLine($"Created {account.Username}");

					if (DoSurvey(account))
					{
						Console.WriteLine("\tCompleted survey");
					}
					else
					{
						Console.WriteLine("\tSurvey failed");
						continue;
					}

					var accountPoints = Points.GetPoints(account).PointsBalance[0]?.PointAmount;

					if (accountPoints is null)
					{
						Console.WriteLine("\tFailed fetching account points");
						continue;
					}
					else if (accountPoints < 50)
					{
						Console.WriteLine("\tAccount doesn't have at least 50 points for the code");
					}

					Console.WriteLine($"\tSpending {accountPoints} points");

					for (int j = 0; j < accountPoints / 50; j++)
					{
						if (GimmeCode(account))
						{
							Console.WriteLine("\tRedeemed 2xp code");
						}
						else
						{
							Console.WriteLine("\tFailed redeeming 2xp code");
						}
					}

					emails.Add(account.Username);
					writer.WriteLine(account.Username);
				}

				writer.Close();
				handle.Close();
			}

			return emails;
		}

		private static void Fetch()
		{
			using (var codesHandle = File.Open("codes.txt", FileMode.OpenOrCreate | FileMode.Append))
			using (var emailHandle = File.Open("emails.txt", FileMode.Open))
			using (var emailReader = new StreamReader(emailHandle))
			using (var codesWriter = new StreamWriter(codesHandle))
			{
				string account;
				while ((account = emailReader.ReadLine()) != null)
				{
					var e = account.Trim();
					Console.WriteLine($"\r\nTrying {e}");

					var mail = new GuerrillaMail(e);
					var mailList = mail.GetMail();

					var blazinRewardsEmails = mailList.list.Where(a => a.mail_from.Equals("blazinrewards@emails.buffalowildwings.com"))
						.Where(b => b.mail_subject.Contains("2XP"));

					if (blazinRewardsEmails is null)
					{
						Console.WriteLine("\tFailed fetching codes");
						continue;
					}

					foreach (var email in blazinRewardsEmails)
					{
						var emailContents = mail.FetchEmail(email.mail_id).mail_body;
						var code = Regex.Match(emailContents, @"[A-Z0-9]{4}-[A-Z0-9]{5}-[A-Z0-9]{4}");
						if (code is null) continue;

						codesWriter.WriteLine(code.Value);
						Console.WriteLine(code.Value);
					}

				}
				codesWriter.Close();
				emailReader.Close();
				codesHandle.Close();
				emailHandle.Close();
			}
		}

		static bool CodLogin(HttpClient client, string username, string password)
		{
			var form = new Dictionary<string, string>
				{
					{ "username", username },
					{ "remember_me", "true" },
					{ "password", password },
				};

			var content = new FormUrlEncodedContent(form);
			var response = client.PostAsync("https://profile.callofduty.com/do_login?new_SiteId=cod", content).Result;

			if (response.RequestMessage.RequestUri.ToString().Contains("failure"))
			{
				Console.WriteLine("Invalid Activision login");
				return false;
			}

			return true;
		}

		static bool TryRedeeming(HttpClient client, string code)
		{
			var form = new Dictionary<string, string>()
			{
				{"code",  code}
			};

			var content = new FormUrlEncodedContent(form);
			var response = client.PostAsync("https://profile.callofduty.com/promotions/redeem/cod/bww", content).Result;
			var html = response.Content.ReadAsStringAsync().Result;
			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var success = doc.DocumentNode.SelectSingleNode("//section[@class='success-container']");
			return success != null;
		}

		/*
		static List<string> GetCodesFromEmail(string emailAddress)
		{
			var codes = new List<string>();
			using (HttpClient client = new HttpClient())
			{
				if (emailAddress.Contains("@")) emailAddress = emailAddress.Split('@')[0];

				var mailResponse = client.GetAsync($"http://www.yopmail.com/en/inbox.php?login={emailAddress}&p=r&d=&ctrl=&scrl=&spam=true&yf=115&v=2.7&r_c=&id=").Result;

				var doc = new HtmlDocument();
				doc.LoadHtml(mailResponse.Content.ReadAsStringAsync().Result);
				File.WriteAllText("aaa.html", mailResponse.Content.ReadAsStringAsync().Result);

				var emails = doc.DocumentNode.SelectNodes("//a[@class='lm']");
				if (emails is null) return null;

				var codEmails = emails.Where(a => a.ChildNodes[1].InnerText.Contains("2XP"));
				if (codEmails is null) return null;

				//Don't even ask
				var iHateHtmlAgilityPack = new List<string>();
				codEmails.ToList().ForEach(a => iHateHtmlAgilityPack.Add(a.Attributes["href"].Value));

				foreach (var url in iHateHtmlAgilityPack)
				{
					var @base = "http://www.yopmail.com/en/";
					var mail = client.GetAsync($"{@base}{url}").Result;
					if (mail.StatusCode != HttpStatusCode.OK) continue;

					doc.LoadHtml(mail.Content.ReadAsStringAsync().Result);
					var code = doc.DocumentNode.SelectNodes("//td").ToList().LastOrDefault(a => Regex.IsMatch(a.InnerText, @"[A-Z0-9]{4}-[A-Z0-9]{5}-[A-Z0-9]{4}"));
					if (code is null) continue;

					codes.Add(code.InnerText.Trim());
				}

			}
			return codes;
		}
		*/

		/*
		static List<string> GetCodesFromEmail(string email)
		{
			var baseAddress = new Uri("https://temp-mail.org/en/");
			var cookieContainer = new CookieContainer();
			using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
			using (HttpClient client = new HttpClient(handler) { BaseAddress = baseAddress })
			{
				cookieContainer.Add(baseAddress, new Cookie("mail", email));
				var response = client.GetAsync("https://temp-mail.org/en/").Result;
				var html = response.Content.ReadAsStringAsync().Result;
				var doc = new HtmlDocument();
				doc.LoadHtml(html);

				if (response.StatusCode != HttpStatusCode.OK)
				{
					return null;
				}

				var subjects = doc.DocumentNode.SelectNodes("//a[@class='title-subject']");
				if (subjects == null)
				{
					return null;
				}

				var codEmails = subjects.Where(a => a.InnerText.Contains("Call of"));

				if (codEmails == null)
				{
					return null;
				}

				var codes = new List<string>();
				foreach (var codEmail in codEmails)
				{
					response = client.GetAsync(codEmail.Attributes["href"].Value).Result;

					if (response.StatusCode != HttpStatusCode.OK)
					{
						return null;
					}

					doc.LoadHtml(response.Content.ReadAsStringAsync().Result);

					//The email is retarded so try some hack here
					var code = doc.DocumentNode.SelectNodes("//td").ToList().LastOrDefault(a => Regex.IsMatch(a.InnerText, @"[A-Z0-9]{4}-[A-Z0-9]{5}-[A-Z0-9]{4}"));

					codes.Add(code.InnerText.Trim());
				}

				return codes;
			}

		}
		*/

		static bool GimmeCode(Account account)
		{
			var url = $"https://bfww-pubapi.epsilon.agilityloyalty.com/api/v1/profiles/{account.ProfileId}/rewards/certificates";
			var form = new Dictionary<string, string>
			{
				{ "ProfileId", account.ProfileId },
				{ "RewardCode", "96" },
			};

			using (HttpClient client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("SocialTokenProvider", account.SocialTokenProvider);
				client.DefaultRequestHeaders.Add("Program-Code", "BDUBS");
				client.DefaultRequestHeaders.Add("SocialTokenProvider", "password");
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
				client.DefaultRequestHeaders.Add("SocialToken", account.SocialToken);
				client.DefaultRequestHeaders.Add("Source-Application", "IOS");
				client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
				client.DefaultRequestHeaders.Add("User-Agent", "Blazin Rewards/510 CFNetwork/887 Darwin/17.0.0");

				var data = JsonConvert.SerializeObject(form);
				var content = new StringContent(data, Encoding.UTF8, "application/json");
				var response = client.PostAsync(url, content).Result;
				if (response.StatusCode != HttpStatusCode.Created)
				{
					return false;
				}

				return true;
			}
		}

		static bool DoSurvey(Account account)
		{
			var url = $"https://bfww-pubapi.epsilon.agilityloyalty.com/api/v1/profiles/{account.ProfileId}/surveys/ab959cbe-907c-4358-b941-d1a63cfe4a32";
			var survey = new Survey()
			{
				ProfileId = account.ProfileId,
				SurveyId = "ab959cbe-907c-4358-b941-d1a63cfe4a32",
				SurveyResponses = new List<Survey.SurveyResponse>()
				{
					new Survey.SurveyResponse()
					{
						AnswerId = "a26123b5-dfcf-45f5-9923-8eb5b9e3e974",
						ResponseDate = "2017-10-30T19:18:16.6800000Z",
						QuestionId =  "c8d0429c-4b8d-4499-bc21-4391587d7335"
					},
					new Survey.SurveyResponse()
					{
						AnswerId = "f6796654-7008-41b5-b9ec-b31d816f5b26",
						ResponseDate = "2017-10-30T19:18:16.6800000Z",
						QuestionId =  "33964564-c258-42ab-9380-bb77fe801046"
					},
					new Survey.SurveyResponse()
					{
						AnswerId = "775803e6-a1c1-45fb-ba13-b1309262e24d",
						ResponseDate = "2017-10-30T19:18:16.6800000Z",
						QuestionId =  "bca99055-6181-4e29-91b0-094f50e537ed"
					},
					new Survey.SurveyResponse()
					{
						AnswerId = "3867412c-2089-4fdc-b98a-c34b7b51c0be",
						ResponseDate = "2017-10-30T19:18:16.6800000Z",
						QuestionId =  "333d0f09-240b-46db-946c-a2170a3dbfe6"
					},
					new Survey.SurveyResponse()
					{
						AnswerId = "348c12b6-d5f4-4c07-96a9-395ae3c07bc5",
						ResponseDate = "2017-10-30T19:18:16.6800000Z",
						QuestionId =  "2676aaae-63b7-4cc4-907a-094d49a09d24"
					},
				}
			};

			using (HttpClient client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("SocialTokenProvider", account.SocialTokenProvider);
				client.DefaultRequestHeaders.Add("Program-Code", "BDUBS");
				client.DefaultRequestHeaders.Add("SocialTokenProvider", "password");
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
				client.DefaultRequestHeaders.Add("SocialToken", account.SocialToken);
				client.DefaultRequestHeaders.Add("Source-Application", "IOS");
				client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
				client.DefaultRequestHeaders.Add("User-Agent", "Blazin Rewards/510 CFNetwork/887 Darwin/17.0.0");

				var data = JsonConvert.SerializeObject(survey);
				var content = new StringContent(data, Encoding.UTF8, "application/json");
				var response = client.PutAsync(url, content).Result;
				if (response.StatusCode != HttpStatusCode.OK)
				{
					return false;
				}

				return true;
			}
		}

		static Account RegisterAccount()
		{
			var email = GenerateEmail();

			using (HttpClient client = new HttpClient())
			{
				var request = new BWW()
				{
					GlobalOptOut = false,
					FirstName = "Bob",
					EnrollChannelCode = "IOS",
					LastName = "Smith",
					Username = email,
					Emails = new List<BWW.Email>()
				{
					new BWW.Email() { EmailAddress =  email }
				},
					BirthDate = "1980-10-14T22:00:00Z",
					Password = "9yIoYwh5GOZu8ki",
					Phones = new List<BWW.Phone>()
				{
					new BWW.Phone() { PhoneNumber = GeneratePhone()}
				},
					Addresses = new List<BWW.Address>()
				{
					new BWW.Address() { PostalCode = "90002" }
				},
					SourceCode = "WEB"
				};

				var data = JsonConvert.SerializeObject(request);
				var content = new StringContent(data, Encoding.UTF8, "application/json");

				client.DefaultRequestHeaders.Add("User-Agent", "Blazin Rewards/510 CFNetwork/887 Darwin/17.0.0");
				client.DefaultRequestHeaders.Add("Program-Code", "BDUBS");
				client.DefaultRequestHeaders.Add("Source-Application", "IOS");
				client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "TU9CQVBJX1BVQkxJQ19VU0VSOk0wOEBwMVB1OGwhYw==");

				var resp = client.PostAsync("https://bfww-pubapi.epsilon.agilityloyalty.com/api/v1/profiles", content).Result;
				dynamic respData = JsonConvert.DeserializeObject(resp.Content.ReadAsStringAsync().Result);

				if (resp.StatusCode != HttpStatusCode.Created)
				{
					Console.WriteLine($"Failed making account: {respData.Message}");
					return null;
				}
			}

			using (HttpClient client = new HttpClient())
			{
				var form = new Dictionary<string, string>
				{
					{ "grant_type", "password" },
					{ "username", email },
					{ "password", "9yIoYwh5GOZu8ki" },
					{ "response_type", "token" }
				};

				client.DefaultRequestHeaders.Add("User-Agent", "Blazin Rewards/510 CFNetwork/887 Darwin/17.0.0");
				client.DefaultRequestHeaders.Add("Program-Code", "BDUBS");
				client.DefaultRequestHeaders.Add("Source-Application", "IOS");
				client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "TU9CQVBJX1BVQkxJQ19VU0VSOk0wOEBwMVB1OGwhYw==");

				var content = new FormUrlEncodedContent(form);
				var resp = client.PostAsync("https://bfww-pubapi.epsilon.agilityloyalty.com/api/v1/authorization/profiles/tokens", content).Result;
				var data = JsonConvert.DeserializeObject<Account>(resp.Content.ReadAsStringAsync().Result);
				return data;
			}

		}

		static string GeneratePhone()
		{
			var rand = new Random();

			return $"214{rand.Next(1000000, 9999999)}";
		}

		static string GenerateEmail(string domain = "sharklasers.com")
		{
			return $"{Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12)}@{domain}";
		}

		static void Main(string[] args)
		{
			Console.Title = $"WWII Double XP Generator by Tustin - Twitter @Tusticles";

			while (true)
			{
				ClearRestore();
				var option = Menu();

				switch (option)
				{
					case MenuSelection.Generate:
					Generate();
					break;
					case MenuSelection.Fetch:
					Fetch();
					break;
					case MenuSelection.Exit:
					return;
				}
				Console.WriteLine("\r\nPress any key to return to menu");
				Console.ReadLine();
			}
		}

	}
}
