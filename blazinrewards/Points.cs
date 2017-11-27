using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace blazinrewards
{
	
	public class PointsBalance
	{
		public double PointAmount { get; set; }
		public string PointTypeShortDescription { get; set; }
		public string PointTypeId { get; set; }
	}

	public class PoolingPointsBalance
	{
		public double PointAmount { get; set; }
		public string PointTypeShortDescription { get; set; }
		public string PointTypeId { get; set; }
	}

	public class Points
	{
		public string ProfileId { get; set; }
		public bool PendingPointsEnabled { get; set; }
		public string DefaultPointTypeId { get; set; }
		public List<PointsBalance> PointsBalance { get; set; }
		public List<PoolingPointsBalance> PoolingPointsBalance { get; set; }

		public static Points GetPoints(Account account)
		{
			var url = $"https://bfww-pubapi.epsilon.agilityloyalty.com/api/v1/profiles/{account.ProfileId}/points/bystatus?status=A";

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

				var response = client.GetAsync(url).Result;

				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new Exception("Invalid HTTP response");
				}

				return JsonConvert.DeserializeObject<Points>(response.Content.ReadAsStringAsync().Result);
			}

		}
	}

}
