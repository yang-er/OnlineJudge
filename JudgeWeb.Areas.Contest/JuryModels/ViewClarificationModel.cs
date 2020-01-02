using JudgeWeb.Data;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryViewClarificationModel
    {
        public Clarification Main { get; set; }

        public IEnumerable<Clarification> Associated { get; set; }

        public IEnumerable<KeyValuePair<int, string>> Teams { get; set; }

        public ContestProblem[] Problems { get; set; }

        public string UserName { get; set; }
    }
}
