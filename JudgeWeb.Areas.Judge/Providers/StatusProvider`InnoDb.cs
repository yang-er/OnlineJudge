using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Providers
{
    internal class InnoDbStatusProvider : StatusProvider
    {
        public LanguageManager Languages { get; }

        public InnoDbStatusProvider(AppDbContext db, UserManager um, LanguageManager lm) : base(db, um)
        {
            Languages = lm;
        }

        public override async Task<IEnumerable<StatusListModel>> FetchListAsync(
            int page, int? pid, int? status, int? uid, string lang)
        {
            var langs = Languages.GetAll();

            var query2 =
                from s in DbContext.Submissions
                join g in DbContext.Judgings
                    on new { s.SubmissionId, Active = true }
                    equals new { g.SubmissionId, g.Active }
                select new { g, s };

            if (pid.HasValue)
                query2 = query2.Where(a => a.s.ProblemId == pid.Value);
            if (status.HasValue)
                query2 = query2.Where(a => (int)a.g.Status == status.Value);
            if (uid.HasValue)
                query2 = query2.Where(a => a.s.Author == uid.Value && a.s.ContestId == 0);

            if (lang != null)
            {
                int langid = langs.Values
                    .FirstOrDefault(k => k.ExternalId == lang)
                    ?.LangId ?? 0;
                query2 = query2.Where(a => a.s.Language == langid);
            }

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
                        LanguageId = a.s.Language,
                        ContestId = a.s.ContestId,
                    })
                .Skip((page - 1) * ItemsPerPage)
                .Take(ItemsPerPage);

            var result = await query.ToListAsync();

            var userIds = result
                .Where(s => s.ContestId == 0)
                .Select(s => s.Author)
                .Distinct()
                .ToArray();

            var users = await DbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.NickName })
                .ToDictionaryAsync(u => u.Id);

            foreach (var item in result)
            {
                if (item.ContestId == 0 && users.ContainsKey(item.Author))
                    item.UserName = users[item.Author].NickName ?? users[item.Author].UserName;
                else
                    item.UserName = null;
                item.Language = langs[item.LanguageId].Name;
            }

            return result;
        }
    }
}
