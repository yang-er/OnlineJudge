﻿using System;

namespace JudgeWeb.Data.Api
{
    [EntityType("runs")]
    public class ContestRun : ContestEventEntity
    {
        public double run_time { get; set; }
        public DateTimeOffset time { get; set; }
        public TimeSpan contest_time { get; set; }
        public int ordinal { get; set; }
        public string judgement_id { get; set; }
        public string judgement_type_id { get; set; }

        public ContestRun() { }

        public ContestRun(DateTimeOffset time, TimeSpan span, int runid, int jid, Verdict v, int rank, int timems)
        {
            this.time = time;
            contest_time = span;
            id = $"{runid}";
            judgement_id = $"{jid}";
            judgement_type_id = JudgementType.For(v);
            ordinal = rank;
            run_time = timems / 1000.0;
        }

        protected override DateTimeOffset GetTime(string action) => time;
    }
}
