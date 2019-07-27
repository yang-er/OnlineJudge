using System;

namespace JudgeWeb.Areas.Contest.Models
{
    public class TeamViewSubmissionModel
    {
        public DateTimeOffset Time { get; set; }

        public int SubmissionId { get; set; }

        public Verdict Verdict { get; set; }

        public int ProblemId { get; set; }

        public string Language { get; set; }

        public int Grade { get; set; }

        public string ProblemShortName { get; set; }

        public string ProblemName { get; set; }

        public string CompilerOutput { get; set; }
    }
}
