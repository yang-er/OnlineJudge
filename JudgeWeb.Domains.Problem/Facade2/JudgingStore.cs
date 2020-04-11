using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

[assembly: Inject(typeof(IJudgingStore), typeof(JudgingStore))]
namespace JudgeWeb.Domains.Problems
{
    public class JudgingStore :
        IJudgingStore,
        ICreateRepositoryImpl<Judging>,
        IUpdateRepositoryImpl<Judging>,
        ICreateRepositoryImpl<Detail>
    {
        public DbContext Context { get; }

        public IMutableFileProvider Files { get; }

        public DbSet<Submission> Submissions => Context.Set<Submission>();

        public DbSet<Judging> Judgings => Context.Set<Judging>();

        public DbSet<Detail> Details => Context.Set<Detail>();

        public JudgingStore(DbContextAccessor context, Features.Storage.IRunFileRepository runs)
        {
            Context = context;
            Files = runs;
        }

        public async Task<IEnumerable<T>> GetDetailsAsync<T>(
            int pid, int jid,
            Expression<Func<Testcase, Detail, T>> selector)
        {
            var _selector = selector.Combine(
                objectTemplate1: new { t = default(Testcase), dd = default(IEnumerable<Detail>) },
                objectTemplate2: default(Detail),
                place1: (a, d) => a.t, place2: (a, d) => d);

            var query = Context.Set<Testcase>()
                .Where(t => t.ProblemId == pid)
                .OrderBy(t => t.Rank)
                .GroupJoin(
                    inner: Context.Set<Detail>(),
                    outerKeySelector: t => new { t.TestcaseId, JudgingId = jid },
                    innerKeySelector: d => new { d.TestcaseId, d.JudgingId },
                    resultSelector: (t, dd) => new { t, dd })
                .SelectMany(
                    collectionSelector: a => a.dd.DefaultIfEmpty(),
                    resultSelector: _selector);

            return await query.ToListAsync();
        }

        public async Task<IFileInfo> GetRunFileAsync(
            int jid, int rid, string type, int? sid, int? pid)
        {
            var notfound = new NotFoundFileInfo($"j{jid}/r{rid}.{type}");
            var fileInfo = Files.GetFileInfo($"j{jid}/r{rid}.{type}");
            if (!fileInfo.Exists) return notfound;
            var accessQuery =
                from r in Details
                where r.JudgingId == jid && r.TestId == rid
                join j in Judgings on r.JudgingId equals j.JudgingId
                join s in Submissions on j.SubmissionId equals s.SubmissionId
                select new { s.SubmissionId, s.ProblemId };
            var result = await accessQuery.SingleOrDefaultAsync();
            if (sid.HasValue && result.SubmissionId != sid.Value) return notfound;
            if (pid.HasValue && result.ProblemId != pid.Value) return notfound;
            return fileInfo;
        }

        public async Task<(
            Judging j,
            int pid, int cid, int uid,
            DateTimeOffset time)> FindAsync(int judgingId)
        {
            var res = await Judgings
                .Where(j => j.JudgingId == judgingId)
                .Select(j => new { j, j.s.ProblemId, j.s.ContestId, j.s.Author, j.s.Time })
                .SingleOrDefaultAsync();
            if (res == null) return default;
            return (res.j, res.ProblemId, res.ContestId, res.Author, res.Time);
        }

        public Task<List<T>> ListAsync<T>(
            Expression<Func<Judging, bool>> predicate,
            Expression<Func<Judging, T>> selector,
            int topCount)
        {
            return Judgings
                .Where(predicate)
                .OrderBy(j => j.JudgingId)
                .Take(topCount)
                .Select(selector)
                .ToListAsync();
        }

        public Task<T> FindAsync<T>(
            Expression<Func<Judging, bool>> predicate,
            Expression<Func<Judging, T>> selector)
        {
            return Judgings
                .Where(predicate)
                .OrderBy(j => j.JudgingId)
                .Select(selector)
                .FirstOrDefaultAsync();
        }

        public Task<List<Judging>> ListAsync(
            Expression<Func<Judging, bool>> predicate,
            int topCount)
        {
            return Judgings
                .Where(predicate)
                .OrderBy(j => j.JudgingId)
                .Take(topCount)
                .ToListAsync();
        }

        public Task<Detail> SummarizeAsync(Judging j)
        {
            return
               (from d in Details
                where d.JudgingId == j.JudgingId
                join t in Context.Set<Testcase>() on d.TestcaseId equals t.TestcaseId
                group new { d.Status, d.ExecuteMemory, d.ExecuteTime, t.Point } by d.JudgingId into g
                select new Detail
                {
                    JudgingId = g.Key,
                    Status = g.Min(a => a.Status),
                    TestcaseId = g.Count(),
                    ExecuteMemory = g.Max(a => a.ExecuteMemory),
                    ExecuteTime = g.Max(a => a.ExecuteTime),
                    TestId = g.Sum(a => a.Status == Verdict.Accepted ? a.Point : 0)
                })
                .SingleOrDefaultAsync();
        }

        public Task<IFileInfo> SetRunFileAsync(int jid, int rid, string type, byte[] content)
        {
            return Files.WriteBinaryAsync($"j{jid}/r{rid}.{type}", content);
        }

        public Task<int> CountAsync(Expression<Func<Judging, bool>> predicate)
        {
            return Judgings.Where(predicate).CountAsync();
        }

        public async Task<List<ServerStatus>> GetJudgeQueueAsync(int? cid = null)
        {
            IQueryable<Submission> submissions = Context.Set<Submission>();
            if (cid != null) submissions = submissions.Where(s => s.ContestId == cid);

            var judgingStatus = await Queryable
                .Join(
                    outer: Context.Set<Judging>(),
                    inner: Context.Set<Submission>(),
                    outerKeySelector: j => j.SubmissionId,
                    innerKeySelector: s => s.SubmissionId,
                    resultSelector: (j, s) => new { j.Status, s.ContestId })
                .GroupBy(g => new { g.Status, g.ContestId })
                .Select(g => new { g.Key.Status, g.Key.ContestId, Count = g.Count() })
                .ToListAsync();

            return judgingStatus
                .GroupBy(a => a.ContestId)
                .Select(g => new ServerStatus
                {
                    cid = g.Key,
                    num_submissions = g.Sum(a => a.Count),
                    num_queued = g.SingleOrDefault(a => a.Status == Verdict.Pending)?.Count ?? 0,
                    num_judging = g.SingleOrDefault(a => a.Status == Verdict.Running)?.Count ?? 0,
                })
                .ToList();
        }

        public async Task<IEnumerable<T>> GetDetailsInnerJoinAsync<T>(
            Expression<Func<Testcase, Detail, T>> selector,
            Expression<Func<Testcase, Detail, bool>>? predicate = null,
            int? limit = null)
        {
            var _selector = selector.Combine(
                objectTemplate: new { t = (Testcase)null, d = (Detail)null },
                place1: a => a.t, place2: a => a.d);
            var _predicate = predicate?.Combine(
                objectTemplate: new { t = (Testcase)null, d = (Detail)null },
                place1: a => a.t, place2: a => a.d);

            var query = Details.Join(
                inner: Context.Set<Testcase>(),
                outerKeySelector: d => d.TestcaseId,
                innerKeySelector: t => t.TestcaseId,
                resultSelector: (d, t) => new { t, d });
            if (_predicate != null)
                query = query.Where(_predicate)
                    .OrderBy(a => a.d.TestId);
            var query2 = query.Select(_selector);
            if (limit.HasValue)
                query2 = query2.Take(limit.Value);
            return await query2.ToListAsync();
        }
    }
}
