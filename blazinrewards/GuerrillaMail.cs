using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace blazinrewards
{
	public class List
	{
		public string mail_from { get; set; }
		public int mail_timestamp { get; set; }
		public int mail_read { get; set; }
		public string mail_date { get; set; }
		public string reply_to { get; set; }
		public string mail_subject { get; set; }
		public string mail_excerpt { get; set; }
		public int mail_id { get; set; }
		public int att { get; set; }
		public string content_type { get; set; }
		public string mail_recipient { get; set; }
		public int source_id { get; set; }
		public int source_mail_id { get; set; }
		public string mail_body { get; set; }
		public int size { get; set; }
	}

	public class Stats
	{
		public string sequence_mail { get; set; }
		public int created_addresses { get; set; }
		public string received_emails { get; set; }
		public string total { get; set; }
		public string total_per_hour { get; set; }
	}

	public class Auth
	{
		public bool success { get; set; }
		public List<object> error_codes { get; set; }
	}

	public class GuerillaMailList
	{
		public List<List> list { get; set; }
		public string count { get; set; }
		public string email { get; set; }
		public string alias { get; set; }
		public int ts { get; set; }
		public string sid_token { get; set; }
		public Stats stats { get; set; }
		public Auth auth { get; set; }
	}

	public class GuerillaEmail
	{
		public string mail_id { get; set; }
		public string mail_from { get; set; }
		public string mail_recipient { get; set; }
		public string mail_subject { get; set; }
		public string mail_excerpt { get; set; }
		public string mail_body { get; set; }
		public string mail_timestamp { get; set; }
		public string mail_date { get; set; }
		public string mail_read { get; set; }
		public string content_type { get; set; }
		public string source_id { get; set; }
		public string source_mail_id { get; set; }
		public string reply_to { get; set; }
		public string mail_size { get; set; }
		public string ver { get; set; }
		public string ref_mid { get; set; }
		public string sid_token { get; set; }
		public Auth auth { get; set; }
	}

	class GuerrillaMail
	{
		private HttpClient _client;

		private string _address;

		public GuerrillaMail(string address)
		{
			_address = address;
			_client = new HttpClient();

			var form = new Dictionary<string, string>
			{
				{ "email_user", address },
				{ "lang", "en" },
				{ "site", "guerrillamail.com" },
			};

			_client.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
			_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiToken", "0f7e5494cca61573d29d7a890101dd5b9d844b4c19f17e9c722e108445489703");

			var content = new FormUrlEncodedContent(form);
			var resp = _client.PostAsync("https://www.guerrillamail.com/ajax.php?f=set_email_user", content).Result;
		}

		public GuerillaMailList GetMail()
		{		
			var resp = _client.GetAsync("https://www.guerrillamail.com/ajax.php?f=get_email_list&offset=0&site=guerrillamail.com").Result;
			return JsonConvert.DeserializeObject<GuerillaMailList>(resp.Content.ReadAsStringAsync().Result);
		}

		public GuerillaEmail FetchEmail(int emailId)
		{
			var resp = _client.GetAsync($"https://www.guerrillamail.com/ajax.php?f=fetch_email&email_id=mr_{emailId}&site=guerrillamail.com").Result;
			return JsonConvert.DeserializeObject<GuerillaEmail>(resp.Content.ReadAsStringAsync().Result);
		}

	}
}
