using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Judgements
{
    public class EntityFrameworkCoreSubmissionRepository<TContext>
        : EntityFrameworkCoreSubmissionRepository
        where TContext : DbContext
    {
        public EntityFrameworkCoreSubmissionRepository(TContext context, IRunFileRepository files)
            : base(context, files)
        {
        }
    }

    public class EntityFrameworkCoreSubmissionRepository : ISubmissionRepository
    {
        public DbContext Context { get; }
        public IMutableFileProvider RunFiles { get; }

        public EntityFrameworkCoreSubmissionRepository(DbContext context, IMutableFileProvider files)
        {
            Context = context;
            RunFiles = files;
        }

        DbSet<Submission> Submissions => Context.Set<Submission>();
        DbSet<Judging> Judgings => Context.Set<Judging>();

        public async Task<Submission> CreateAsync(
            string code,
            string langid,
            int probid,
            int? contest,
            int uid,
            IPAddress ipAddr,
            string via,
            string username,
            Verdict? expected = null,
            DateTimeOffset? time = null,
            bool fullJudge = false)
        {
            var s = Submissions.Add(new Submission
            {
                Author = uid,
                CodeLength = code.Length,
                ContestId = contest ?? 0,
                Ip = ipAddr.ToString(),
                Language = langid,
                ProblemId = probid,
                Time = time ?? DateTimeOffset.Now,
                SourceCode = code.ToBase64(),
                ExpectedResult = expected,
            });

            await Context.SaveChangesAsync();

            Context.Add(new Auditlog
            {
                ContestId = contest,
                Time = s.Entity.Time.DateTime,
                DataId = $"{s.Entity.SubmissionId}",
                DataType = AuditlogType.Submission,
                Action = "added",
                ExtraInfo = $"via {via}",
                UserName = username,
            });

            bool fullTest = fullJudge || expected.HasValue;

            Judgings.Add(new Judging
            {
                SubmissionId = s.Entity.SubmissionId,
                FullTest = fullTest,
                Active = true,
                Status = Verdict.Pending,
            });

            await Context.SaveChangesAsync();
            return s.Entity;
        }

        public Task<Submission> FindAsync(
            int sid,
            bool includeJudgings = false)
        {
            var query = Submissions
                .Where(s => s.SubmissionId == sid);
            if (includeJudgings)
                query = query.Include(s => s.Judgings);
            return query.SingleOrDefaultAsync();
        }

        public Task<Submission> FindByJudgingAsync(int jid)
        {
            return (from j in Judgings
                    where j.JudgingId == jid
                    join s in Submissions on j.SubmissionId equals s.SubmissionId
                    select s).SingleOrDefaultAsync();
        }

        public async Task<string> GetAuthorNameAsync(int sid)
        {
            var result = await GetAuthorNamesAsync(sid);
            return result.Values.SingleOrDefault();
        }

        public Task<Dictionary<int, string>> GetAuthorNamesAsync(params int[] sids)
        {
            var query =
                from s in Submissions
                where sids.Contains(s.SubmissionId)
                join u in Context.Set<User>() on new { s.ContestId, s.Author } equals new { ContestId = 0, Author = u.Id }
                into uu from u in uu.DefaultIfEmpty()
                join t in Context.Set<Team>() on new { s.ContestId, s.Author } equals new { t.ContestId, Author = t.TeamId }
                into tt from t in tt.DefaultIfEmpty()
                select new { s.SubmissionId, s.ContestId, s.Author, u.UserName, t.TeamName };

            return query.ToDictionaryAsync(
                keySelector: r => r.SubmissionId,
                elementSelector: r => r.ContestId == 0
                    ? $"{r.UserName ?? "SYSTEM"} (u{r.Author})"
                    : $"{r.TeamName ?? "CONTEST"} (c{r.ContestId}t{r.Author})");
        }

        public async Task<IEnumerable<(Detail, Testcase)>> GetDetailsAsync(int pid, int jid)
        {
            var query =
                from t in Context.Set<Testcase>()
                where t.ProblemId == pid
                join d in Context.Set<Detail>() on new { t.TestcaseId, JudgingId = jid } equals new { d.TestcaseId, d.JudgingId }
                orderby t.Rank ascending
                select new { t, d };
            var results = await query.ToListAsync();
            return results.Select(a => (a.d, a.t));
        }

        public async Task<IFileInfo> GetRunFileAsync(
            int jid, int rid, string type,
            int? sid = null, int? pid = null)
        {
            var notfound = new NotFoundFileInfo($"j{jid}/r{rid}.{type}");
            var fileInfo = RunFiles.GetFileInfo($"j{jid}/r{rid}.{type}");
            if (!fileInfo.Exists) return notfound;
            var accessQuery =
                from r in Context.Set<Detail>()
                where r.JudgingId == jid && r.TestId == rid
                join j in Judgings on r.JudgingId equals j.JudgingId
                join s in Submissions on j.SubmissionId equals s.SubmissionId
                select new { s.SubmissionId, s.ProblemId };
            var result = await accessQuery.SingleOrDefaultAsync();
            if (sid.HasValue && result.SubmissionId != sid.Value) return notfound;
            if (pid.HasValue && result.ProblemId != pid.Value) return notfound;
            return fileInfo;
        }

        public async Task<IEnumerable<Submission>> ListAsync(
            Expression<Func<Submission, bool>> predicate = null)
        {
            return await Submissions.Where(predicate).ToListAsync();
        }

        public async Task<IEnumerable<T>> ListAsync<T>(
            Expression<Func<Submission, bool>> predicate,
            Expression<Func<Submission, T>> projection)
        {
            return await Submissions.Where(predicate).Select(projection).ToListAsync();
        }

        public async Task RejudgeAsync(Submission sub, bool fullTest = false)
        {
            if (sub.ExpectedResult != null) fullTest = true;

            var currentJudging = await Judgings
                .Where(j => j.SubmissionId == sub.SubmissionId && j.Active)
                .SingleAsync();

            currentJudging.Active = false;
            fullTest = fullTest || currentJudging.FullTest;
            Judgings.Update(currentJudging);

            Judgings.Add(new Judging
            {
                SubmissionId = sub.SubmissionId,
                FullTest = fullTest,
                Active = true,
                Status = Verdict.Pending,
            });

            await Context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SubmissionStatistics>> StatisticsByUserAsync(int uid)
        {
            var query =
                from ss in Context.Set<SubmissionStatistics>()
                where ss.Author == uid && ss.ContestId == 0
                join a in Context.Set<ProblemArchive>() on ss.ProblemId equals a.ProblemId
                select new SubmissionStatistics
                {
                    AcceptedSubmission = ss.AcceptedSubmission,
                    Author = ss.Author,
                    ContestId = ss.ContestId,
                    TotalSubmission = ss.TotalSubmission,
                    ProblemId = a.PublicId
                };

            return await query.ToListAsync();
        }

        public Task UpdateAsync(Submission submission)
        {
            Submissions.Update(submission);
            return Context.SaveChangesAsync();
        }

        public async ValueTask<(IEnumerable<T> list, int totPage)> ListWithJudgingAsync<T>(
            (int Page, int PageCount) pagination,
            Expression<Func<Submission, Judging, T>> selector,
            Expression<Func<Submission, bool>> predicate = null)
        {
            IQueryable<Submission> submissions = Submissions;
            if (predicate != null)
                submissions = submissions.Where(predicate);

            int tot = await submissions.CountAsync();
            int totPage = (tot - 1) / pagination.PageCount + 1;

            var query = Queryable.Join(
                outer: submissions.OrderByDescending(s => s.SubmissionId),
                inner: Judgings,
                outerKeySelector: s => new { s.SubmissionId, Active = true },
                innerKeySelector: j => new { j.SubmissionId, j.Active },
                resultSelector: selector);
            var result = await query
                .Skip((pagination.Page - 1) * pagination.PageCount)
                .Take(pagination.PageCount)
                .ToListAsync();
            return (result, totPage);
        }

        public async Task<IEnumerable<T>> ListWithJudgingAsync<T>(
            Expression<Func<Submission, Judging, T>> selector,
            Expression<Func<Submission, bool>> predicate = null)
        {
            IQueryable<Submission> submissions = Submissions;
            if (predicate != null)
                submissions = submissions.Where(predicate);

            var query = Queryable.Join(
                outer: submissions.OrderByDescending(s => s.SubmissionId),
                inner: Judgings,
                outerKeySelector: s => new { s.SubmissionId, Active = true },
                innerKeySelector: j => new { j.SubmissionId, j.Active },
                resultSelector: selector);
            return await query.ToListAsync();
        }
    }
}
