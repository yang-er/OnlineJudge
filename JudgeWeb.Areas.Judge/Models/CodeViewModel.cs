using JudgeWeb.Data;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Judge.Models
{
    public class CodeViewModel : Submission
    {
        public CodeViewModel() { }

        public Verdict Status { get; set; }
        public int ExecuteTime { get; set; }
        public int ExecuteMemory { get; set; }
        public int ServerId { get; set; }
        public int JudgingId { get; set; }
        public string CompileError { get; set; }

        public string ProblemTitle { get; set; }
        public string LanguageName { get; set; }
        public string ServerName { get; set; }
        public int TestcaseNumber { get; set; }
        public string LanguageExternalId { get; set; }

        public IList<Detail> Details { get; set; }
    }
}