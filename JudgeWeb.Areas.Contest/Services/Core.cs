using EFCore.BulkExtensions;
using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Areas.Contest.Services;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TContest = JudgeWeb.Data.Contest;

[assembly: Inject(typeof(ContestManager))]
namespace JudgeWeb.Areas.Contest.Services
{
    public partial class ContestManager
    {
        private static readonly AsyncLock _locker = new AsyncLock();

        public string AuditlogUserName { get; set; }

        private AppDbContext DbContext { get; }

        private IMemoryCache Cache => ContestCache._cache;

        public IQueryable<Clarification> Clarifications => DbContext.Clarifications;
        public IQueryable<Team> Teams => DbContext.Teams;
        public IQueryable<TeamCategory> TeamCategories => DbContext.TeamCategories;
        public IQueryable<TeamAffiliation> TeamAffiliations => DbContext.TeamAffiliations;
        public IQueryable<AuditLog> Auditlogs => DbContext.AuditLogs;
        public IQueryable<TContest> Contests => DbContext.Contests;


        public ContestManager(AppDbContext db)
        {
            DbContext = db;
        }

        private void InternalLog(AuditLog log)
        {
            log.LogId = 0;
            log.Time = DateTimeOffset.Now;
            log.UserName = AuditlogUserName ?? throw new ApplicationException("Please make sure logname exists.");
            DbContext.AuditLogs.Add(log);
        }

        public Task<TContest> GetContestAsync(int cid) =>
            DbContext.Contests
                .Where(c => c.ContestId == cid)
                .CachedSingleOrDefaultAsync($"`c{cid}`info", TimeSpan.FromMinutes(5));

        public Task UpdateContestAsync(int cid, Expression<Func<TContest, TContest>> update) =>
            DbContext.Contests
                .Where(c => c.ContestId == cid)
                .BatchUpdateAsync(update)
                .ContinueWith(t => Cache.Remove($"`c{cid}`info"));

        public Task<Dictionary<int, Language>> GetLanguagesAsync(int cid) =>
            DbContext.Languages
                .CachedToDictionaryAsync(
                    keySelector: k => k.LangId,
                    tag: $"`c{cid}`langs",
                    timeSpan: TimeSpan.FromMinutes(10));

        public Task<ContestProblem[]> GetProblemsAsync(int cid)
        {
            return Cache.GetOrCreateAsync($"`c{cid}`probs", async entry =>
            {
                var query1 =
                    from cp in DbContext.ContestProblem
                    where cp.ContestId == cid
                    orderby cp.Rank ascending
                    join p in DbContext.Problems on cp.ProblemId equals p.ProblemId
                    select new ContestProblem(cp, p.Title, p.TimeLimit, p.MemoryLimit, p.CombinedRunCompare);

                var query2 =
                    from cp in DbContext.ContestProblem
                    where cp.ContestId == cid
                    join t in DbContext.Testcases on cp.ProblemId equals t.ProblemId
                    group t by cp.ProblemId into g
                    select new { g.Key, Count = g.Count() };

                var result = await query1.ToArrayAsync();
                var result2 = await query2.ToDictionaryAsync(k => k.Key, v => v.Count);
                foreach (var item in result)
                    item.TestcaseCount = result2.GetValueOrDefault(item.ProblemId);

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return result;
            });
        }

