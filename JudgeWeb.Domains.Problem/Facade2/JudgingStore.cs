using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementFacade :
        IJudgingStore,
        ICreateRepositoryImpl<Judging>,
        IUpdateRepositoryImpl<Judging>,
        ICreateRepositoryImpl<Detail>
    {
        public IJudgingStore JudgingStore => this;

        public DbSet<Judging> Judgings => Context.Set<Judging>();

        public DbSet<Detail> Details => Context.Set<Detail>();

        async Task<IEnumerable<T>> IJudgingStore.GetDetailsAsync<T>(
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

        async Task<IFileInfo> IJudgingStore.GetRunFileAsync(
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

        async Task<(Judging j, int pid, int cid, int uid, DateTimeOffset time)> IJudgingStore.FindAsync(int judgingId)
        {
            var res = await Judgings
                .Where(j => j.JudgingId == judgingId)
                .Select(j => new { j, j.s.ProblemId, j.s.ContestId, j.s.Author, j.s.Time })
                .SingleOrDefaultAsync();
            if (res == null) return default;
            return (res.j, res.ProblemId, res.ContestId, res.Author, res.Time);
        }

        Task<List<T>> IJudgingStore.ListAsync<T>(
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

        Task<Detail> IJudgingStore.SummarizeAsync(Judging j)
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

        Task<IFileInfo> IJudgingStore.SetRunFileAsync(int jid, int rid, string type, byte[] content)
        {
            return Files.WriteBinaryAsync($"j{jid}/r{rid}.{type}", content);
        }
    }
}
