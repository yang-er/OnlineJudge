using System;

namespace JudgeWeb.Areas.Judge.Models
{
    public class StatusListModel
    {
        public int SubmissionId { get; set; }
        public DateTimeOffset Time { get; set; }
        public int Author { get; set; }
        public int CodeLength { get; set; }
        public Verdict Status { get; set; }
        public int ProblemId { get; set; }
        public int ExecuteTime { get; set; }
        public int ExecuteMemory { get; set; }
        public int ContestId { get; set; }
        public string UserName { get; set; }
        public int LanguageId { get; set; }
        public string Language { get; set; }
    }
}
