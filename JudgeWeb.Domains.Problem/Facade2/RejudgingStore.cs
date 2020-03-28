using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

[assembly: Inject(typeof(IRejudgingStore), typeof(RejudgingStore))]
namespace JudgeWeb.Domains.Problems
{
    public class RejudgingStore :
        IRejudgingStore,
        ICrudRepositoryImpl<Rejudge>
    {
        public DbContext Context { get; }

        DbSet<Rejudge> Rejudges => Context.Set<Rejudge>();

        DbSet<Submission> Submissions => Context.Set<Submission>();

        DbSet<Judging> Judgings => Context.Set<Judging>();

        public RejudgingStore(DbContext context)
        {
            Context = context;
        }

        public async Task RejudgeAsync(Submission sub, bool fullTest)
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

        public async Task<int> BatchRejudgeAsync(
            Expression<Func<Submission, Judging, bool>> predicate,
            Rejudge rejudge, bool fullTest)
        {
            int cid = rejudge?.ContestId ?? 0;
            var selectionQuery = Submissions
                .Where(s => s.ContestId == cid && s.RejudgeId == null)
                .NatureJoin(Judgings, (s, j) => new { s, j });

            var _predicate = predicate.Combine(
                objectTemplate: new { s = default(Submission), j = default(Judging) },
                place1: a => a.s, place2: a => a.j);
            selectionQuery = selectionQuery.Where(_predicate);
            var sublist_0 = selectionQuery.Select(a => a.s.SubmissionId);
            var sublist = Submissions.Where(s => sublist_0.Contains(s.SubmissionId));

            if (rejudge == null)
            {
                // TODO: Optimize here
                var items = await sublist.ToListAsync();
                foreach (var sub in items)
                    await RejudgeAsync(sub, fullTest);
                return items.Count;
            }
            else
            {
                int rejid = rejudge.RejudgeId;
                int count = await sublist
                    .BatchUpdateAsync(s => new Submission { RejudgeId = rejid });
                if (count == 0) return 0;

                var newJudgings = Submissions
                    .Where(s => s.RejudgeId == rejid)
                    .NatureJoin(Judgings, (s, j) => new Judging
                    {
                        Active = false,
                        SubmissionId = s.SubmissionId,
                        FullTest = fullTest ? true : j.FullTest,
                        Status = Verdict.Pending,
                        RejudgeId = rejid,
                        PreviousJudgingId = j.JudgingId,
                    });

                var tok = await newJudgings
                    .BatchInsertIntoAsync(Judgings);
                return tok;
            }
        }

        public Task<Rejudge> FindAsync(int cid, int rjid)
        {
            var query =
                from r in Rejudges
                where r.ContestId == cid && r.RejudgeId == rjid
                join u in Context.Set<User>() on r.IssuedBy equals u.Id
                into uu1 from u1 in uu1.DefaultIfEmpty()
                join u in Context.Set<User>() on r.OperatedBy equals u.Id
                into uu2 from u2 in uu2.DefaultIfEmpty()
                select new Rejudge(r, u1.UserName, u2.UserName);
            return query.SingleOrDefaultAsync();
        }

        public async Task<List<Rejudge>> ListAsync(int cid)
        {
            var query =
                from r in Rejudges
                where r.ContestId == cid
                join u in Context.Set<User>() on r.IssuedBy equals u.Id
                into uu1 from u1 in uu1.DefaultIfEmpty()
                join u in Context.Set<User>() on r.OperatedBy equals u.Id
                into uu2 from u2 in uu2.DefaultIfEmpty()
                select new Rejudge(r, u1.UserName, u2.UserName);
            var model = await query.ToListAsync();

            var query2 =
                from j in Judgings
                where (from r in Rejudges
                       where r.ContestId == cid && r.OperatedBy == null
                       select (int?)r.RejudgeId).Contains(j.RejudgeId)
                group 1 by new { j.RejudgeId, j.Status } into g
                select new { g.Key, Cnt = g.Count() };
            var q2 = await query2.ToListAsync();

            foreach (var qqq in q2.GroupBy(a => a.Key.RejudgeId))
            {
                int tot = qqq.Sum(a => a.Cnt);
                int ped = qqq
                    .Where(a => a.Key.Status == Verdict.Pending || a.Key.Status == Verdict.Running)
                    .DefaultIfEmpty()
                    .Sum(a => a?.Cnt) ?? 0;
                model.First(r => r.RejudgeId == qqq.Key).Ready = (tot, ped);
            }

            return model;
        }

        public async Task<IEnumerable<(
            Judging, Judging, int pid, string lang, DateTimeOffset time, int teamid)>>
            ViewAsync(Rejudge rejudge)
        {
            var query =
                from j in Judgings
                where j.RejudgeId == rejudge.RejudgeId
                join s in Submissions on j.SubmissionId equals s.SubmissionId
                join j2 in Judgings on j.PreviousJudgingId equals j2.JudgingId
                orderby s.Time descending
                select new { s.ProblemId, s.Language, s.Time, s.Author, j, j2 };
            var results = await query.ToListAsync();
            return results.Select(a => (a.j, a.j2, a.ProblemId, a.Language, a.Time, a.Author));
        }

        public async Task CancelAsync(Rejudge rej, int uid)
        {
            int rid = rej.RejudgeId;

            var cancelJudgings = await Judgings
                .Where(j => j.RejudgeId == rid && j.Status == Verdict.Pending)
                .BatchDeleteAsync();
            var resetSubmits = await Submissions
                .Where(s => s.RejudgeId == rid)
                .BatchUpdateAsync(s => new Submission { RejudgeId = null });

            rej.EndTime = DateTimeOffset.Now;
            rej.Applied = false;
            rej.OperatedBy = uid;
            Rejudges.Update(rej);
            await Context.SaveChangesAsync();
        }

        public async Task ApplyAsync(Rejudge rejudge, int uid)
        {
            int rid = rejudge.RejudgeId;
            var applyNew = await Judgings
                .Where(j => j.RejudgeId == rid)
                .BatchUpdateAsync(j => new Judging { Active = true });

            var oldJudgings = Judgings
                .Where(j => j.RejudgeId == rid)
                .Select(j => j.PreviousJudgingId);
            var supplyOld = await Judgings
                .Where(j => oldJudgings.Contains(j.JudgingId))
                .BatchUpdateAsync(j => new Judging { Active = false });

            var oldSubmissions = Judgings
                .Where(j => j.RejudgeId == rid)
                .Select(j => j.SubmissionId);
            var resetSubmit = await Submissions
                .Where(s => oldSubmissions.Contains(s.SubmissionId))
                .BatchUpdateAsync(s => new Submission { RejudgeId = null });

            rejudge.Applied = true;
            rejudge.EndTime = DateTimeOffset.Now;
            rejudge.OperatedBy = uid;
            Rejudges.Update(rejudge);
            await Context.SaveChangesAsync();
        }
    }
}
