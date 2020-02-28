using System;

namespace JudgeWeb.Data.Api
{
    [EntityType("clarifications")]
    public class ContestClarification : ContestEventEntity
    {
        public DateTimeOffset time { get; set; }
        public TimeSpan contest_time { get; set; }
        public string reply_to_id { get; set; }
        public string from_team_id { get; set; }
        public string to_team_id { get; set; }
        public string problem_id { get; set; }
        public string text { get; set; }

        public ContestClarification() { }

        public ContestClarification(Clarification c, DateTimeOffset contestTime)
        {
            time = c.SubmitTime;
            contest_time = c.SubmitTime - contestTime;
            id = $"{c.ClarificationId}";
            reply_to_id = c.ResponseToId?.ToString();
            from_team_id = c.Sender?.ToString();
            to_team_id = c.Recipient?.ToString();
            problem_id = c.ProblemId?.ToString();
            text = c.Body;
        }

        protected override DateTimeOffset GetTime(string action) => time;
    }
}
