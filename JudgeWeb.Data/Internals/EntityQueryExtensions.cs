using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace JudgeWeb.Data
{
    public enum ContestState
    {
        NotScheduled,
        ScheduledToStart,
        Started,
        Frozen,
        Ended,
        Finalized,
    }

    public static class EntityQueryExtensions
    {

        public static bool CheckPermission(this Clarification clar, int teamid)
        {
            return !clar.Recipient.HasValue || clar.Recipient == teamid || clar.Sender == teamid;
        }

        public static ContestState GetState(this Contest cst, DateTimeOffset? nowTime = null)
        {
            var now = nowTime ?? DateTimeOffset.Now;

            var startTime = cst.StartTime;
            var endTime = cst.EndTime;
            var freezeTime = cst.FreezeTime;
            var unfreezeTime = cst.UnfreezeTime;

            if (!startTime.HasValue)
                return ContestState.NotScheduled;
            if (startTime.Value > now)
                return ContestState.ScheduledToStart;
            if (!endTime.HasValue)
                return ContestState.Started;

            if (freezeTime.HasValue)
            {
                if (unfreezeTime.HasValue && unfreezeTime.Value < now)
                    return ContestState.Finalized;
                if (endTime.Value < now)
                    return ContestState.Ended;
                if (freezeTime.Value < now)
                    return ContestState.Frozen;
                return ContestState.Started;
            }
            else
            {
                if (endTime.Value < now)
                    return ContestState.Finalized;
                return ContestState.Started;
            }
        }
    }
}
