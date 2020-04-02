using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public class XCPCRank : IRankingStrategy
    {
        public IEnumerable<Team> SortByRule(IEnumerable<Team> source, bool isPublic)
            => isPublic
                ? source.OrderByDescending(a => a.RankCache.PointsPublic)
                    .ThenBy(a => a.RankCache.TotalTimePublic)
                : source.OrderByDescending(a => a.RankCache.PointsRestricted)
                    .ThenBy(a => a.RankCache.TotalTimeRestricted);


        public async Task Accept(ScoreboardContext db, ScoreboardEventArgs args)
        {
            Expression<Func<ScoreCache, ScoreCache>> scp;

            // first blood
            var fbQuery =
                from sc in db.ScoreCache
                where sc.ContestId == args.ContestId && sc.ProblemId == args.ProblemId && sc.FirstToSolve
                join t in db.Teams on new { sc.ContestId, sc.TeamId } equals new { t.ContestId, t.TeamId }
                join c in db.Categories on t.CategoryId equals c.CategoryId
                where (from t in db.Teams
                       where t.ContestId == args.ContestId && t.TeamId == args.TeamId
                       join c in db.Categories on t.CategoryId equals c.CategoryId
                       select c.SortOrder).Contains(c.SortOrder)
                select new { tid = sc.TeamId };
            bool firstBlood = !await fbQuery.AnyAsync();
            double time = (args.SubmitTime - args.ContestTime).TotalSeconds;

            if (args.Frozen)
            {
                scp = s => new ScoreCache
                {
                    PendingRestricted = s.PendingRestricted - 1,
                    SubmissionRestricted = s.SubmissionRestricted + 1,
                    IsCorrectRestricted = true,
                    SolveTimeRestricted = time,
                    FirstToSolve = firstBlood,
                };
            }
            else
            {
                scp = s => new ScoreCache
                {
                    PendingPublic = s.PendingPublic - 1,
                    SubmissionPublic = s.SubmissionPublic + 1,
                    IsCorrectPublic = true,
                    SolveTimePublic = time,
                    PendingRestricted = s.PendingRestricted - 1,
                    SubmissionRestricted = s.SubmissionRestricted + 1,
                    IsCorrectRestricted = true,
                    SolveTimeRestricted = time,
                    FirstToSolve = firstBlood,
                };
            }

            int count = await db.Score(args)
                .Where(s => s.PendingRestricted > 0)
                .BatchUpdateAsync(scp);

            if (count == 0) return;

            // update rank
            var tries = await db.Score(args)
                .Select(s => new { t = s.SolveTimeRestricted, s = s.SubmissionRestricted })
                .SingleOrDefaultAsync();

            int penalty = (tries.s - 1) * 20;
            penalty += tries.t < 0 ? -(((int)(-tries.t)) / 60) : ((int)(tries.t)) / 60;

            if (args.Frozen)
            {
                await db.RankCache.MergeAsync(
                    sourceTable: new[] { new { args.ContestId, args.TeamId, penalty } },
                    targetKey: rc => new { rc.ContestId, rc.TeamId },
                    sourceKey: rc => new { rc.ContestId, rc.TeamId },
                    delete: false,

                    updateExpression: (r, s) => new RankCache
                    {
                        PointsRestricted = r.PointsRestricted + 1,
                        TotalTimeRestricted = r.TotalTimeRestricted + s.penalty,
                    },

                    insertExpression: s => new RankCache
                    {
                        PointsRestricted = 1,
                        TotalTimeRestricted = s.penalty,
                        TeamId = s.TeamId,
                        ContestId = s.ContestId,
                    });
            }
            else
            {
                await db.RankCache.MergeAsync(
                    sourceTable: new[] { new { args.ContestId, args.TeamId, penalty } },
                    targetKey: rc => new { rc.ContestId, rc.TeamId },
                    sourceKey: rc => new { rc.ContestId, rc.TeamId },
                    delete: false,

                    updateExpression: (r, s) => new RankCache
                    {
                        PointsRestricted = r.PointsRestricted + 1,
                        TotalTimeRestricted = r.TotalTimeRestricted + s.penalty,
                        PointsPublic = r.PointsPublic + 1,
                        TotalTimePublic = r.TotalTimePublic + s.penalty,
                    },

                    insertExpression: s => new RankCache
                    {
                        PointsRestricted = 1,
                        TotalTimeRestricted = s.penalty,
                        PointsPublic = 1,
                        TotalTimePublic = s.penalty,
                        TeamId = s.TeamId,
                        ContestId = s.ContestId,
                    });
            }

            if (!args.Frozen && args.Balloon)
            {
                await db.Balloon.MergeAsync(
                    sourceTable: new[] { new { args.SubmissionId } },
                    targetKey: s => s.SubmissionId,
                    sourceKey: s => s.SubmissionId,
                    updateExpression: null,
                    insertExpression: s => new Balloon { SubmissionId = s.SubmissionId },
                    delete: false);
            }
        }


        public Task CompileError(ScoreboardContext db, ScoreboardEventArgs args)
        {
            Expression<Func<ScoreCache, ScoreCache>> scp;

            if (args.Frozen)
            {
                scp = s => new ScoreCache
                {
                    PendingRestricted = s.PendingRestricted - 1,
                };
            }
            else
            {
                scp = s => new ScoreCache
                {
                    PendingPublic = s.PendingPublic - 1,
                    PendingRestricted = s.PendingRestricted - 1,
                };
            }

            return db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .Where(s => s.PendingRestricted > 0)
                .BatchUpdateAsync(scp);
        }


        public Task Pending(ScoreboardContext db, ScoreboardEventArgs args)
        {
            return db.ScoreCache.MergeAsync(
                sourceTable: new[] { new { args.ContestId, args.TeamId, args.ProblemId } },
                targetKey: sc => new { sc.ContestId, sc.TeamId, sc.ProblemId },
                sourceKey: sc => new { sc.ContestId, sc.TeamId, sc.ProblemId },

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
                },

                delete: false);
        }


        public Task Reject(ScoreboardContext db, ScoreboardEventArgs args)
        {
            Expression<Func<ScoreCache, ScoreCache>> scp;

            if (args.Frozen)
            {
                scp = s => new ScoreCache
                {
                    PendingRestricted = s.PendingRestricted - 1,
                    SubmissionRestricted = s.SubmissionRestricted + 1,
                };
            }
            else
            {
                scp = s => new ScoreCache
                {
                    PendingPublic = s.PendingPublic - 1,
                    SubmissionPublic = s.SubmissionPublic + 1,
                    PendingRestricted = s.PendingRestricted - 1,
                    SubmissionRestricted = s.SubmissionRestricted + 1,
                };
            }

            return db.Score(args.ContestId, args.ProblemId, args.TeamId)
                .Where(s => s.PendingRestricted > 0)
                .BatchUpdateAsync(scp);
        }


        public async Task RefreshCache(ScoreboardContext db, ScoreboardEventArgs args)
        {
            int cid = args.ContestId;

            var query =
                from s in db.Submissions
                where s.ContestId == cid
                join t in db.Teams on new { s.ContestId, TeamId = s.Author } equals new { t.ContestId, t.TeamId }
                join tc in db.Categories on t.CategoryId equals tc.CategoryId
                join j in db.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                orderby s.Time ascending
                select new { s.SubmissionId, t.TeamId, tc.SortOrder, s.ProblemId, s.Time, j.Status };
            var results = await query.ToListAsync();

            var rcc = new Dictionary<int, RankCache>();
            var scc = new Dictionary<(int, int), ScoreCache>();
            var fb = new HashSet<(int, int)>();
            var oks = new List<int>();
            var endTime = args.EndTime < args.SubmitTime ? args.EndTime : args.SubmitTime;

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

                    sc.IsCorrectRestricted = true;
                    sc.SolveTimeRestricted = (s.Time - args.ContestTime).TotalSeconds;
                    oks.Add(s.SubmissionId);

                    int penalty = (sc.SubmissionRestricted - 1) * 20;
                    penalty += sc.SolveTimeRestricted < 0 ? -(((int)(-sc.SolveTimeRestricted)) / 60) : ((int)(sc.SolveTimeRestricted)) / 60;
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

                    if (s.Status == Verdict.Accepted)
                    {
                        var rc = rcc[s.TeamId];
                        rc.PointsPublic = rc.PointsRestricted;
                        rc.TotalTimePublic = rc.TotalTimeRestricted;
                    }
                }
            }

            await db.ScoreCache
                .Where(t => t.ContestId == cid)
                .BatchDeleteAsync();
            
            await db.RankCache
                .Where(t => t.ContestId == cid)
                .BatchDeleteAsync();

            db.RankCache.AddRange(rcc.Values);
            db.ScoreCache.AddRange(scc.Values);

            if (args.Balloon)
            {
                var createdBalloons = await db.Balloon
                    .Select(b => b.SubmissionId)
                    .ToListAsync();
                var balloons = oks.Except(createdBalloons)
                    .Select(s => new Balloon { SubmissionId = s })
                    .ToList();
                db.Balloon.AddRange(balloons);
            }

            await db.SaveChangesAsync();
        }
    }
}
