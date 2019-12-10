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

        public async Task<int> CreateAsync(
            string code, int language, int problemId,
            IPAddress ip, int cid, int uid, string username)
        {
            var s = DbContext.Submissions.Add(new Submission
            {
                Author = uid,
                CodeLength = code.Length,
                Ip = ip.ToString(),
                Language = language,
                ProblemId = problemId,
                SourceCode = code.ToBase64(),
                ContestId = cid,
                Time = DateTimeOffset.Now,
            });

            await DbContext.SaveChangesAsync();

            DbContext.AuditLogs.Add(new AuditLog
            {
                ContestId = cid,
                EntityId = s.Entity.SubmissionId,
                Time = s.Entity.Time.DateTime,
                Resolved = cid == 0,
                Type = AuditLog.TargetType.Submission,
                UserName = username,
                Comment = "added via problem list",
            });

            DbContext.Judgings.Add(new Judging
            {
                SubmissionId = s.Entity.SubmissionId,
                FullTest = false,
                Active = true,
                Status = Verdict.Pending,
            });

            await DbContext.SaveChangesAsync();
            return s.Entity.SubmissionId;
        }
    }
}
