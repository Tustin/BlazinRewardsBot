using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazinrewards
{
	class Survey
	{
		public class SurveyResponse
		{
			public string AnswerId { get; set; }
			public string ResponseDate { get; set; }
			public string QuestionId { get; set; }
		}

		public string ProfileId { get; set; }
		public string SurveyId { get; set; }
		public List<SurveyResponse> SurveyResponses { get; set; }
	}
}
