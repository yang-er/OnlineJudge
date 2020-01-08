using System;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Contest.Models
{
    public class SubmissionViewModel
    {
        public int SubmissionId { get; set; }

        public Verdict Verdict { get; set; }

        public DateTimeOffset Time { get; set; }

        public int TeamId { get; set; }

        public string TeamName { get; set; }

        public Data.ContestProblem Problem { get; set; }

        public Data.Language Language { get; set; }

        public IEnumerable<Verdict> Details { get; set; }

        public int Grade { get; set; }

        public string CompilerOutput { get; set; }

        public string SourceCode { get; set; }
    }
}
