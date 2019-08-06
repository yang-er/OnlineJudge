using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Providers
{
    internal class DefaultStatusProvider : StatusProvider
    {
        public DefaultStatusProvider(AppDbContext db, UserManager um) : base(db, um) { }

        public override async Task<IEnumerable<StatusListModel>> FetchListAsync(
            int page, int? pid, int? status, int? uid, string lang)
        {
            var query2 =
                from s in DbContext.Submissions
                join g in DbContext.Judgings
                    on new { s.SubmissionId, Active = true }
                    equals new { g.SubmissionId, g.Active }
                join l in DbContext.Languages on s.Language equals l.LangId
                join u in DbContext.Users on s.Author equals u.Id into uu
                from u in uu.DefaultIfEmpty()
                select new { g, s, l, u };

            if (pid.HasValue)
                query2 = query2.Where(a => a.s.ProblemId == pid.Value);
            if (status.HasValue)
                query2 = query2.Where(a => (int)a.g.Status == status.Value);
            if (uid.HasValue)
                query2 = query2.Where(a => a.s.Author == uid.Value && a.s.ContestId == 0);
            if (lang != null)
                query2 = query2.Where(a => a.l.ExternalId == lang);

            if (page < 0)
            {
                query2 = query2.OrderBy(a => a.s.SubmissionId);
                page = -page;
            }
            else
            {
                query2 = query2.OrderByDescending(a => a.s.SubmissionId);
            }

            var query = query2
                .Select(a =>
                    new StatusListModel
                    {
                        Author = a.s.Author,
                        CodeLength = a.s.CodeLength,
                        ProblemId = a.s.ProblemId,
                        ExecuteMemory = a.g.ExecuteMemory,
                        ExecuteTime = a.g.ExecuteTime,
                        Status = a.g.Status,
                        SubmissionId = a.s.SubmissionId,
                        Time = a.s.Time,
                        Language = a.l.Name,
                        LanguageId = a.s.Language,
                        ContestId = a.s.ContestId,
                        UserName = a.u == null || a.s.ContestId != 0
                                 ? null
                                 : string.IsNullOrEmpty(a.u.NickName)
                                 ? a.u.UserName
                                 : a.u.NickName
                    })
                .Skip((page - 1) * ItemsPerPage)
                .Take(ItemsPerPage);

            return await query.ToListAsync();
        }
    }
}
