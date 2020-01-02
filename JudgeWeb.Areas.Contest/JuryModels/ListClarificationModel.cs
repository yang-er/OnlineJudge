using JudgeWeb.Data;
using System.Collections.Generic;
using System.Linq;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryListClarificationModel
    {
        public List<Clarification> AllClarifications { get; set; }

        public ContestProblem[] Problems { get; set; }

        public string JuryName { get; set; }

        public IEnumerable<Clarification> NewRequests =>
            AllClarifications.Where(c => !c.Answered && c.Sender.HasValue);

        public IEnumerable<Clarification> OldRequests =>
            AllClarifications.Where(c => c.Answered && c.Sender.HasValue);

        public IEnumerable<Clarification> GeneralClarifications =>
            AllClarifications.Where(c => !c.Sender.HasValue);
    }
}
