using EFCore.BulkExtensions;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public class ICPCScoreboard : IScoreboard
    {
        public IEnumerable<BoardQuery> SortByRule(IEnumerable<BoardQuery> src, bool isPublic)
        {
            if (isPublic)
            {
                return src.OrderByDescending(a => a.Rank.PointsPublic)
                    .ThenBy(a => a.Rank.TotalTimePublic);
            }
            else
            {
                return src.OrderByDescending(a => a.Rank.PointsRestricted)
                    .ThenBy(a => a.Rank.TotalTimeRestricted);
            }
        }

        public async Task UpdateScoreboardPendingAsync(
            DbContext db, int cid, int teamid, int probid, bool freeze)
        {
            var sc = await db.Set<ScoreCache>()
                .Where(s => s.ContestId == cid && s.ProblemId == probid && s.TeamId == teamid)
                .CountAsync();

            if (sc == 0)
            {
                await db.InsertAsync(new ScoreCache
                {
                    ContestId = cid,
                    TeamId = teamid,
                    ProblemId = probid,
                    PendingPublic = 1,
                    PendingRestricted = 1,
                });
            }
            else
            {
                await db.Set<ScoreCache>()
                    .Where(s => s.ContestId == cid && s.ProblemId == probid && s.TeamId == teamid && !s.IsCorrectRestricted)
                    .BatchUpdateAsync(s => new ScoreCache
                    {
                        PendingPublic = s.PendingPublic + 1,
                        PendingRestricted = s.PendingRestricted + 1,
                    });
            }
        }

        public Task UpdateScoreboardCompileErrorAsync(
            DbContext db, int cid, int teamid, int probid, bool freeze)
        {
            Expression<Func<ScoreCache, ScoreCache>> scp;

            if (freeze)
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

            return db.Set<ScoreCache>()
                .Where(s => s.ContestId == cid && s.ProblemId == probid && s.TeamId == teamid)
                .BatchUpdateAsync(scp);
        }

        public Task UpdateScoreboardRejectedAsync(
            DbContext db, int cid, int teamid, int probid, bool freeze)
        {
            Expression<Func<ScoreCache, ScoreCache>> scp;

            if (freeze)
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

            return db.Set<ScoreCache>()
                .Where(s => s.ContestId == cid && s.ProblemId == probid && s.TeamId == teamid && !s.IsCorrectRestricted)
                .BatchUpdateAsync(scp);
        }

        public async Task UpdateScoreboardCorrectAsync(
            DbContext db, int submitid, int cid, int teamid, int probid, double time, bool freeze, bool useBalloon, bool opfb = true)
        {
            Expression<Func<ScoreCache, ScoreCache>> scp;

            if (opfb)
            {
                // first blood
                var sortOrderQuery =
                    from t in db.Set<Team>()
                    join c in db.Set<TeamCategory>() on t.CategoryId equals c.CategoryId
                    select c.SortOrder;
                var so = await sortOrderQuery.FirstAsync();

                var fbQuery =
                    from sc in db.Set<ScoreCache>()
                    where sc.ContestId == cid && sc.ProblemId == probid && sc.FirstToSolve
                    join t in db.Set<Team>() on sc.TeamId equals t.TeamId
                    join c in db.Set<TeamCategory>() on t.CategoryId equals c.CategoryId
                    where c.SortOrder == so
                    select new { tid = sc.TeamId };
                var it = await fbQuery.CountAsync();
                opfb = it == 0;
            }

            if (freeze)
            {
                scp = s => new ScoreCache
                {
                    PendingRestricted = s.PendingRestricted - 1,
                    SubmissionRestricted = s.SubmissionRestricted + 1,
                    IsCorrectRestricted = true,
                    SolveTimeRestricted = time,
                    FirstToSolve = opfb,
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
                    FirstToSolve = opfb,
                };
            }

            int count = await db.Set<ScoreCache>()
                .Where(s => s.ContestId == cid && s.ProblemId == probid && s.TeamId == teamid && !s.IsCorrectRestricted)
                .BatchUpdateAsync(scp);

            if (count == 0) return;

            // update rank
            var tries = await db.Set<ScoreCache>()
                .Where(s => s.ContestId == cid && s.ProblemId == probid && s.TeamId == teamid)
                .Select(s => new { t = s.SolveTimeRestricted, s = s.SubmissionRestricted })
                .SingleOrDefaultAsync();

            Expression<Func<RankCache, RankCache>> rcp;

            int penalty = (tries.s - 1) * 20;
            penalty += tries.t < 0 ? -(((int)(-tries.t)) / 60) : ((int)(tries.t)) / 60;

            if (freeze)
            {
                rcp = r => new RankCache
                {
                    PointsRestricted = r.PointsRestricted + 1,
                    TotalTimeRestricted = r.TotalTimeRestricted + penalty,
                };
            }
            else
            {
                rcp = r => new RankCache
                {
                    PointsRestricted = r.PointsRestricted + 1,
                    TotalTimeRestricted = r.TotalTimeRestricted + penalty,
                    PointsPublic = r.PointsPublic + 1,
                    TotalTimePublic = r.TotalTimePublic + penalty,
                };
            }

            int cnt = await db.Set<RankCache>()
                .Where(r => r.ContestId == cid && r.TeamId == teamid)
                .BatchUpdateAsync(rcp);

            if (cnt == 0)
            {
                var rc = rcp.Compile().Invoke(new RankCache());
                rc.ContestId = cid;
                rc.TeamId = teamid;
                await db.InsertAsync(rc);
            }

            if (!freeze && useBalloon)
            {
                await db.InsertAsync(new Data.Ext.Balloon { SubmissionId = submitid });
            }
        }

        public async Task UpdateScoreboardBundleAsync(
            DbContext db, Contest c, IEnumerable<(int sid, int team, int so, int prob, DateTimeOffset time, Verdict? v)> results)
        {
            var rcc = new Dictionary<int, RankCache>();
            var scc = new Dictionary<(int, int), ScoreCache>();
            var fb = new HashSet<(int, int)>();
            var oks = new List<int>();

            foreach (var (sid, team, so, prob, time, v) in results)
            {
                var stat = c.GetState(time);
                if (stat >= ContestState.Ended) continue;
                if (!scc.ContainsKey((team, prob)))
                    scc.Add((team, prob), new ScoreCache { ContestId = c.ContestId, TeamId = team, ProblemId = prob });
                var sc = scc[(team, prob)];
                if (sc.IsCorrectRestricted || v == Verdict.CompileError) continue;

                if (v == Verdict.Running || v == Verdict.Pending)
                {
                    sc.PendingPublic++;
                    sc.PendingRestricted++;
                    continue;
                }

                sc.SubmissionRestricted++;

                if (v == Verdict.Accepted)
                {
                    if (!rcc.ContainsKey(team))
                        rcc.Add(team, new RankCache { ContestId = c.ContestId, TeamId = team });
                    var rc = rcc[team];

                    sc.IsCorrectRestricted = true;
                    sc.SolveTimeRestricted = (time - c.StartTime)?.TotalSeconds ?? 0;
                    oks.Add(sid);

                    int penalty = (sc.SubmissionRestricted - 1) * 20;
                    penalty += sc.SolveTimeRestricted < 0 ? -(((int)(-sc.SolveTimeRestricted)) / 60) : ((int)(sc.SolveTimeRestricted)) / 60;
                    rc.PointsRestricted++;
                    rc.TotalTimeRestricted += penalty;

                    if (!fb.Contains((prob, so)))
                    {
                        fb.Add((prob, so));
                        sc.FirstToSolve = true;
                    }
                }

                if (stat == ContestState.Frozen)
                {
                    sc.PendingPublic++;
                }
                else
                {
                    sc.SolveTimePublic = sc.SolveTimeRestricted;
                    sc.SubmissionPublic = sc.SubmissionRestricted;
                    sc.IsCorrectPublic = sc.IsCorrectRestricted;

                    if (v == Verdict.Accepted)
                    {
                        var rc = rcc[team];
                        rc.PointsPublic = rc.PointsRestricted;
                        rc.TotalTimePublic = rc.TotalTimeRestricted;
                    }
                }
            }

            int cid = c.ContestId;
            await db.Set<ScoreCache>()
                .Where(t => t.ContestId == cid)
                .BatchDeleteAsync();
            await db.Set<RankCache>()
                .Where(t => t.ContestId == cid)
                .BatchDeleteAsync();
            await db.BulkInsertAsync(rcc.Values.ToList());
            await db.BulkInsertAsync(scc.Values.ToList());

            if (c.BalloonAvaliable)
            {
                var createdBalloons = await db
                    .Set<Data.Ext.Balloon>()
                    .Select(b => b.SubmissionId)
                    .ToListAsync();

                var inserted = oks
                    .Except(createdBalloons)
                    .ToList();
                var balloons = inserted
                    .Select(s => new Data.Ext.Balloon { SubmissionId = s })
                    .ToList();
                await db.BulkInsertAsync(balloons);
            }
        }
    }
}
