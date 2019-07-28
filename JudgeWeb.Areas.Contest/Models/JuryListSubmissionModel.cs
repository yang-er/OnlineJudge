using JudgeWeb.Data;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryListSubmissionModel
    {
        public int SubmissionId { get; set; }

        public DateTimeOffset Time { get; set; }

        public int TeamId { get; set; }

        public ContestTestcase Problem { get; set; }

        public string TeamName { get; set; }

        public string Language { get; set; }

        public Verdict Result { get; set; }

        public IEnumerable<Verdict> Details { get; set; }
    }
}
