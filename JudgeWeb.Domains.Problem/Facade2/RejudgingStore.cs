using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementFacade :
        IRejudgingStore,
        ICrudRepositoryImpl<Rejudge>
    {
        public IRejudgingStore RejudgingStore => this;

        DbSet<Rejudge> Rejudges { get; }

        async Task IRejudgingStore.RejudgeAsync(Submission sub, bool fullTest)
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

        async Task<int> IRejudgingStore.BatchRejudgeAsync(
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
                    await RejudgingStore.RejudgeAsync(sub, fullTest);
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
    }
}
