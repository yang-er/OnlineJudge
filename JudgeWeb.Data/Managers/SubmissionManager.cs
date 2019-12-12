using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace JudgeWeb.Data
{
    public class SubmissionManager
    {
        protected AppDbContext DbContext { get; }

        public SubmissionManager(AppDbContext adbc)
        {
            DbContext = adbc;
        }

        public IQueryable<Submission> Submissions => DbContext.Submissions;

        public IQueryable<Judging> Judgings => DbContext.Judgings;


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


        public async Task<int> RejudgeAsync(List<Submission> subs,
            Func<Rejudge> rejudger, bool fullTest = false)
        {
            if (subs.Any(s => s.RejudgeId.HasValue))
                return -1;
            var rejudge = rejudger.Invoke();
            if (rejudge.RejudgeId != 0)
                throw new InvalidOperationException();
            DbContext.Rejudges.Add(rejudge);
            await DbContext.SaveChangesAsync();

            foreach (var sub in subs)
            {
                sub.RejudgeId = rejudge.RejudgeId;
                DbContext.Submissions.Update(sub);

                DbContext.Judgings.Add(new Judging
                {
                    SubmissionId = sub.SubmissionId,
                    FullTest = fullTest,
                    Active = false,
                    Status = Verdict.Pending,
                    RejudgeId = rejudge.RejudgeId,
                });
            }

            await DbContext.SaveChangesAsync();
            return rejudge.RejudgeId;
        }


        public async Task RejudgeForErrorAsync(Judging judging)
        {
            DbContext.Judgings.Add(new Judging
            {
                Active = judging.Active,
                Status = Verdict.Pending,
                FullTest = judging.FullTest,
                RejudgeId = judging.RejudgeId,
                SubmissionId = judging.SubmissionId,
            });

            judging.Active = false;
            judging.Status = Verdict.UndefinedError;
            judging.RejudgeId = null;
            if (!judging.StopTime.HasValue)
                judging.StopTime = DateTimeOffset.Now;

            DbContext.Judgings.Update(judging);
            await DbContext.SaveChangesAsync();
        }


        public async Task RejudgeForErrorAsync(int jid)
        {
            var judging = await Judgings
                .Where(j => j.JudgingId == jid)
                .SingleAsync();
            await RejudgeForErrorAsync(judging);
        }


        public async Task<Submission> CreateAsync(
            string code, int langid, int probid, int cid, int uid,
            IPAddress ipAddr, string via, string username, Verdict? expected = null)
        {
            var s = DbContext.Submissions.Add(new Submission
            {
                Author = uid,
                CodeLength = code.Length,
                ContestId = cid,
                Ip = ipAddr.ToString(),
                Language = langid,
                ProblemId = probid,
                Time = DateTimeOffset.Now,
                SourceCode = code.ToBase64(),
                ExpectedResult = expected,
            });

            await DbContext.SaveChangesAsync();

            DbContext.AuditLogs.Add(new AuditLog
            {
                ContestId = cid,
                EntityId = s.Entity.SubmissionId,
                Time = s.Entity.Time.DateTime,
                Resolved = true,
                Type = AuditLog.TargetType.Submission,
                UserName = username,
                Comment = "added via " + via,
            });

            DbContext.Judgings.Add(new Judging
            {
                SubmissionId = s.Entity.SubmissionId,
                FullTest = expected.HasValue,
                Active = true,
                Status = Verdict.Pending,
            });

            await DbContext.SaveChangesAsync();
            return s.Entity;
        }
    }
}
