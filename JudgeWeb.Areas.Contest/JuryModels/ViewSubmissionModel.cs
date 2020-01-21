using JudgeWeb.Data;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryViewSubmissionModel
    {
        public Submission Submission { get; set; }
        public Judging Judging { get; set; }
        public IEnumerable<Judging> AllJudgings { get; set; }
        public ContestProblem Problem { get; set; }
        public Language Language { get; set; }
        public Team Team { get; set; }
        public IEnumerable<(Detail r, Testcase t)> Details { get; set; }
    }
}
