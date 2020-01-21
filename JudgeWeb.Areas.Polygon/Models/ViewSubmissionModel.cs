using JudgeWeb.Data;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Polygon.Models
{
    public class ViewSubmissionModel
    {
        public ViewSubmissionModel() { }

        public bool CombinedRunCompare { get; set; }
        public int SubmissionId { get; set; }
        public Verdict Status { get; set; }
        public Verdict? Expected { get; set; }
        public int ExecuteTime { get; set; }
        public int ExecuteMemory { get; set; }
        public int JudgingId { get; set; }
        public int ContestId { get; set; }
        public string CompileError { get; set; }
        public string LanguageId { get; set; }
        public int Author { get; set; }

        public string LanguageName { get; set; }
        public string ServerName { get; set; }
        public int TestcaseNumber { get; set; }
        public string LanguageExternalId { get; set; }

        public IEnumerable<(Detail, Testcase)> Details { get; set; }

        public Judging Judging { get; set; }
        public int TimeLimit { get; set; }
        public double TimeFactor { get; set; }
        public DateTimeOffset Time { get; set; }
        public string UserName { get; set; }

        public IEnumerable<Judging> AllJudgings { get; set; }
        public string SourceCode { get; set; }
    }
}
