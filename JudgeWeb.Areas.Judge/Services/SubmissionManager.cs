using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JudgeWeb.Areas.Judge.Models;
using System.Net;
using JudgeWeb.Areas.Judge.Services;

[assembly: Inject(typeof(SubmissionManager))]
namespace JudgeWeb.Areas.Judge.Services
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

        public async Task<int> CreateAsync(CodeSubmitModel model,
            IPAddress ip, int uid, string username)
        {
            var s = DbContext.Submissions.Add(new Submission
            {
                Author = uid,
                CodeLength = model.Code.Length,
                Ip = ip.ToString(),
                Language = model.Language,
                ProblemId = model.ProblemId,
                SourceCode = model.Code.ToBase64(),
                ContestId = 0,
                Time = DateTimeOffset.Now,
            });

            await DbContext.SaveChangesAsync();

            DbContext.AuditLogs.Add(new AuditLog
            {
                ContestId = 0,
                EntityId = s.Entity.SubmissionId,
                Time = s.Entity.Time.DateTime,
                Resolved = true,
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
                RejudgeId = 0,
            });

            await DbContext.SaveChangesAsync();
            return s.Entity.SubmissionId;
        }
    }
}