        public async Task<IEnumerable<SubmissionViewModel>> ListSubmissionsByJuryAsync(
            int cid, int? teamid = null, bool all = true)
        {
            var probs = await GetProblemsAsync(cid);
            var langs = await GetLanguagesAsync(cid);
            var teamNames = await GetTeamNameAsync(cid);

            return await Cache.GetOrCreateAsync($"`c{cid}`t{teamid ?? -1}`sub_jury`{all}", async entry =>
            {
                var submissions = DbContext.Submissions
                    .Where(s => s.ContestId == cid);
                if (teamid.HasValue)
                    submissions = submissions.Where(s => s.Author == teamid);
                submissions = submissions.OrderByDescending(s => s.SubmissionId);
                if (!all) submissions = submissions.Take(50);
                else submissions = submissions.Take(10000);

                var query =
                    from s in submissions
                    join j in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                    join d in DbContext.Details on j.JudgingId equals d.JudgingId into dd
                    from d in dd.DefaultIfEmpty()
                    select new { s.SubmissionId, s.Time, j.Status, s.ProblemId, s.Author, s.Language, d = (Verdict?)d.Status, s.Ip };
                var result = await query.ToListAsync();

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);

                return result
                    .GroupBy(a => new { a.Status, a.SubmissionId, a.Time, a.ProblemId, a.Author, a.Language, a.Ip }, a => a.d)
                    .OrderByDescending(g => g.Key.SubmissionId)
                    .Select(g => new SubmissionViewModel
                    {
                        Language = langs.GetValueOrDefault(g.Key.Language),
                        TeamId = g.Key.Author,
                        TeamName = teamNames.GetValueOrDefault(g.Key.Author),
                        Problem = probs.FirstOrDefault(cp => cp.ProblemId == g.Key.ProblemId),
                        SubmissionId = g.Key.SubmissionId,
                        Verdict = g.Key.Status,
                        Time = g.Key.Time,
                        CompilerOutput = g.Key.Ip,
                        Details = g.Where(v => v.HasValue).Select(v => v.Value).ToArray()
                    })
                    .ToList();
            });
        }

        public async Task<ContestResult> ChangeStateAsync(
            TContest contest, string target, DateTimeOffset now)
        {
            int cid = contest.ContestId;
            var state = contest.GetState(now);

            if (target == "startnow")
            {
                if (!contest.EndTime.HasValue)
                    return ContestResult.FromError("No end time specified.");
                now += TimeSpan.FromSeconds(30);
                DateTimeOffset old;

                if (contest.StartTime.HasValue)
                {
                    // from scheduled to start
                    if (contest.StartTime.Value < now)
                        return ContestResult.FromError("Error starting contest for the remaining time is less than 30 secs.");
                    old = contest.StartTime.Value;
                }
                else
                {
                    // from delay to start
                    old = DateTimeOffset.UnixEpoch;
                }

                contest.StartTime = now;
                contest.EndTime = now + (contest.EndTime.Value - old);
                if (contest.FreezeTime.HasValue)
                    contest.FreezeTime = now + (contest.FreezeTime.Value - old);
                if (contest.UnfreezeTime.HasValue)
                    contest.UnfreezeTime = now + (contest.UnfreezeTime.Value - old);
            }
            else if (target == "freeze")
            {
                if (state != ContestState.Started)
                    return ContestResult.FromError("Contest is not started.");
                contest.FreezeTime = now;
            }
            else if (target == "endnow")
            {
                if (state != ContestState.Started && state != ContestState.Frozen)
                    return ContestResult.FromError("Error contest has not started or has ended.");
                contest.EndTime = now;

                if (contest.FreezeTime.HasValue && contest.FreezeTime.Value > now)
                    contest.FreezeTime = now;
            }
            else if (target == "unfreeze")
            {
                if (state != ContestState.Ended)
                    return ContestResult.FromError("Contest has not ended.");
                contest.UnfreezeTime = now;
            }
            else if (target == "delay")
            {
                if (state != ContestState.ScheduledToStart)
                    return ContestResult.FromError("Contest has been started.");

                var old = contest.StartTime.Value;
                contest.StartTime = null;
                if (contest.EndTime.HasValue)
                    contest.EndTime = DateTimeOffset.UnixEpoch + (contest.EndTime.Value - old);
                if (contest.FreezeTime.HasValue)
                    contest.FreezeTime = DateTimeOffset.UnixEpoch + (contest.FreezeTime.Value - old);
                if (contest.UnfreezeTime.HasValue)
                    contest.UnfreezeTime = DateTimeOffset.UnixEpoch + (contest.UnfreezeTime.Value - old);
            }

            DbContext.Contests.Update(contest);
            InternalLog(new AuditLog
            {
                Comment = "modified time",
                ContestId = cid,
                EntityId = cid,
                Resolved = true,
                Type = AuditLog.TargetType.Contest,
            });

            await DbContext.SaveChangesAsync();
            Cache.Remove($"`c{cid}`info");
            return ContestResult.FromOk("Contest state changed.");
        }
    }
}
