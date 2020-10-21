using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    /// <summary>
    /// XCPC的榜单计算方法。
    /// </summary>
    /// <example>
    /// Restricted 对应内榜
    /// Public     对应公榜
    /// 
    /// Pending    表示挂起数、未结束测评数
    /// Submission 表示非CE提交数
    /// Score      表示显示在榜单中央的时间数字
    /// IsCorrect  表示是否正确
    /// SolveTime  表示解出题目的时间
    /// FirstToSolve 表示是否为第一个过题的人
    /// 
    /// Points     表示总分
    /// TotalTime  表示总罚时
    /// </example>
    public class XCPCRank : IRankingStrategy
    {
        public IEnumerable<Team> SortByRule(IEnumerable<Team> source, bool isPublic)
            => isPublic
                ? source.OrderByDescending(a => a.RankCache.PointsPublic)
                    .ThenBy(a => a.RankCache.TotalTimePublic)
                : source.OrderByDescending(a => a.RankCache.PointsRestricted)
                    .ThenBy(a => a.RankCache.TotalTimeRestricted);


        public async Task Accept(DbContext db, JudgingFinishedRequest args)
        {
            // first blood：此处竞态条件可以暂时无视？
            var fbQuery =
                from sc in db.Set<ScoreCache>()
                where sc.ContestId == args.ContestId && sc.ProblemId == args.ProblemId && sc.FirstToSolve
                join t in db.Set<Team>() on new { sc.ContestId, sc.TeamId } equals new { t.ContestId, t.TeamId }
                join c in db.Set<TeamCategory>() on t.CategoryId equals c.CategoryId
                where (from t in db.Set<Team>()
                       where t.ContestId == args.ContestId && t.TeamId == args.TeamId
                       join c in db.Set<TeamCategory>() on t.CategoryId equals c.CategoryId
                       select c.SortOrder).Contains(c.SortOrder)
                select t.TeamId;
            bool firstBlood = !await fbQuery.AnyAsync();

            double time = (args.SubmitTime - args.ContestTime).TotalSeconds;
            int score = time < 0 ? -(((int)-time) / 60) : ((int)time) / 60;
            bool showPublic = !args.Frozen;

            int affectedRows = await db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .Where(s => s.PendingRestricted > 0)
                .BatchUpdateAsync(s => new ScoreCache
                {
                    PendingPublic    = showPublic ? s.PendingPublic - 1    : s.PendingPublic,
                    SubmissionPublic = showPublic ? s.SubmissionPublic + 1 : s.SubmissionPublic,
                    ScorePublic      = showPublic ? score                  : (int?)null,
                    SolveTimePublic  = showPublic ? time                   : 0,
                    IsCorrectPublic  = showPublic,

                    PendingRestricted    = s.PendingRestricted - 1,
                    SubmissionRestricted = s.SubmissionRestricted + 1,
                    ScoreRestricted      = score,
                    SolveTimeRestricted  = time,
                    IsCorrectRestricted  = true,

                    FirstToSolve = firstBlood,
                });

            if (affectedRows == 0) return;

            // 将此处罚时计算交给数据库，减少数据拉取量
            var updateRankQuery = db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .Select(sc => new
                {
                    sc.ContestId, sc.TeamId,
                    Penalty = score + 20 * (sc.SubmissionRestricted - 1)
                });

            await db.Set<RankCache>().MergeAsync(
                sourceTable: updateRankQuery,
                targetKey: rc => new { rc.ContestId, rc.TeamId },
                sourceKey: rc => new { rc.ContestId, rc.TeamId },
                delete: false,

                updateExpression: (r, s) => new RankCache
                {
                    PointsRestricted    = r.PointsRestricted + 1,
                    TotalTimeRestricted = r.TotalTimeRestricted + s.Penalty,
                    PointsPublic    = showPublic ? r.PointsPublic + 1            : r.PointsPublic,
                    TotalTimePublic = showPublic ? r.TotalTimePublic + s.Penalty : r.TotalTimePublic,
                },

                insertExpression: s => new RankCache
                {
                    ContestId = s.ContestId,
                    TeamId    = s.TeamId,
                    PointsRestricted    = 1,
                    TotalTimeRestricted = s.Penalty,
                    PointsPublic    = showPublic ? 1         : 0,
                    TotalTimePublic = showPublic ? s.Penalty : 0,
                });
        }


        public Task CompileError(DbContext db, JudgingFinishedRequest args)
        {
            bool showPublic = !args.Frozen;
            return db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .Where(s => s.PendingRestricted > 0)
                .BatchUpdateAsync(s => new ScoreCache
                {
                    PendingPublic = showPublic ? s.PendingPublic - 1 : s.PendingPublic,
                    PendingRestricted = s.PendingRestricted - 1,
                });
        }


        public Task Pending(DbContext db, SubmissionCreatedRequest args)
        {
            return db.Set<ScoreCache>().MergeAsync(
                sourceTable: new[] { new { args.ContestId, args.TeamId, args.ProblemId } },
                targetKey: sc => new { sc.ContestId, sc.TeamId, sc.ProblemId },
                sourceKey: sc => new { sc.ContestId, sc.TeamId, sc.ProblemId },
                delete: false,

                updateExpression: (t, s) => new ScoreCache
                {
                    PendingPublic = t.IsCorrectRestricted ? t.PendingPublic : t.PendingPublic + 1,
                    PendingRestricted = t.IsCorrectRestricted ? t.PendingRestricted : t.PendingRestricted + 1,
                },

                insertExpression: s => new ScoreCache
                {
                    ContestId = s.ContestId,
                    TeamId = s.TeamId,
                    ProblemId = s.ProblemId,
                    PendingPublic = 1,
                    PendingRestricted = 1,
                });
        }


        public Task Reject(DbContext db, JudgingFinishedRequest args)
        {
            bool showPublic = !args.Frozen;
            int publicDelta = args.Frozen ? 0 : 1;
            return db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .Where(s => s.PendingRestricted > 0)
                .BatchUpdateAsync(s => new ScoreCache
                {
                    PendingPublic    = showPublic ? s.PendingPublic - 1    : s.PendingPublic,
                    SubmissionPublic = showPublic ? s.SubmissionPublic + 1 : s.SubmissionPublic,
                    PendingRestricted    = s.PendingRestricted - 1,
                    SubmissionRestricted = s.SubmissionRestricted + 1,
                });
        }


        public async Task RefreshCache(DbContext db, RefreshScoreboardCacheRequest args)
        {
            int cid = args.ContestId;

            var query =
                from s in db.Set<Submission>()
                where s.ContestId == cid
                join t in db.Set<Team>() on new { s.ContestId, TeamId = s.Author } equals new { t.ContestId, t.TeamId }
                join tc in db.Set<TeamCategory>() on t.CategoryId equals tc.CategoryId
                join j in db.Set<Judging>() on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                orderby s.Time ascending
                select new { s.SubmissionId, t.TeamId, tc.SortOrder, s.ProblemId, s.Time, j.Status };
            var results = await query.ToListAsync();

            var rcc = new Dictionary<int, RankCache>();
            var scc = new Dictionary<(int, int), ScoreCache>();
            var fb = new HashSet<(int, int)>();
            var oks = new List<int>();
            var endTime = args.Deadline;

            foreach (var s in results)
            {
                if (s.Time > endTime) continue;
                if (!scc.ContainsKey((s.TeamId, s.ProblemId)))
                    scc.Add((s.TeamId, s.ProblemId), new ScoreCache { ContestId = cid, TeamId = s.TeamId, ProblemId = s.ProblemId });
                var sc = scc[(s.TeamId, s.ProblemId)];
                if (sc.IsCorrectRestricted || s.Status == Verdict.CompileError) continue;

                if (s.Status == Verdict.Running || s.Status == Verdict.Pending)
                {
                    sc.PendingPublic++;
                    sc.PendingRestricted++;
                    continue;
                }

                sc.SubmissionRestricted++;

                if (s.Status == Verdict.Accepted)
                {
                    if (!rcc.ContainsKey(s.TeamId))
                        rcc.Add(s.TeamId, new RankCache { ContestId = cid, TeamId = s.TeamId });
                    var rc = rcc[s.TeamId];
                    var timee = (s.Time - args.ContestTime).TotalSeconds;

                    sc.IsCorrectRestricted = true;
                    sc.SolveTimeRestricted = timee;
                    sc.ScoreRestricted = timee < 0 ? -(((int)-timee) / 60) : ((int)timee) / 60;
                    oks.Add(s.SubmissionId);

                    int penalty = (sc.SubmissionRestricted - 1) * 20 + sc.ScoreRestricted.Value;
                    rc.PointsRestricted++;
                    rc.TotalTimeRestricted += penalty;

                    if (!fb.Contains((s.ProblemId, s.SortOrder)))
                    {
                        fb.Add((s.ProblemId, s.SortOrder));
                        sc.FirstToSolve = true;
                    }
                }

                if (args.FreezeTime.HasValue && s.Time >= args.FreezeTime.Value)
                {
                    sc.PendingPublic++;
                }
                else
                {
                    sc.SolveTimePublic = sc.SolveTimeRestricted;
                    sc.SubmissionPublic = sc.SubmissionRestricted;
                    sc.IsCorrectPublic = sc.IsCorrectRestricted;
                    sc.ScorePublic = sc.ScoreRestricted;

                    if (s.Status == Verdict.Accepted)
                    {
                        var rc = rcc[s.TeamId];
                        rc.PointsPublic = rc.PointsRestricted;
                        rc.TotalTimePublic = rc.TotalTimeRestricted;
                    }
                }
            }

            await db.Set<ScoreCache>()
                .Where(t => t.ContestId == cid)
                .BatchDeleteAsync();
            
            await db.Set<RankCache>()
                .Where(t => t.ContestId == cid)
                .BatchDeleteAsync();

            db.Set<RankCache>().AddRange(rcc.Values);
            db.Set<ScoreCache>().AddRange(scc.Values);

            await db.SaveChangesAsync();
        }
    }
}
