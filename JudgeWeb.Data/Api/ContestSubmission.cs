using System;

namespace JudgeWeb.Data.Api
{
    [EntityType("submissions")]
    public class ContestSubmission : ContestEventEntity
    {
        public string language_id { get; set; }
        public DateTimeOffset time { get; set; }
        public TimeSpan contest_time { get; set; }
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

        public ContestSubmission() { }

        public ContestSubmission(int cid, string langid, int submitid, int probid, int teamid, DateTimeOffset time, TimeSpan diff)
        {
            contest_time = diff;
            id = $"{submitid}";
            problem_id = $"{probid}";
            team_id = $"{teamid}";
            language_id = langid;
            this.time = time;

            files = new[]
            {
                new FileMeta
                {
                    href = $"contests/{cid}/submissions/{submitid}/files",
                    mime = "application/zip"
                }
            };
        }

        protected override DateTimeOffset GetTime(string action) => time;
    }
}
