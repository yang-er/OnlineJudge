using System;

namespace JudgeWeb.Areas.Api.Models
{
    public class ContestJudgement
    {
        public double? max_run_time { get; set; }
        public DateTimeOffset? start_time { get; set; }
        public TimeSpan? start_contest_time { get; set; }
        public DateTimeOffset? end_time { get; set; }
        public TimeSpan? end_contest_time { get; set; }
        public string id { get; set; }
        public string submission_id { get; set; }
        public bool valid { get; set; }
        public string judgehost { get; set; }
        public string judgement_type_id { get; set; }
    }
}
