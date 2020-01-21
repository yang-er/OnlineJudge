using System;

namespace JudgeWeb.Data.Api
{
    [EntityType("judgements")]
    public class ContestJudgement : ContestEventEntity
    {
        public double? max_run_time { get; set; }
        public DateTimeOffset? start_time { get; set; }
        public TimeSpan? start_contest_time { get; set; }
        public DateTimeOffset? end_time { get; set; }
        public TimeSpan? end_contest_time { get; set; }
        public string submission_id { get; set; }
        public bool valid { get; set; }
        public string judgehost { get; set; }
        public string judgement_type_id { get; set; }

        public ContestJudgement(Judging j, DateTimeOffset contestTime)
        {
            id = $"{j.JudgingId}";
            submission_id = $"{j.SubmissionId}";
            judgehost = j.Server;
            judgement_type_id = JudgementType.For(j.Status);
            valid = j.Active;
            start_contest_time = j.StartTime.Value - contestTime;
            start_time = j.StartTime.Value;

            if (judgement_type_id != null)
            {
                end_contest_time = j.StopTime.Value - contestTime;
                end_time = j.StopTime.Value;
                if (judgement_type_id != "CE" && judgement_type_id != "JE")
                    max_run_time = j.ExecuteTime / 1000.0;
            }
        }

        protected override DateTimeOffset GetTime(string action) =>
            (action == "create" ? start_time : end_time).Value;
    }
}
