using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Providers;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(StatusProvider), typeof(DefaultStatusProvider))]
namespace JudgeWeb.Areas.Judge.Providers
{
    public abstract class StatusProvider
    {
        public int ItemsPerPage { get; set; } = 15;

        public AppDbContext DbContext { get; }

        public UserManager UserManager { get; }

        public StatusProvider(AppDbContext db, UserManager um)
        {
            DbContext = db;
            DbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            UserManager = um;
        }

        public abstract Task<IEnumerable<StatusListModel>> FetchListAsync(
            int page, int? pid, int? status, int? uid, string lang);

        public virtual async Task<CodeViewModel> FetchCodeViewAsync(
            int sid, int? gid, System.Security.Claims.ClaimsPrincipal user)
        {
            IQueryable<Judging> gradeSource;

            bool admin = user.IsInRoles("Administrator,Problem");
            string uid = UserManager.GetUserId(user);

            if (gid is null || !admin)
            {
                gradeSource = DbContext.Judgings
                    .Where(g => g.SubmissionId == sid && g.Active);
            }
            else
            {
                gradeSource = DbContext.Judgings
                    .Where(g => g.SubmissionId == sid && g.JudgingId == gid.Value);
            }

            var query =
                from g in gradeSource
                join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                join p in DbContext.Problems on s.ProblemId equals p.ProblemId
                join l in DbContext.Languages on s.Language equals l.LangId
                join h in DbContext.JudgeHosts on g.ServerId equals h.ServerId into hh
                from h in hh.DefaultIfEmpty()
                select new CodeViewModel
                {
                    Time = s.Time,
                    SubmissionId = s.SubmissionId,
                    Status = g.Status,
                    Author = s.Author,
                    ContestId = s.ContestId,
                    CodeLength = s.CodeLength,
                    ExecuteMemory = g.ExecuteMemory,
                    ExecuteTime = g.ExecuteTime,
                    Ip = s.Ip,
                    ProblemId = s.ProblemId,
                    Language = s.Language,
                    ServerId = g.ServerId,
                    SourceCode = s.SourceCode,
                    CompileError = g.CompileError,
                    ProblemTitle = p.Title,
                    LanguageName = l.Name,
                    ServerName = h.ServerName ?? "UNKNOWN",
                    LanguageExternalId = l.ExternalId,
                    JudgingId = g.JudgingId,
                };

            if (!admin)
            {
                if (uid == null) return null;
                else if (!int.TryParse(uid, out int uuuid)) return null;
                else query = query.Where(q => q.Author == uuuid && q.ContestId == 0);
            }

            var model = await query.FirstOrDefaultAsync();
            if (model == null) return null;

            model.Details = await DbContext.Details
                .Where(d => d.JudgingId == model.JudgingId)
                .ToListAsync();

            model.TestcaseNumber = await DbContext.Testcases
                .CountAsync(t => t.ProblemId == model.ProblemId);

            return model;
        }
    }
}
