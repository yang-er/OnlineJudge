using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

[assembly: Inject(typeof(ISubmissionStore), typeof(SubmissionStore))]
namespace JudgeWeb.Domains.Problems
{
    public class SubmissionStore :
        ISubmissionStore,
        IUpdateRepositoryImpl<Submission>
    {
        public DbContext Context { get; }

        DbSet<Submission> Submissions => Context.Set<Submission>();

        DbSet<Judging> Judgings => Context.Set<Judging>();

        public SubmissionStore(DbContextAccessor context)
        {
            Context = context;
        }

        public async Task<Submission> CreateAsync(
            string code, string lang, int pid, int? cid, int uid,
            IPAddress ipAddr, string via, string username, Verdict? expected,
            DateTimeOffset? time, bool fullJudge)
        {
            var s = Submissions.Add(new Submission
            {
                Author = uid,
                CodeLength = code.Length,
                ContestId = cid ?? 0,
                Ip = ipAddr.ToString(),
                Language = lang,
                ProblemId = pid,
                Time = time ?? DateTimeOffset.Now,
                SourceCode = code.ToBase64(),
                ExpectedResult = expected,
            });

            await Context.SaveChangesAsync();

            Context.Add(new Auditlog
            {
                ContestId = cid,
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

            // TODO: contest event????

            await Context.SaveChangesAsync();
            return s.Entity;
        }

        public Task<Submission> FindAsync(int sid, bool includeJudgings)
        {
            var query = Submissions
                .Where(s => s.SubmissionId == sid);
            if (includeJudgings)
                query = query.Include(s => s.Judgings);
            return query.SingleOrDefaultAsync();
        }

        public Task<Submission> FindByJudgingAsync(int jid)
        {
            return Judgings
                .Where(j => j.JudgingId == jid)
                .Select(j => j.s)
                .SingleOrDefaultAsync();
        }

        public async Task<string> GetAuthorNameAsync(int sid)
        {
            var result = await GetAuthorNamesAsync(s => s.SubmissionId == sid);
            return result.Values.SingleOrDefault();
        }

        public Task<Dictionary<int, string>> GetAuthorNamesAsync(IQueryable<Submission> sids)
        {
            var query =
                from s in sids
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

        public async Task<(IEnumerable<T> list, int count)> ListWithJudgingAsync<T>(
            (int Page, int PageCount) pagination,
            Expression<Func<Submission, Judging, T>> selector,
            Expression<Func<Submission, bool>>? predicate)
        {
            IQueryable<Submission> submissions = Submissions;
            if (predicate != null)
                submissions = submissions.Where(predicate);

            int tot = await submissions.CountAsync();

            var result = await submissions
                .OrderByDescending(s => s.SubmissionId)
                .NatureJoin(Judgings, selector)
                .Skip((pagination.Page - 1) * pagination.PageCount)
                .Take(pagination.PageCount)
                .ToListAsync();
            return (result, tot);
        }

        public async Task<IEnumerable<T>> ListWithJudgingAsync<T>(
            Expression<Func<Submission, Judging, T>> selector,
            Expression<Func<Submission, bool>> predicate, int? limits)
        {
            IQueryable<Submission> submissions = Submissions;
            if (predicate != null)
                submissions = submissions.Where(predicate);
            submissions = submissions.OrderByDescending(s => s.SubmissionId);
            if (limits.HasValue) submissions = submissions.Take(limits.Value);
            return await submissions
                .NatureJoin(Judgings, selector)
                .ToListAsync();
        }

        public async Task<(string, string)?> GetFileAsync(int sid)
        {
            var query =
                from s in Submissions
                where s.SubmissionId == sid
                join l in Context.Set<Language>() on s.Language equals l.Id
                select new { s.SourceCode, l.FileExtension };
            var result = await query.SingleOrDefaultAsync();
            if (result == null) return null;
            return (result.SourceCode, result.FileExtension);
        }

        public async Task<IEnumerable<T>> ListAsync<T>(
            Expression<Func<Submission, T>> projection,
            Expression<Func<Submission, bool>> predicate)
        {
            IQueryable<Submission> query = Submissions;
            if (predicate != null) query = query.Where(predicate);
            var query2 = query.Select(projection);
            return await query2.ToListAsync();
        }

        public Task<Dictionary<int, string>> GetAuthorNamesAsync(Expression<Func<Submission, bool>> sids)
        {
            return GetAuthorNamesAsync(Submissions.Where(sids));
        }
    }
}
