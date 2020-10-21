using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    /// <summary>
    /// CF的榜单计算方法。
    /// </summary>
    /// <example>
    /// Restricted 按全部都是RJ计算的榜单
    /// Public     按最后一次AC计算的榜单
    /// 
    /// Pending    表示挂起数、未结束测评数 （Restricted）
    /// Submission 表示非CE提交数
    /// Score      表示显示在榜单中央的分数 （Public）
    /// IsCorrect  表示是否得到分数         （Public）
    /// SolveTime  表示最后一次提交该题的时间（Public）
    /// FirstToSolve 表示终测后是否FST
    /// 
    /// Points     表示总分数         （Public）
    /// TotalTime  表示最后一次提交时间（Public）
    /// </example>
    public class CFRank : IRankingStrategy
    {
        public IEnumerable<Team> SortByRule(IEnumerable<Team> source, bool isPublic)
            => source.OrderByDescending(a => a.RankCache.PointsPublic)
                .ThenBy(a => a.RankCache.TotalTimePublic);


        public Task CompileError(DbContext db, JudgingFinishedRequest args)
        {
            return db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .Where(s => s.PendingRestricted > 0)
                .BatchUpdateAsync(s => new ScoreCache
                {
                    PendingRestricted = s.PendingRestricted - 1,
                });
        }


        public Task Pending(DbContext db, SubmissionCreatedRequest args)
        {
            return db.Set<ScoreCache>().MergeAsync(
                sourceTable: new[] { new { args.ContestId, args.TeamId, args.ProblemId } },
                targetKey: sc => new { sc.ContestId, sc.TeamId, sc.ProblemId },
                sourceKey: sc => new { sc.ContestId, sc.TeamId, sc.ProblemId },

                updateExpression: (t, s) => new ScoreCache
                {
                    PendingRestricted = t.PendingRestricted + 1,
                },

                insertExpression: s => new ScoreCache
                {
                    ContestId = s.ContestId,
                    TeamId = s.TeamId,
                    ProblemId = s.ProblemId,
                    PendingRestricted = 1,
                },

                delete: false);
        }


        public Task Reject(DbContext db, JudgingFinishedRequest args)
        {
            // 在这种情况下，是系统终测时测了某个Accepted提交
            // 我们规定 System Test 是由某个 Rejudging 关联的，但是处于 Active 状态的 Rejudging。
            bool fst = args.Judging.Active && args.Judging.RejudgeId.HasValue;

            return db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .BatchUpdateAsync(t => new ScoreCache
                {
                    SubmissionRestricted = t.SubmissionRestricted + 1,
                    SubmissionPublic = t.IsCorrectPublic ? t.SubmissionPublic : t.SubmissionPublic + 1,
                    FirstToSolve = fst,
                });
        }


        public async Task Accept(DbContext db, JudgingFinishedRequest args)
        {
            double time = (args.SubmitTime - args.ContestTime).TotalSeconds;
            int timee = (int)(time / 60);
            int minScore = args.CfScore * 3 / 10, rateScore = args.CfScore - timee * (args.CfScore / 250);

            var oldScoreQuery = db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .Select(old => new
                {
                    old.ContestId,
                    old.TeamId,
                    OldScore = old.ScorePublic ?? 0,
                    NewScore = minScore > rateScore - old.SubmissionRestricted * 50 ? minScore : rateScore - old.SubmissionRestricted * 50,
                });

            await db.Set<RankCache>().MergeAsync(
                sourceTable: oldScoreQuery,
                targetKey: rc => new { rc.ContestId, rc.TeamId },
                sourceKey: rc => new { rc.ContestId, rc.TeamId },
                delete: false,

                updateExpression: (r, s) => new RankCache
                {
                    PointsPublic = r.PointsPublic - s.OldScore + s.NewScore,
                    TotalTimePublic = timee,
                },

                insertExpression: s => new RankCache
                {
                    ContestId = s.ContestId,
                    TeamId = s.TeamId,
                    PointsPublic = s.NewScore,
                    TotalTimePublic = timee,
                });

            await db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .BatchUpdateAsync(old => new ScoreCache
                {
                    SubmissionRestricted = old.SubmissionRestricted + 1,
                    PendingRestricted    = old.PendingRestricted - 1,
                    ScorePublic = minScore > rateScore - old.SubmissionRestricted * 50 ? minScore : rateScore - old.SubmissionRestricted * 50,
                    IsCorrectPublic = true,
                    SolveTimePublic = time,
                    SubmissionPublic = old.SubmissionRestricted + 1,
                });
        }


        public async Task RefreshCache(DbContext db, RefreshScoreboardCacheRequest args)
        {
            int cid = args.ContestId;
            var scores = await db.Set<ContestProblem>()
                .Where(cp => cp.ContestId == cid)
                .Select(cp => new { cp.ProblemId, cp.Score })
                .ToDictionaryAsync(a => a.ProblemId, a => a.Score);

            var query =
                from s in db.Set<Submission>()
                where s.ContestId == cid
                join t in db.Set<Team>() on new { s.ContestId, TeamId = s.Author } equals new { t.ContestId, t.TeamId }
                join j in db.Set<Judging>() on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                orderby s.Time ascending
                select new { s.SubmissionId, t.TeamId, s.ProblemId, s.Time, j.Status };
            var results = await query.ToListAsync();

            var rcc = new Dictionary<int, RankCache>();
            var scc = new Dictionary<(int, int), ScoreCache>();
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
                    sc.PendingRestricted++;
                    continue;
                }

                if (s.Status == Verdict.Accepted)
                {
                    if (!rcc.ContainsKey(s.TeamId))
                        rcc.Add(s.TeamId, new RankCache { ContestId = cid, TeamId = s.TeamId });
                    var rc = rcc[s.TeamId];
                    int score = scores.GetValueOrDefault(sc.ProblemId);

                    var time = sc.SolveTimePublic = (s.Time - args.ContestTime).TotalSeconds;
                    var timee = (int)(time / 60);
                    int nowScore = Math.Max(score * 3 / 10, score - timee * (score / 250) - sc.SubmissionRestricted * 50);

                    rc.PointsPublic += nowScore - (sc.ScorePublic ?? 0);
                    rc.TotalTimePublic = timee;

                    sc.ScorePublic = nowScore;
                    sc.SubmissionPublic = sc.SubmissionRestricted + 1;
                    sc.IsCorrectPublic = true;
                    sc.SolveTimePublic = time;
                }

                sc.SubmissionRestricted++;
                sc.SubmissionPublic = sc.IsCorrectPublic ? sc.SubmissionPublic : sc.SubmissionRestricted + 1;
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
