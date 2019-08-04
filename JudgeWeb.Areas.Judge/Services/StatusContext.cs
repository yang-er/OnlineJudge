using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Services
{
    public partial class JudgeContext
    {
        public async Task<IEnumerable<StatusListModel>> GetStatusList(int page,
            int? pid, int? status, int? uid, string lang)
        {
            var query2 =
                from s in DbContext.Submissions
                join g in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { g.SubmissionId, g.Active }
                join l in DbContext.Languages on s.Language equals l.LangId
                join u in DbContext.Users on s.Author equals u.Id into uu
                from u in uu.DefaultIfEmpty()
                select new { g, s, l, u };

            if (pid.HasValue)
                query2 = query2.Where(a => a.s.ProblemId == pid.Value);
            if (status.HasValue)
                query2 = query2.Where(a => (int)a.g.Status == status.Value);
            if (uid.HasValue)
                query2 = query2.Where(a => a.s.Author == uid.Value);
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
                        UserName = a.u == null
                                 ? null
                                 : string.IsNullOrEmpty(a.u.NickName)
                                 ? a.u.UserName
                                 : a.u.NickName
                    })
                .Skip((page - 1) * ItemsPerPage)
                .Take(ItemsPerPage);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<StatusListModel>> GetStatusListFast(int page,
            int? pid, int? status, int? uid, string lang)
        {
            var langs = GetLanguages();

            var query2 =
                from s in DbContext.Submissions
                join g in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { g.SubmissionId, g.Active }
                select new { g, s };

            if (pid.HasValue)
                query2 = query2.Where(a => a.s.ProblemId == pid.Value);
            if (status.HasValue)
                query2 = query2.Where(a => (int)a.g.Status == status.Value);
            if (uid.HasValue)
                query2 = query2.Where(a => a.s.Author == uid.Value);

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

        public async Task<int> ActivateJudging(int gid, string username)
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
                    UserName = username
                });

                await DbContext.SaveChangesAsync();
            }

            return newGrade.SubmissionId;
        }

        public async Task<int> Rejudge(int sid, bool full)
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
                RejudgeId = -1,
            });

            await DbContext.SaveChangesAsync();
            return judging.Entity.JudgingId;
        }

        public async Task<CodeViewModel> ViewCode(int sid, int? gid, string uid, bool admin)
        {
            IQueryable<Judging> gradeSource;

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

        public async Task<IEnumerable<(Judging, string)>> GetJudgings(int sid)
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
    }
}
