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
    public class OIRank : IRankingStrategy
    {
        public IEnumerable<BoardQuery> SortByRule(IEnumerable<BoardQuery> source, bool isPublic)
            => isPublic
                ? source.OrderByDescending(a => a.Rank.PointsPublic)
                    .ThenBy(a => a.Rank.TotalTimePublic)
                : source.OrderByDescending(a => a.Rank.PointsRestricted)
                    .ThenBy(a => a.Rank.TotalTimeRestricted);


        public Task Accept(AppDbContext db, ScoreboardEventArgs args)
        {
            return Reject(db, args);
        }


        public Task CompileError(AppDbContext db, ScoreboardEventArgs args)
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

            return db.Score(args)
                .BatchUpdateAsync(scp);
        }


        public async Task Pending(AppDbContext db, ScoreboardEventArgs args)
        {
            var sc = await db.Score(args).CountAsync();

            if (sc == 0)
            {
                await db.InsertAsync(new ScoreCache
                {
                    ContestId = args.ContestId,
                    TeamId = args.TeamId,
                    ProblemId = args.ProblemId,
                    PendingPublic = 1,
                    PendingRestricted = 1,
                });
            }
            else
            {
                await db.Score(args)
                    .BatchUpdateAsync(s => new ScoreCache
                    {
                        PendingPublic = s.PendingPublic + 1,
                        PendingRestricted = s.PendingRestricted + 1,
                    });
            }
        }


        public async Task Reject(AppDbContext db, ScoreboardEventArgs args)
        {
            var sr = await (from t in db.Teams
                            where t.TeamId == args.TeamId && t.ContestId == args.ContestId
                            join scc in db.ScoreCache on new { t.ContestId, t.TeamId, args.ProblemId } equals new { scc.ContestId, scc.TeamId, scc.ProblemId }
                            into sccs from scc in sccs.DefaultIfEmpty()
                            join rcc in db.RankCache on new { t.ContestId, t.TeamId } equals new { rcc.ContestId, rcc.TeamId }
                            into rccs from rcc in rccs.DefaultIfEmpty()
                            select new { scc, rcc }).SingleOrDefaultAsync();
            var sc = sr.scc;
            var rc = sr.rcc;

            if (sc == null)
            {
                sc = db.ScoreCache.Add(new ScoreCache
                {
                    ContestId = args.ContestId,
                    ProblemId = args.ProblemId,
                    TeamId = args.TeamId
                }).Entity;
            }
            else
            {
                db.ScoreCache.Update(sc);
            }

            if (rc == null)
            {
                rc = db.RankCache.Add(new RankCache
                {
                    TeamId = args.TeamId,
                    ContestId = args.ContestId,
                }).Entity;
            }
            else
            {
                db.RankCache.Update(rc);
            }

            // first blood
            rc.PointsRestricted -= (int)sc.SolveTimeRestricted / 60;
            var allScore = await (from t in db.Testcases
                                  where t.ProblemId == args.ProblemId
                                  select t.Point).SumAsync();
            sc.FirstToSolve = allScore == args.TotalScore;
            sc.SolveTimeRestricted = args.TotalScore * 60;
            sc.IsCorrectRestricted = args.TotalScore > 0;
            sc.PendingRestricted--;
            sc.SubmissionRestricted++;
            rc.PointsRestricted += args.TotalScore;
            rc.TotalTimeRestricted = (int)(args.SubmitTime - args.ContestTime).TotalMinutes;

            if (!args.Frozen)
            {
                sc.SolveTimePublic = sc.SolveTimeRestricted;
                sc.SubmissionPublic = sc.SubmissionRestricted;
                sc.IsCorrectPublic = sc.IsCorrectRestricted;
                sc.PendingPublic = sc.PendingRestricted;
                rc.PointsPublic = rc.PointsRestricted;
                rc.TotalTimePublic = rc.TotalTimeRestricted;
            }

            await db.SaveChangesAsync();
        }


        public async Task RefreshCache(AppDbContext db, ScoreboardEventArgs args)
        {
            int cid = args.ContestId;

            var query =
                from s in db.Submissions
                where s.ContestId == cid
                join j in db.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                orderby s.Time ascending
                select new { s.SubmissionId, TeamId = s.Author, s.ProblemId, s.Time, j.Status, j.TotalScore };
            var results = await query.ToListAsync();

            var scrs =
                from cp in db.ContestProblem
                where cp.ContestId == cid
                join t in db.Testcases on cp.ProblemId equals t.ProblemId
                select new { cp.ProblemId, t.Point };
            var q = await scrs.ToListAsync();
            var full = q.GroupBy(a => a.ProblemId).ToDictionary(a => a.Key, a => a.Sum(aa => aa.Point));

            var rcc = new Dictionary<int, RankCache>();
            var scc = new Dictionary<(int, int), ScoreCache>();
            var lastop1 = new Dictionary<int, int>();
            var lastop2 = new Dictionary<int, int>();
            var endTime = args.EndTime < args.SubmitTime ? args.EndTime : args.SubmitTime;

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
                sc.IsCorrectRestricted = s.TotalScore != 0;
                sc.SolveTimeRestricted = s.TotalScore.Value * 60;
                sc.FirstToSolve = s.TotalScore == full.GetValueOrDefault(s.ProblemId);
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
                    item.PointsPublic += (int)(rr.SolveTimePublic / 60.0);
                    item.PointsRestricted += (int)(rr.SolveTimeRestricted / 60.0);
                    item.TotalTimePublic = lastop1.GetValueOrDefault(r.Key);
                    item.TotalTimeRestricted = lastop2.GetValueOrDefault(r.Key);
                }

                rcc.Add(r.Key, item);
            }

            await db.ScoreCache
                .Where(t => t.ContestId == cid)
                .BatchDeleteAsync();
            await db.RankCache
                .Where(t => t.ContestId == cid)
                .BatchDeleteAsync();
            await db.BulkInsertAsync(rcc.Values.ToList());
            await db.BulkInsertAsync(scc.Values.ToList());
        }
    }
}
