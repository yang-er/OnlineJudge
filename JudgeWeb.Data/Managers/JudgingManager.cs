using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JudgeWeb.Data
{
    [Obsolete]
    public class JudgingManager
    {
        protected AppDbContext DbContext { get; }

        protected UserManager UserManager { get; }

        public ClaimsPrincipal User { get; set; }

        private string UserName => UserManager.GetUserName(User)
            ?? throw new InvalidOperationException();

        public JudgingManager(AppDbContext adbc, UserManager um)
        {
            DbContext = adbc;
            UserManager = um;
        }

        public JudgingManager WithUser(ClaimsPrincipal user)
        {
            User = user;
            return this;
        }

        public async Task<bool> CheckAvaliabilityForAdminAsync(int sid)
        {
            if (User.IsInRole("Administrator")) return true;
            var pid = await DbContext.Submissions
                .Where(s => s.SubmissionId == sid)
                .Select(s => new { s.ProblemId })
                .FirstOrDefaultAsync();
            if (pid == null) return false;
            if (!User.IsInRole("AuthorOfProblem" + pid.ProblemId)) return false;
            return true;
        }

        public async Task<bool> CheckAvaliabilityForAdminByJudgingAsync(int jid)
        {
            if (User.IsInRole("Administrator")) return true;
            var pid = await DbContext.Judgings
                .Where(j => j.JudgingId == jid)
                .Join(
                    inner: DbContext.Submissions,
                    outerKeySelector: j => j.SubmissionId,
                    innerKeySelector: s => s.SubmissionId,
                    resultSelector: (j, s) => new { s.ProblemId })
                .FirstOrDefaultAsync();
            if (pid == null) return false;
            if (!User.IsInRole("AuthorOfProblem" + pid.ProblemId)) return false;
            return true;
        }

        public async Task<IEnumerable<(Judging, string)>> EnumerateBySubmissionAsync(int sid)
        {
            var grades =
                from g in DbContext.Judgings
                where g.SubmissionId == sid
                join h in DbContext.JudgeHosts on g.ServerId equals h.ServerId into hh
                from h in hh.DefaultIfEmpty()
                select new { g, n = h.ServerName ?? "-" };
            var gs = await grades.ToListAsync();
            return gs.Select(a => (a.g, a.n));
        }

        public async Task<int> ActivateByIdAsync(int gid)
        {
            var newGrade = await DbContext.Judgings
                .Where(g => g.JudgingId == gid)
                .FirstOrDefaultAsync();
            if (newGrade is null) return -1;

            if (!newGrade.Active)
            {
                var oldGrade = await DbContext.Judgings
                    .Where(g => g.SubmissionId == newGrade.SubmissionId && g.Active)
                    .FirstOrDefaultAsync();
                if (oldGrade is null) return int.MinValue;

                newGrade.Active = true;
                oldGrade.Active = false;
                DbContext.Judgings.Update(newGrade);
                DbContext.Judgings.Update(oldGrade);

                DbContext.AuditLogs.Add(new AuditLog
                {
                    Comment = $"changed active judging from " +
                        $"j{oldGrade.JudgingId} to j{newGrade.JudgingId}",
                    ContestId = 0,
                    EntityId = newGrade.SubmissionId,
                    Type = AuditLog.TargetType.Submission,
                    Resolved = true,
                    Time = DateTimeOffset.Now,
                    UserName = UserName,
                });

                await DbContext.SaveChangesAsync();
            }

            return newGrade.SubmissionId;
        }

        public async Task<int> RejudgeSubmissionAsync(int sid, bool full)
        {
            var query = await DbContext.Submissions
                .CountAsync(s => s.SubmissionId == sid);
            if (query != 1) return -1;

            var judging = DbContext.Judgings.Add(new Judging
            {
                SubmissionId = sid,
                Active = false,
                FullTest = full,
                Status = Verdict.Pending,
            });

            await DbContext.SaveChangesAsync();

            DbContext.AuditLogs.Add(new AuditLog
            {
                Comment = $"created rejudge j" +
                    judging.Entity.JudgingId,
                ContestId = 0,
                EntityId = sid,
                Type = AuditLog.TargetType.Submission,
                Resolved = true,
                Time = DateTimeOffset.Now,
                UserName = UserName,
            });

            await DbContext.SaveChangesAsync();
            return judging.Entity.JudgingId;
        }
    }
}
