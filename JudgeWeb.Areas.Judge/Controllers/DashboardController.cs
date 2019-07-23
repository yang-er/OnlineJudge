using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternalErrorStatus = JudgeWeb.Data.InternalError.ErrorStatus;

namespace JudgeWeb.Areas.Judge.Controllers
{
    [Area("Judge")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]/[action]")]
    public class DashboardController : Controller2
    {
        private AppDbContext DbContext { get; }

        public DashboardController(AppDbContext adbc)
        {
            DbContext = adbc;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Toggle(string @as, string aj, string jh)
        {
            if (@as != null)
            {
                var cur = await DbContext.Languages
                    .Where(l => l.ExternalId == @as)
                    .FirstOrDefaultAsync();

                if (cur != null)
                {
                    cur.AllowSubmit = !cur.AllowSubmit;
                    DbContext.Languages.Update(cur);
                }
            }
            else if (aj != null)
            {
                var cur = await DbContext.Languages
                    .Where(l => l.ExternalId == aj)
                    .FirstOrDefaultAsync();

                if (cur != null)
                {
                    cur.AllowJudge = !cur.AllowJudge;
                    DbContext.Languages.Update(cur);
                }
            }
            else if (jh != null)
            {
                var cur = await DbContext.JudgeHosts
                    .Where(l => l.ServerName == jh)
                    .FirstOrDefaultAsync();

                if (cur != null)
                {
                    cur.Active = !cur.Active;
                    DbContext.JudgeHosts.Update(cur);
                }
            }
            else
            {
                return BadRequest();
            }

            await DbContext.SaveChangesAsync();
            return RedirectToAction(jh is null ? nameof(Language) : nameof(JudgeHost));
        }

        public async Task<IActionResult> Executable()
        {
            if (!IsWindowAjax)
            {
                var execs = await DbContext.Executable
                    .Select(e => new Executable
                    {
                        ExecId = e.ExecId,
                        Description = e.Description,
                        Md5sum = e.Md5sum,
                        Type = e.Type,
                        ZipSize = e.ZipSize,
                    })
                    .ToListAsync();

                return View(execs);
            }
            else
            {
                return Window("ExecutableUpload", new ExecutableUploadModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Executable(ExecutableUploadModel model)
        {
            if (string.IsNullOrEmpty(model.ID))
                return BadRequest();
            var exec = await DbContext.Executable
                .Where(e => e.ExecId == model.ID)
                .FirstOrDefaultAsync();

            if (exec is null)
            {
                if (string.IsNullOrWhiteSpace(model.Description) || model.UploadContent is null)
                    return BadRequest();

                var uploaded = await model.UploadContent.ReadAsync();
                DbContext.Executable.Add(new Executable
                {
                    Md5sum = uploaded.Item2,
                    ZipFile = uploaded.Item1,
                    Description = model.Description,
                    ExecId = model.ID,
                    Type = model.Type,
                });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(model.Description))
                    exec.Description = model.Description.Trim();

                if (model.UploadContent != null)
                {
                    var uploaded = await model.UploadContent.ReadAsync();
                    exec.Md5sum = uploaded.Item2;
                    exec.ZipFile = uploaded.Item1;
                }

                DbContext.Executable.Update(exec);
            }

            await DbContext.SaveChangesAsync();
            return Message(
                "Executable Upload",
                $"Executable `{model.ID}` uploaded successfully.",
                MessageType.Success);
        }

        [HttpGet("{target}")]
        public async Task<IActionResult> Executable(string target)
        {
            var bytes = await DbContext.Executable
                .Where(e => e.ExecId == target)
                .Select(e => e.ZipFile)
                .FirstOrDefaultAsync();
            if (bytes is null) return NotFound();
            return File(bytes, "application/zip", $"{target}.zip", false);
        }

        public async Task<IActionResult> InternalError()
        {
            var model = await DbContext.InternalErrors
                .Select(e => new InternalError
                {
                    ErrorId = e.ErrorId,
                    Status = e.Status,
                    Time = e.Time,
                    Description = e.Description,
                })
                .OrderByDescending(e => e.ErrorId)
                .ToListAsync();

            return View("InternalErrors", model);
        }

        [HttpGet("{eid}")]
        public async Task<IActionResult> InternalError(int eid, string todo)
        {
            var ie = await DbContext.InternalErrors
                .Where(i => i.ErrorId == eid)
                .FirstOrDefaultAsync();
            if (ie is null) return NotFound();

            if (ie.Status == InternalErrorStatus.Open && todo != null)
            {
                ie.Status = todo == "resolve"
                          ? InternalErrorStatus.Resolved
                          : todo == "ignore"
                          ? InternalErrorStatus.Ignored
                          : InternalErrorStatus.Open;

                if (ie.Status != InternalErrorStatus.Open)
                {
                    DbContext.InternalErrors.Update(ie);
                    var toDisable = JObject.Parse(ie.Disabled);
                    var kind = toDisable["kind"].Value<string>();

                    if (kind == "language")
                    {
                        var langid = toDisable["langid"].Value<string>();
                        var lang = await DbContext.Languages
                            .Where(l => l.ExternalId == langid)
                            .FirstOrDefaultAsync();

                        if (lang != null)
                        {
                            lang.AllowJudge = true;
                            DbContext.Languages.Update(lang);
                        }
                    }
                    else if (kind == "judgehost")
                    {
                        var hostname = toDisable["hostname"].Value<string>();
                        var host = await DbContext.JudgeHosts
                            .Where(h => h.ServerName == hostname)
                            .FirstOrDefaultAsync();

                        if (host != null)
                        {
                            host.Active = true;
                            DbContext.JudgeHosts.Update(host);
                        }
                    }

                    await DbContext.SaveChangesAsync();
                }
            }

            return View(ie);
        }

        public async Task<IActionResult> JudgeHost()
        {
            var hosts = await DbContext.JudgeHosts.ToListAsync();
            return View("JudgeHosts", hosts);
        }

        [HttpGet("{hostname}")]
        public async Task<IActionResult> JudgeHost(string hostname)
        {
            var host = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();
            if (host is null) return NotFound();
            ViewBag.Host = host;

            var gradeQuery =
                from g in DbContext.Judgings
                where g.ServerId == host.ServerId
                orderby g.JudgingId descending
                select g;

            var detailQuery =
                from g in gradeQuery.Take(100)
                join d in DbContext.Details on g.JudgingId equals d.JudgingId into dd
                select new { g, dd = dd.ToList() };

            ViewBag.Judgings = (await detailQuery.ToListAsync())
                .Select(gg => (gg.g, gg.dd.AsEnumerable(), gg.g.SubmissionId));

            var counts = await DbContext.Judgings
                .Where(g => g.ServerId == host.ServerId)
                .CountAsync();
            ViewData["Count"] = counts;
            return View();
        }

        public async Task<IActionResult> Language()
        {
            ViewBag.Statistics = (await DbContext.Submissions
                .GroupBy(s => s.Language)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync()).Select(a => (a.Key, a.Count));
            return View("Languages", await DbContext.Languages.ToListAsync());
        }

        [HttpGet("{extid}")]
        public async Task<IActionResult> Language(string extid)
        {
            var lang = await DbContext.Languages
                .Where(l => l.ExternalId == extid)
                .FirstOrDefaultAsync();
            if (lang is null) return NotFound();
            ViewBag.Language = lang;

            var subs = await (
                from g in DbContext.Judgings
                where g.Active
                join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                where s.Language == lang.LangId
                orderby g.SubmissionId descending
                select new { s, g }
            ).Take(200).ToListAsync();
            ViewBag.Submissions = subs.Select(a => (a.s, a.g));
            return View();
        }
    }
}
