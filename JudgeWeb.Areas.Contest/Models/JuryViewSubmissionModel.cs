using JudgeWeb.Data;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryViewSubmissionModel
    {
        public JuryViewSubmissionModel() { }

        public int SubmissionId { get; set; }
        public Verdict Status { get; set; }
        public int ExecuteTime { get; set; }
        public int ExecuteMemory { get; set; }
        public int ServerId { get; set; }
        public int JudgingId { get; set; }
        public string CompileError { get; set; }
        public int LanguageId { get; set; }
        public int ProblemId { get; set; }
        public int Author { get; set; }

        public string ProblemTitle { get; set; }
        public string LanguageName { get; set; }
        public string ServerName { get; set; }
        public int TestcaseNumber { get; set; }
        public string LanguageExternalId { get; set; }

        public IList<Detail> Details { get; set; }

        public Judging Judging { get; set; }
        public string ProblemShortName { get; set; }
        public int ContestId { get; set; }
        public int TimeLimit { get; set; }
        public Team Team { get; set; }
        public DateTimeOffset Time { get; set; }

        public IEnumerable<(Judging g, string n)> AllJudgings { get; set; }
        public string SourceCode { get; set; }
    }
}
