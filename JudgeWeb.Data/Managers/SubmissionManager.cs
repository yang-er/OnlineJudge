using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace JudgeWeb.Data
{
    public class SubmissionManager
    {
        private static IMemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions { Clock = new SystemClock() });

        protected AppDbContext DbContext { get; }

        public SubmissionManager(AppDbContext adbc)
        {
            DbContext = adbc;
        }

        public IQueryable<Submission> Submissions => DbContext.Submissions;

        public IQueryable<Judging> Judgings => DbContext.Judgings;

        public IQueryable<Language> Languages => DbContext.Languages;


        public async Task<IEnumerable<(Detail, Testcase)>> GetDetailsAsync(int jid, int pid)
        {
            var query =
                from t in DbContext.Testcases
                where t.ProblemId == pid
                orderby t.Rank ascending
                join d in DbContext.Details
                    on new { t.TestcaseId, JudgingId = jid }
                    equals new { d.TestcaseId, d.JudgingId }
                    into dd
                from d in dd.DefaultIfEmpty()
                select new { d, t };

            var result = await query.ToListAsync();
            return result.Select(a => (a.d, a.t));
        }


        public Task<IEnumerable<SubmissionStatistics>> StatisticsByUserAsync(int uid)
        {
            return Cache.GetOrCreateAsync<IEnumerable<SubmissionStatistics>>($"SubStatU{uid}", async (t) =>
            {
                var item = await DbContext
                    .Set<SubmissionStatistics>()
                    .FromSqlRaw(
                        "SELECT COUNT(*) AS [TotalSubmission], [a].[PublicId] AS [ProblemId], " +
                            "SUM(CASE WHEN [j].[Status] = 11 THEN 1 ELSE 0 END) AS [AcceptedSubmission], " +
                            "@__uid AS [Author], 0 AS [ContestId] " +
                        "FROM [Submissions] AS [s] " +
                        "INNER JOIN [Archives] AS [a] ON [s].[ProblemId] = [a].[ProblemId] " +
                        "INNER JOIN [Judgings] AS [j] ON ([s].[SubmissionId] = [j].[SubmissionId]) AND ([j].[Active] = 1) " +
                        "WHERE ([s].[ContestId] = 0) AND ([s].[Author] = @__uid)" +
                        "GROUP BY [a].[PublicId]", new SqlParameter("__uid", uid))
                    .ToListAsync();
                item.Sort((a, b) => a.ProblemId.CompareTo(b.ProblemId));
                t.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return item;
            });
        }


        public async Task<IEnumerable<(Submission, Judging)>> EnumerateAsync(
            Expression<Func<Submission, bool>> conditions, int count)
        {
            var query = await DbContext.Submissions
                .Where(conditions)
                .OrderBy(a => a.SubmissionId)
                .Join(
                    inner: DbContext.Judgings,
                    outerKeySelector: s => new { s.SubmissionId, Active = true },
                    innerKeySelector: g => new { g.SubmissionId, g.Active },
                    resultSelector: (s, g) => new { s, g })
                .Take(count)
                .ToListAsync();

            return query.Select(a => (a.s, a.g));
        }


        public Task<Submission> FindAsync(int sid, int? pid = null, int? cid = null)
        {
            var query = Submissions.Where(s => s.SubmissionId == sid);
            if (pid.HasValue) query = query.Where(s => s.ProblemId == pid.Value);
            if (cid.HasValue) query = query.Where(s => s.ContestId == cid.Value);
            return query.FirstOrDefaultAsync();
        }


        public Task UpdateAsync(Submission sub)
        {
            DbContext.Submissions.Update(sub);
            return DbContext.SaveChangesAsync();
        }


        public async Task RejudgeAsync(Submission sub,
            Rejudge rejudge = null, bool fullTest = false,
            Action<Judging> oldSolve = null)
        {
            if (sub.ExpectedResult != null) fullTest = true;

            if (rejudge == null)
            {
                var currentJudging = await DbContext.Judgings
                    .Where(j => j.SubmissionId == sub.SubmissionId && j.Active)
                    .SingleAsync();

                currentJudging.Active = false;
                fullTest = fullTest || currentJudging.FullTest;
                oldSolve?.Invoke(currentJudging);
                DbContext.Judgings.Update(currentJudging);

                DbContext.Judgings.Add(new Judging
                {
                    SubmissionId = sub.SubmissionId,
                    FullTest = fullTest,
                    Active = true,
                    Status = Verdict.Pending,
                });

                await DbContext.SaveChangesAsync();
            }
            else
            {
                DbContext.Judgings.Add(new Judging
                {
                    SubmissionId = sub.SubmissionId,
                    FullTest = fullTest,
                    Active = true,
                    Status = Verdict.Pending,
                    RejudgeId = rejudge.RejudgeId,
                });

                sub.RejudgeId = rejudge.RejudgeId;
                DbContext.Submissions.Update(sub);
                await DbContext.SaveChangesAsync();
            }
        }


        public async Task<Submission> CreateAsync(
            string code, Language langid, int probid, Contest cid, int uid,
            IPAddress ipAddr, string via, string username, Verdict? expected = null, DateTimeOffset? time = null)
        {
            var s = DbContext.Submissions.Add(new Submission
            {
                Author = uid,
                CodeLength = code.Length,
                ContestId = cid?.ContestId ?? 0,
                Ip = ipAddr.ToString(),
                Language = langid.Id,
                ProblemId = probid,
                Time = time ?? DateTimeOffset.Now,
                SourceCode = code.ToBase64(),
                ExpectedResult = expected,
            });

            await DbContext.SaveChangesAsync();

            DbContext.Auditlogs.Add(new Auditlog
            {
                ContestId = cid?.ContestId,
                Time = s.Entity.Time.DateTime,
                DataId = $"{s.Entity.SubmissionId}",
                DataType = AuditlogType.Submission,
                Action = "added",
                ExtraInfo = $"via {via}",
                UserName = username,
            });

            bool fullTest = expected.HasValue || ((cid?.RankingStrategy ?? 0) == 2);

            DbContext.Judgings.Add(new Judging
            {
                SubmissionId = s.Entity.SubmissionId,
                FullTest = fullTest,
                Active = true,
                Status = Verdict.Pending,
            });

            /*
            if (cid != null)
            {
                var cs = new Api.ContestSubmission(
                    cid: cid.ContestId,
                    langid: langid.Id,
                    submitid: s.Entity.SubmissionId,
                    probid: probid,
                    teamid: uid,
                    time: s.Entity.Time,
                    diff: (s.Entity.Time - cid.StartTime) ?? TimeSpan.Zero);

                DbContext.Events.Add(cs.ToEvent("create", cid.ContestId));
            }*/

            await DbContext.SaveChangesAsync();
            return s.Entity;
        }
    }
}
