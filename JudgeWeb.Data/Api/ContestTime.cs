using System;

namespace JudgeWeb.Data.Api
{
    [EntityType("state")]
    public class ContestTime : ContestEventEntity
    {
        public DateTimeOffset? started { get; set; }
        public DateTimeOffset? ended { get; set; }
        public DateTimeOffset? frozen { get; set; }
        public DateTimeOffset? thawed { get; set; }
        public DateTimeOffset? finalized { get; set; }
        public DateTimeOffset? end_of_updates { get; set; }

        public ContestTime() { }

        public ContestTime(Contest ctx)
        {
            id = $"{ctx.ContestId}";

            switch (ctx.GetState())
            {
                case ContestState.Finalized:
                    end_of_updates = DateTimeOffset.Now;
                    thawed = ctx.UnfreezeTime;
                    ended = ctx.EndTime;
                    finalized = frozen.HasValue ? thawed : ended;
                    frozen = ctx.FreezeTime;
                    started = ctx.StartTime;
                    break;
                case ContestState.Ended:
                    ended = ctx.EndTime;
                    frozen = ctx.FreezeTime;
                    started = ctx.StartTime;
                    break;
                case ContestState.Frozen:
                    frozen = ctx.FreezeTime;
                    started = ctx.StartTime;
                    break;
                case ContestState.Started:
                    started = ctx.StartTime;
                    break;
                case ContestState.NotScheduled:
                case ContestState.ScheduledToStart:
                default:
                    break;
            }
        }
    }
}
