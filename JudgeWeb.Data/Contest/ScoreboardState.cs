using System;

namespace JudgeWeb.Data
{
    public struct ScoreboardState
    {
        public int SubmissionId { get; set; }
        public int TeamId { get; set; }
        public int ContestId { get; set; }
        public int ProblemId { get; set; }

        public Verdict? Verdict { get; set; }
        public int RankStrategy { get; set; }
        public bool UseBalloon { get; set; }
        
        public DateTimeOffset Time { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? FreezeTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public DateTimeOffset? UnfreezeTime { get; set; }

        public ContestState GetState()
        {
            if (!StartTime.HasValue)
                return ContestState.NotScheduled;
            if (StartTime.Value > Time)
                return ContestState.ScheduledToStart;
            if (!EndTime.HasValue)
                return ContestState.Started;

            if (FreezeTime.HasValue)
            {
                if (UnfreezeTime.HasValue && UnfreezeTime.Value < Time)
                    return ContestState.Finalized;
                if (EndTime.Value < Time)
                    return ContestState.Ended;
                if (FreezeTime.Value < Time)
                    return ContestState.Frozen;
                return ContestState.Started;
            }
            else
            {
                if (EndTime.Value < Time)
                    return ContestState.Finalized;
                return ContestState.Started;
            }
        }
    }
}
