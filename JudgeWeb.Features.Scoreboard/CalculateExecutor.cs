using EFCore.BulkExtensions;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public class CalculateExecutor : IDisposable
    {
        private AppDbContext DbContext { get; }

        private ILogger<CalculateExecutor> Logger { get; }

        public CalculateExecutor(AppDbContext adbc, ILogger<CalculateExecutor> log)
        {
            DbContext = adbc;
            Logger = log;
        }

        /// <summary>
        /// 检查比赛状态的更新，例如比赛开始结束时间之类的。
        /// </summary>
        /// <returns>是否需要下一次再来</returns>
        public async Task<bool> GetContestLogs()
        {
            var contestLogs = await DbContext.AuditLogs
                .Where(a => a.Resolved == false && a.Type == AuditLog.TargetType.Contest)
                .FirstOrDefaultAsync();
            if (contestLogs == null) return false;

            if (contestLogs.Comment != "updated")
            {
                Logger.LogWarning("Unrecognized comment: {comment}", contestLogs.Comment);
            }

            int cid = contestLogs.ContestId;

            var contest = await DbContext.Contests
                .Where(c => c.ContestId == cid)
                .FirstAsync();

            var scoreCache = new Dictionary<(int, int), ScoreCache>();
            var rankCache = new Dictionary<int, RankCache>();

            if (contest.StartTime.HasValue)
            {
                // Use logs to join out solved judgings
                var querySolvedJudgings =
                    from log in DbContext.AuditLogs
                    where log.LogId < contestLogs.LogId
                        && log.Type == AuditLog.TargetType.Judging
                        && log.ContestId == contestLogs.ContestId
                    join j in DbContext.Judgings
                        on log.EntityId equals j.JudgingId
                    where j.Active
                    join s in DbContext.Submissions
                        on j.SubmissionId equals s.SubmissionId
                    join t in DbContext.Teams
                        on new { s.ContestId, TeamId = s.Author }
                        equals new { t.ContestId, t.TeamId }
                    join c in DbContext.TeamCategories
                        on t.CategoryId equals c.CategoryId
                    select new
                    {
                        Log = log,
                        Status = j.Status,
                        SubmissionId = s.SubmissionId,
                        SubmitTime = s.Time,
                        SortOrder = c.SortOrder,
                        ContestId = log.ContestId,
                        TeamId = t.TeamId,
                        ProblemId = s.ProblemId,
                    };

                var solvedJudgings = await querySolvedJudgings
                    .ToListAsync();

                var solvedSubmissions =
                    from log in DbContext.AuditLogs
                    where log.LogId < contestLogs.LogId
                        && log.Type == AuditLog.TargetType.Judging
                        && log.ContestId == contestLogs.ContestId
                    join j in DbContext.Judgings
                        on log.EntityId equals j.JudgingId
                    where j.Active
                    select j.SubmissionId;

                var queryPendingJudgings =
                    from log in DbContext.AuditLogs
                    where log.LogId < contestLogs.LogId
                        && log.Type == AuditLog.TargetType.Submission
                        && log.ContestId == contestLogs.ContestId
                        && !solvedSubmissions.Contains(log.EntityId)
                    join s in DbContext.Submissions
                        on log.EntityId equals s.SubmissionId
                    join t in DbContext.Teams
                        on new { s.ContestId, TeamId = s.Author }
                        equals new { t.ContestId, t.TeamId }
                    join c in DbContext.TeamCategories
                        on t.CategoryId equals c.CategoryId
                    select new
                    {
                        Log = log,
                        Status = Verdict.Pending,
                        SubmissionId = s.SubmissionId,
                        SubmitTime = s.Time,
                        SortOrder = c.SortOrder,
                        ContestId = log.ContestId,
                        TeamId = t.TeamId,
                        ProblemId = s.ProblemId,
                    };

                var pendingJudgings = await queryPendingJudgings
                    .ToListAsync();

                var unsolvedHistory = solvedJudgings;
                unsolvedHistory.AddRange(pendingJudgings);
                unsolvedHistory.Sort((a1, a2) => a1.SubmitTime.CompareTo(a2.SubmitTime));
                var solvedProblems = new HashSet<(int, int)>();

                foreach (var item in unsolvedHistory)
                {
                    if (item.Status == Verdict.CompileError) continue;

                    var key = (item.TeamId, item.ProblemId);

                    if (!scoreCache.ContainsKey(key))
                        scoreCache.Add(key, new ScoreCache
                        {
                            TeamId = key.TeamId,
                            ProblemId = key.ProblemId,
                            ContestId = item.Log.ContestId
                        });

                    var sc = scoreCache[key];

                    if (sc.IsCorrectRestricted) continue;

                    if (item.Status == Verdict.Pending)
                    {
                        sc.PendingRestricted++;
                    }
                    else if (item.Status == Verdict.Accepted)
                    {
                        sc.IsCorrectRestricted = true;
                        sc.SolveTimeRestricted = (item.SubmitTime - contest.StartTime).Value.TotalSeconds;

                        if (!solvedProblems.Contains((item.ProblemId, item.SortOrder)))
                        {
                            solvedProblems.Add((item.ProblemId, item.SortOrder));
                            sc.FirstToSolve = true;
                        }

                        if (!rankCache.ContainsKey(item.TeamId))
                            rankCache.Add(item.TeamId, new RankCache
                            {
                                TeamId = key.TeamId,
                                ContestId = item.Log.ContestId,
                            });

                        var rc = rankCache[item.TeamId];
                        rc.PointsRestricted++;
                        rc.TotalTimeRestricted += sc.SolveTimeRestricted >= 0
                            ? (int)(sc.SolveTimeRestricted / 60)
                            : -(int)(-sc.SolveTimeRestricted / 60);
                    }

                    sc.SubmissionRestricted++;

                    if (contest.GetState(item.SubmitTime.DateTime) < ContestState.Frozen)
                    {
                        sc.PendingPublic = sc.PendingRestricted;
                        sc.SolveTimePublic = sc.SolveTimeRestricted;
                        sc.IsCorrectPublic = sc.IsCorrectRestricted;
                        sc.SubmissionPublic = sc.SubmissionRestricted;

                        if (item.Status == Verdict.Accepted)
                        {
                            var rc = rankCache[item.TeamId];
                            rc.PointsPublic = rc.PointsRestricted;
                            rc.TotalTimePublic = rc.TotalTimeRestricted;
                        }
                    }
                }
            }

            await DbContext.RankCache
                .Where(rc => rc.ContestId == cid)
                .BatchDeleteAsync();
            await DbContext.ScoreCache
                .Where(rc => rc.ContestId == cid)
                .BatchDeleteAsync();
            await DbContext.AuditLogs
                .Where(a => a.ContestId == cid && a.LogId <= contestLogs.LogId)
                .BatchUpdateAsync(a => new AuditLog { Resolved = true });
            await DbContext
                .BulkInsertAsync(scoreCache.Values.ToList());
            await DbContext
                .BulkInsertAsync(rankCache.Values.ToList());
            return true;
        }

        public async Task<bool> UpdateSJLogs()
        {
            var otherLogs = await DbContext.AuditLogs
                .Where(a => a.Resolved == false)
                .OrderBy(a => a.Time)
                .Take(10)
                .ToListAsync();

            if (otherLogs.Count == 0) return false;

            var cids = otherLogs
                .Select(a => a.ContestId)
                .Distinct()
                .ToArray();

            var contests = await DbContext.Contests
                .Where(c => cids.Contains(c.ContestId))
                .ToDictionaryAsync(c => c.ContestId);

            var lrc = new List<RankCache>();
            var lsc = new List<ScoreCache>();

            foreach (var log in otherLogs)
            {
                log.Resolved = true;

                if (log.Type == AuditLog.TargetType.Submission)
                {
                    var sid = log.EntityId;
                    var cts = contests[log.ContestId];
                    if (!cts.StartTime.HasValue) continue;

                    var s = await DbContext.Submissions
                        .Where(ss => ss.SubmissionId == sid)
                        .FirstAsync();
                    var sc = lsc.FirstOrDefault(cc => cc.ContestId == log.ContestId && cc.TeamId == s.Author && cc.ProblemId == s.ProblemId);
                    sc = sc ?? await DbContext.ScoreCache
                        .Where(cc => cc.ContestId == log.ContestId && cc.TeamId == s.Author && cc.ProblemId == s.ProblemId)
                        .FirstOrDefaultAsync();
                    sc = sc ?? new ScoreCache { ContestId = log.ContestId, TeamId = s.Author, ProblemId = s.ProblemId };
                    var stat = cts.GetState(s.Time.DateTime);

                    if (!sc.IsCorrectRestricted
                        && stat < ContestState.Ended
                        && stat > ContestState.NotScheduled)
                    {
                        sc.PendingRestricted++;
                        sc.PendingPublic++;
                        if (!lsc.Contains(sc))
                            lsc.Add(sc);
                    }
                }
                else if (log.Type == AuditLog.TargetType.Judging)
                {
                    var jid = log.EntityId;
                    var cts = contests[log.ContestId];
                    if (!cts.StartTime.HasValue) continue;

                    var j = await DbContext.Judgings
                        .Where(jj => jj.JudgingId == jid)
                        .FirstAsync();
                    if (!j.Active) continue;

                    var s = await DbContext.Submissions
                        .Where(ss => ss.SubmissionId == j.SubmissionId)
                        .FirstAsync();
                    var sc = lsc.FirstOrDefault(cc => cc.ContestId == log.ContestId && cc.TeamId == s.Author && cc.ProblemId == s.ProblemId);
                    sc = sc ?? await DbContext.ScoreCache
                        .Where(cc => cc.ContestId == log.ContestId && cc.TeamId == s.Author && cc.ProblemId == s.ProblemId)
                        .FirstOrDefaultAsync();
                    sc = sc ?? new ScoreCache { ContestId = log.ContestId, TeamId = s.Author, ProblemId = s.ProblemId };
                    var stat = cts.GetState(s.Time.DateTime);

                    if (!sc.IsCorrectRestricted
                        && stat < ContestState.Ended
                        && stat > ContestState.NotScheduled)
                    {
                        sc.PendingRestricted--;
                        if (j.Status != Verdict.CompileError)
                            sc.SubmissionRestricted++;

                        if (j.Status == Verdict.Accepted)
                        {
                            sc.SolveTimeRestricted = (s.Time.DateTime - cts.StartTime).Value.TotalSeconds;
                            sc.IsCorrectRestricted = true;
                            var tid = s.Author;

                            var sortOrderQuery =
                                from t in DbContext.Teams
                                where t.ContestId == s.ContestId && t.TeamId == s.Author
                                join cat in DbContext.TeamCategories on t.CategoryId equals cat.CategoryId
                                select cat.SortOrder;
                            var sortorder = await sortOrderQuery.FirstAsync();

                            var anySolvedQuery =
                                from scc in DbContext.ScoreCache
                                where scc.ContestId == s.ContestId && scc.ProblemId == s.ProblemId && scc.FirstToSolve
                                join t in DbContext.Teams on new { scc.ContestId, scc.TeamId } equals new { t.ContestId, t.TeamId }
                                join cat in DbContext.TeamCategories on t.CategoryId equals cat.CategoryId
                                where cat.SortOrder == sortorder
                                select scc;
                            var anySolved = await anySolvedQuery.AnyAsync();
                            sc.FirstToSolve = !anySolved;
                        }

                        if (stat != ContestState.Frozen)
                        {
                            sc.IsCorrectPublic = sc.IsCorrectRestricted;
                            sc.PendingPublic = sc.PendingRestricted;
                            sc.SolveTimePublic = sc.SolveTimeRestricted;
                            sc.SubmissionPublic = sc.SubmissionRestricted;
                        }

                        if (j.Status == Verdict.Accepted)
                        {
                            var rc = lrc.FirstOrDefault(rcc => rcc.ContestId == s.ContestId && rcc.TeamId == s.Author);
                            rc = rc ?? await DbContext.RankCache
                                .Where(rcc => rcc.ContestId == s.ContestId && rcc.TeamId == s.Author)
                                .FirstOrDefaultAsync();
                            rc = rc ?? new RankCache { ContestId = s.ContestId, TeamId = s.Author };

                            int penalty = (sc.SubmissionRestricted - 1) * 20;
                            int basetime = sc.SolveTimeRestricted >= 0
                                         ? (int)(sc.SolveTimeRestricted / 60)
                                         : -(int)(-sc.SolveTimeRestricted / 60);
                            rc.TotalTimeRestricted += basetime + penalty;
                            rc.PointsRestricted++;

                            if (stat != ContestState.Frozen)
                            {
                                rc.TotalTimePublic = rc.TotalTimeRestricted;
                                rc.PointsPublic = rc.PointsRestricted;
                            }

                            if (!lrc.Contains(rc))
                                lrc.Add(rc);
                        }

                        if (!lsc.Contains(sc))
                            lsc.Add(sc);
                    }
                }
            }

            await DbContext.BulkUpdateAsync(otherLogs);
            await DbContext.BulkInsertOrUpdateAsync(lsc);
            await DbContext.BulkInsertOrUpdateAsync(lrc);
            return true;
        }

        public void Dispose()
        {
            ((IDisposable)DbContext).Dispose();
        }
    }
}
