using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazinrewards
{
	public class Account
	{
		public string ProgramCode { get; set; }
		public string ProfileId { get; set; }
		public string Username { get; set; }
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
		public DateTime AccessTokenExpiration { get; set; }
		public DateTime RefreshTokenExpiration { get; set; }
		public bool Success { get; set; }
		public bool RequireSsl { get; set; }
		public string TenantId { get; set; }
		public string TenantName { get; set; }
		public string SocialTokenProvider { get; set; }
		public string SocialToken { get; set; }
		public bool TermsConditionsAcceptedInd { get; set; }
	}
}
