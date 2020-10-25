using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    /// <summary>
    /// OI的榜单计算方法。
    /// </summary>
    /// <example>
    /// Restricted 对应内榜
    /// Public     对应公榜
    /// 
    /// Pending    表示挂起数、未结束测评数
    /// Submission 表示非CE提交数
    /// Score      表示显示在榜单中央的分数
    /// IsCorrect  表示是否得到分数
    /// SolveTime  表示最后一次提交该题的时间
    /// FirstToSolve 表示是否在此题完整通过所有测试用例
    /// 
    /// Points     表示总分数
    /// TotalTime  表示最后一次提交时间
    /// </example>
    public class OIRank : IRankingStrategy
    {
        public IEnumerable<Team> SortByRule(IEnumerable<Team> source, bool isPublic)
            => isPublic
                ? source.OrderByDescending(a => a.RankCache.PointsPublic)
                    .ThenBy(a => a.RankCache.TotalTimePublic)
                : source.OrderByDescending(a => a.RankCache.PointsRestricted)
                    .ThenBy(a => a.RankCache.TotalTimeRestricted);


        public Task Accept(DbContext db, JudgingFinishedRequest args)
        {
            return Reject(db, args);
        }


        public Task CompileError(DbContext db, JudgingFinishedRequest args)
        {
            bool showPublic = !args.Frozen;
            return db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .BatchUpdateAsync(s => new ScoreCache
                {
                    PendingPublic = showPublic ? s.PendingPublic - 1 : s.PendingPublic,
                    PendingRestricted = s.PendingRestricted - 1,
                });
        }


        public async Task Pending(DbContext db, SubmissionCreatedRequest args)
        {
            await db.Set<ScoreCache>().MergeAsync(
                sourceTable: new[] { new { args.ContestId, args.ProblemId, args.TeamId } },
                targetKey: args => new { args.ContestId, args.ProblemId, args.TeamId },
                sourceKey: args => new { args.ContestId, args.ProblemId, args.TeamId },
                delete: false,

                updateExpression: (s, _) => new ScoreCache
                {
                    PendingPublic = s.PendingPublic + 1,
                    PendingRestricted = s.PendingRestricted + 1,
                },

                insertExpression: r => new ScoreCache
                {
                    ContestId = r.ContestId,
                    TeamId = r.TeamId,
                    ProblemId = r.ProblemId,
                    PendingPublic = 1,
                    PendingRestricted = 1,
                });
        }


        public async Task Reject(DbContext db, JudgingFinishedRequest args)
        {
            bool showPublic = !args.Frozen;
            double time = (args.SubmitTime - args.ContestTime).TotalSeconds;
            int time2 = (int)(time / 60);
            int score = args.TotalScore;
            bool fullScore = args.Verdict == Verdict.Accepted;
            bool isCorrect = fullScore || args.TotalScore > 0;
            
            // 反向操作
            var oldScoreQuery = db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .Select(sc => new { sc.ContestId, sc.TeamId, Delta = score - (sc.ScoreRestricted ?? 0) });

            await db.Set<RankCache>().MergeAsync(
                sourceTable: oldScoreQuery,
                targetKey: args => new { args.ContestId, args.TeamId },
                sourceKey: args => new { args.ContestId, args.TeamId },
                delete: false,
                
                updateExpression: (rc, dt) => new RankCache
                {
                    PointsRestricted    = rc.PointsRestricted + dt.Delta,
                    TotalTimeRestricted = time2,

                    PointsPublic    = showPublic ? rc.PointsPublic + dt.Delta : rc.PointsPublic,
                    TotalTimePublic = showPublic ? time2 : rc.TotalTimePublic,
                },
                
                insertExpression: dt => new RankCache
                {
                    ContestId = dt.ContestId,
                    TeamId    = dt.TeamId,

                    PointsRestricted    = dt.Delta,
                    TotalTimeRestricted = time2,

                    PointsPublic    = showPublic ? dt.Delta : 0,
                    TotalTimePublic = showPublic ? time2    : 0,
                });

            await db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .BatchUpdateAsync(sc => new ScoreCache
                {
                    ScoreRestricted      = score,
                    SolveTimeRestricted  = time,
                    IsCorrectRestricted  = isCorrect,
                    PendingRestricted    = sc.PendingRestricted - 1,
                    SubmissionRestricted = sc.SubmissionRestricted + 1,

                    ScorePublic      = showPublic ? score                   : sc.ScorePublic,
                    SolveTimePublic  = showPublic ? time                    : sc.SolveTimePublic,
                    IsCorrectPublic  = showPublic ? isCorrect               : sc.IsCorrectPublic,
                    PendingPublic    = showPublic ? sc.PendingPublic - 1    : sc.PendingPublic,
                    SubmissionPublic = showPublic ? sc.SubmissionPublic + 1 : sc.SubmissionPublic,

                    FirstToSolve = fullScore,
                });
        }


        public async Task RefreshCache(DbContext db, RefreshScoreboardCacheRequest args)
        {
            int cid = args.ContestId;

            var rebuildScoreQuery =
                from j in db.Set<Judging>()
                join s in db.Set<Submission>() on j.SubmissionId equals s.SubmissionId
                where s.ContestId == cid
                join d in db.Set<Detail>() on j.JudgingId equals d.JudgingId
                join t in db.Set<Testcase>() on d.TestcaseId equals t.TestcaseId
                group d.Status == Verdict.Accepted ? t.Point : 0 by j.JudgingId into g
                select new { JudgingId = g.Key, Score = g.Sum() };

            await db.Set<Judging>().MergeAsync(
                sourceTable: rebuildScoreQuery,
                targetKey: j => j.JudgingId,
                sourceKey: j => j.JudgingId,
                updateExpression: (j, s) => new Judging { TotalScore = s.Score },
                insertExpression: null, delete: false);

            var query =
                from s in db.Set<Submission>()
                where s.ContestId == cid
                join j in db.Set<Judging>() on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                orderby s.Time ascending
                select new { s.SubmissionId, TeamId = s.Author, s.ProblemId, s.Time, j.Status, j.TotalScore };
            var results = await query.ToListAsync();

            var scrs =
                from cp in db.Set<ContestProblem>()
                where cp.ContestId == cid
                join t in db.Set<Testcase>() on cp.ProblemId equals t.ProblemId
                select new { cp.ProblemId, t.Point };
            var q = await scrs.ToListAsync();
            var full = q.GroupBy(a => a.ProblemId).ToDictionary(a => a.Key, a => a.Sum(aa => aa.Point));

            var rcc = new Dictionary<int, RankCache>();
            var scc = new Dictionary<(int, int), ScoreCache>();
            var lastop1 = new Dictionary<int, int>();
            var lastop2 = new Dictionary<int, int>();
            var endTime = args.Deadline;

            foreach (var s in results)
            {
                if (s.Time > endTime) continue;
                if (!scc.ContainsKey((s.TeamId, s.ProblemId)))
                    scc.Add((s.TeamId, s.ProblemId), new ScoreCache { ContestId = cid, TeamId = s.TeamId, ProblemId = s.ProblemId });
                var sc = scc[(s.TeamId, s.ProblemId)];
                if (s.Status == Verdict.CompileError) continue;

                if (s.Status == Verdict.Running || s.Status == Verdict.Pending)
                {
                    sc.PendingPublic++;
                    sc.PendingRestricted++;
                    continue;
                }

                sc.SubmissionRestricted++;
                sc.IsCorrectRestricted = s.Status == Verdict.Accepted || s.TotalScore != 0;
                sc.ScoreRestricted = s.TotalScore;
                sc.SolveTimeRestricted = (s.Time - args.ContestTime).TotalMinutes;
                sc.FirstToSolve = s.Status == Verdict.Accepted;
                if (lastop2.ContainsKey(s.TeamId))
                    lastop2[s.TeamId] = (int)(s.Time - args.ContestTime).TotalMinutes;
                else
                    lastop2.Add(s.TeamId, (int)(s.Time - args.ContestTime).TotalMinutes);

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
                    if (lastop1.ContainsKey(s.TeamId))
                        lastop1[s.TeamId] = (int)(s.Time - args.ContestTime).TotalMinutes;
                    else
                        lastop1.Add(s.TeamId, (int)(s.Time - args.ContestTime).TotalMinutes);
                }
            }

            foreach (var r in scc.GroupBy(t => t.Key.Item1, v => v.Value))
            {
                var item = new RankCache
                {
                    ContestId = cid,
                    TeamId = r.Key,
                };

                foreach (var rr in r)
                {
                    item.PointsPublic += rr.ScorePublic ?? 0;
                    item.PointsRestricted += rr.ScoreRestricted ?? 0;
                    item.TotalTimePublic = lastop1.GetValueOrDefault(r.Key);
                    item.TotalTimeRestricted = lastop2.GetValueOrDefault(r.Key);
                }

                rcc.Add(r.Key, item);
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
