using System;

namespace JudgeWeb.Areas.Api.Models
{
    public class ContestSubmission
    {
        public string language_id { get; set; }
        public DateTimeOffset time { get; set; }
        public TimeSpan contest_time { get; set; }
        public string id { get; set; }
        public string externalid { get; set; }
        public string team_id { get; set; }
        public string problem_id { get; set; }
        public string entry_point { get; set; }
        public FileMeta[] files { get; set; }

        public class FileMeta
        {
            public string href { get; set; }
            public string mime { get; set; }
        }
    }
}
