using JudgeWeb.Data;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Domains.Problems
{
    public class ListSubmissionModel
    {
        public int SubmissionId { get; set; }

        public int JudgingId { get; set; }

        public int ProblemId { get; set; }

        public int ContestId { get; set; }

        public DateTimeOffset Time { get; set; }

        public int AuthorId { get; set; }

        public int CodeLength { get; set; }

        public string Language { get; set; }

        public int? ExecutionTime { get; set; }

        public int? ExecutionMemory { get; set; }

        public Verdict? Expected { get; set; }

        public Verdict Verdict { get; set; }

        public ICollection<Detail> Details { get; set; }

        public string AuthorName { get; set; }

        public string Ip { get; set; }
    }
}
