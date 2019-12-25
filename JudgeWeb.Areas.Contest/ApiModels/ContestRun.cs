using System;

namespace JudgeWeb.Areas.Api.Models
{
    public class ContestRun
    {
        public double run_time { get; set; }
        public DateTimeOffset time { get; set; }
        public TimeSpan contest_time { get; set; }
        public int ordinal { get; set; }
        public string id { get; set; }
        public string judgement_id { get; set; }
        public string judgement_type_id { get; set; }
    }
}
