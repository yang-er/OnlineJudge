using System;

namespace JudgeWeb.Areas.Polygon.Models
{
    public class StatusListModel
    {
        public int SubmissionId { get; set; }
        public DateTimeOffset Time { get; set; }
        public Verdict Verdict { get; set; }
        public int ProblemId { get; set; }
        public int? ExecutionTime { get; set; }
        public int? ExecutionMemory { get; set; }
        public int CodeLength { get; set; }
        public string Language { get; set; }
        public int ContestId { get; set; }
        public int Author { get; set; }
        public int JudgingId { get; set; }
    }
}
