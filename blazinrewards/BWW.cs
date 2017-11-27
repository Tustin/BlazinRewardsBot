using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazinrewards
{
	internal class BWW
	{
		public class Email
		{
			public string EmailAddress { get; set; }
		}

		public class Phone
		{
			public string PhoneNumber { get; set; }
		}

		public class Address
		{
			public string PostalCode { get; set; }
		}

		public bool GlobalOptOut { get; set; }
		public string FirstName { get; set; }
		public string EnrollChannelCode { get; set; }
		public string LastName { get; set; }
		public string Username { get; set; }
		public List<Email> Emails { get; set; }
		public string BirthDate { get; set; }
		public string Password { get; set; }
		public List<Phone> Phones { get; set; }
		public List<Address> Addresses { get; set; }
		public string SourceCode { get; set; }
	}
}
