using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    [Area("Judge")]
    [Route("[area]/[controller]/[action]")]
    public class StatusController : Controller2
    {
        const int itemsPerPage = 15;
        const string problemRole = "Administrator,Problem";

        private AppDbContext DbContext { get; }

        private UserManager UserManager { get; }

        public StatusController(AppDbContext jdbc, UserManager jum)
        {
            DbContext = jdbc;
            UserManager = jum;
        }

        [HttpGet("{id}/{full}")]
        [Authorize(Roles = problemRole)]
        public async Task<IActionResult> Rejudge(int id, string full)
        {
            var query = await DbContext.Submissions
                .CountAsync(s => s.SubmissionId == id);
            if (query != 1) return NotFound();

            var judging = DbContext.Judgings.Add(new Judging
            {
                SubmissionId = id,
                Active = false,
                FullTest = full == "full",
                Status = Verdict.Pending,
                RejudgeId = -1,
            });

            await DbContext.SaveChangesAsync();
            return RedirectToAction(nameof(View), new { gid = judging.Entity.JudgingId });
        }

        [HttpGet("{gid}")]
        [Authorize(Roles = problemRole)]
        public async Task<IActionResult> Activate(int gid)
        {
            var newGrade = await DbContext.Judgings
                .Where(g => g.JudgingId == gid)
                .FirstOrDefaultAsync();
            if (newGrade is null) return NotFound();

            if (!newGrade.Active)
            {
                var oldGrade = await DbContext.Judgings
                    .Where(g => g.SubmissionId == newGrade.SubmissionId && g.Active)
                    .FirstOrDefaultAsync();
                if (oldGrade is null) return BadRequest();

                newGrade.Active = true;
                oldGrade.Active = false;
                DbContext.Judgings.Update(newGrade);
                DbContext.Judgings.Update(oldGrade);
                await DbContext.SaveChangesAsync();
            }

            return RedirectToAction("View", new { id = newGrade.SubmissionId, gid });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> View(int id, int? gid)
        {
            IQueryable<Judging> gradeSource;

            if (gid is null || !User.IsInRoles(problemRole))
            {
                gradeSource = DbContext.Judgings
                    .Where(g => g.SubmissionId == id && g.Active);
            }
            else
            {
                gradeSource = DbContext.Judgings
                    .Where(g => g.SubmissionId == id && g.JudgingId == gid.Value);
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

            var model = await query.FirstOrDefaultAsync();
            if (model is null) return NotFound();

            if (User.IsInRoles(problemRole))
            {
                var grades =
                    from g in DbContext.Judgings
                    where g.SubmissionId == id
                    join h in DbContext.JudgeHosts on g.ServerId equals h.ServerId into hh
                    from h in hh.DefaultIfEmpty()
                    select new { g, n = h.ServerName ?? "-" };
                var gs = await grades.ToListAsync();
                ViewBag.AllJudgings = gs.Select(a => (a.g, a.n));
            }
            else if (model.Author.ToString() != UserManager.GetUserId(User))
            {
                return Forbid();
            }

            model.Details = await DbContext.Details
                .Where(d => d.JudgingId == model.JudgingId)
                .ToListAsync();
            model.TestcaseNumber = await DbContext.Testcases
                .CountAsync(t => t.ProblemId == model.ProblemId);
            return View(model);
        }

        [HttpGet("{pg?}")]
        public async Task<IActionResult> List(int pg = 1,
            int? pid = null, int? status = null, int? uid = null, string lang = null)
        {
            var page = pg;
            if (page == 0) page = 1;
            ViewData["Page"] = page;

            var query2 =
                from s in DbContext.Submissions
                join g in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { g.SubmissionId, g.Active }
                join l in DbContext.Languages on s.Language equals l.LangId
                join u in DbContext.Users on s.Author equals u.Id into uu
                from u in uu.DefaultIfEmpty()
                select new { g, s, l, u };

            var filter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (pid.HasValue)
            {
                query2 = query2.Where(a => a.s.ProblemId == pid.Value);
                filter.Add(nameof(pid), pid.Value.ToString());
            }

            if (status.HasValue)
            {
                query2 = query2.Where(a => (int)a.g.Status == status.Value);
                filter.Add(nameof(status), status.Value.ToString());
            }

            if (uid.HasValue)
            {
                query2 = query2.Where(a => a.s.Author == uid.Value);
                filter.Add(nameof(uid), uid.Value.ToString());
            }

            if (lang != null)
            {
                query2 = query2.Where(a => a.l.ExternalId == lang);
                filter.Add(nameof(lang), lang);
            }

            ViewBag.Filter = filter;

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
                        UserName = a.u == null
                                 ? null
                                 : string.IsNullOrEmpty(a.u.NickName)
                                 ? a.u.UserName
                                 : a.u.NickName
                    })
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage);

            var list = await query.ToListAsync();
            return View(list);
        }

        [HttpGet("{jid}/{rid}/{type}")]
        [Authorize(Roles = problemRole)]
        public IActionResult RunDetails(int jid, int rid, string type)
        {
            var io = HttpContext.RequestServices.GetRequiredService<IFileRepository>();
            io.SetContext("Runs");
            if (!io.ExistPart($"j{jid}", $"r{rid}.{type}")) return NotFound();
            return ContentFile($"Runs/j{jid}/r{rid}.{type}", "application/octet-stream", $"j{jid}_r{rid}.{type}");
        }
    }
}