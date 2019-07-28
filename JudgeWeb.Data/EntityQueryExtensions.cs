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
        public static IQueryable<Testcase> WithoutBlob(this IQueryable<Testcase> query)
        {
            return query.Select(t => new Testcase
            {
                TestcaseId = t.TestcaseId,
                Description = t.Description,
                InputLength = t.InputLength,
                IsSecret = t.IsSecret,
                Md5sumInput = t.Md5sumInput,
                Md5sumOutput = t.Md5sumOutput,
                OutputLength = t.OutputLength,
                Point = t.Point,
                ProblemId = t.ProblemId,
                Rank = t.Rank,
            });
        }

        public static IQueryable<Executable> WithoutBlob(this IQueryable<Executable> query)
        {
            return query.Select(e => new Executable
            {
                ExecId = e.ExecId,
                Description = e.Description,
                Md5sum = e.Md5sum,
                Type = e.Type,
                ZipSize = e.ZipSize,
            });
        }

        public static bool CheckPermission(this Clarification clar, int teamid)
        {
            return !clar.Recipient.HasValue || clar.Recipient == teamid || clar.Sender == teamid;
        }

        public static ContestState GetState(this Contest cst, DateTime? nowTime = null)
        {
            var now = nowTime ?? DateTime.Now;

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
