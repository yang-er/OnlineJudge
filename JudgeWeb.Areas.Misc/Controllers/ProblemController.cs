using JudgeWeb.Areas.Misc.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    [Route("[controller]")]
    public partial class ProblemController : Controller2
    {
        const int ItemsPerPage = 50;

        private SubmissionManager SubmissionManager { get; }

        private UserManager UserManager { get; }

        private AppDbContext DbContext { get; }

        [TempData]
        public string StatusMessage { get; set; }

        public ProblemController(AppDbContext db, SubmissionManager sm, UserManager um)
        {
            SubmissionManager = sm;
            DbContext = db;
            UserManager = um;
        }

        private Task<List<SelectListItem>> LanguagesAsync()
        {
            return DbContext.Languages
                .Where(t => t.AllowSubmit)
                .Select(l => new SelectListItem(l.Name, l.Id))
                .CachedToListAsync("prob::oklang", System.TimeSpan.FromMinutes(5));
        }

        private Task<int> MaxPageAsync()
        {
            return DbContext.CachedGetAsync("prob::totcount", System.TimeSpan.FromMinutes(10), async () =>
            {
                var pid = await DbContext.Archives
                    .OrderByDescending(p => p.PublicId)
                    .Select(p => new { p.PublicId })
                    .FirstOrDefaultAsync();
                return ((pid?.PublicId ?? 1000) - 1000) / ItemsPerPage + 1;
            });
        }


        /// <summary>
        /// 题目列表的页面。
        /// </summary>
        /// <param name="pg">页面编号</param>
        [HttpGet("/[controller]s")]
        public async Task<IActionResult> List(int page = 1)
        {
            if (page < 1) page = 1;
            ViewBag.Page = page;
            ViewBag.TotalPage = await MaxPageAsync();

            var probsQuery =
                from a in DbContext.Archives
                where a.PublicId <= 1000 + page * ItemsPerPage
                    && a.PublicId > 1000 + (page - 1) * ItemsPerPage
                join p in DbContext.Problems on a.ProblemId equals p.ProblemId
                select new ProblemArchive(a, p.Title, p.Source);

            ViewBag.Stat = await SubmissionManager
                .StatisticsByUserAsync(int.Parse(UserManager.GetUserId(User) ?? "-100"));
            return View(await probsQuery.ToListAsync());
        }


        /// <summary>
        /// 展示某一个题目。
        /// </summary>
        /// <param name="pid">题目编号</param>
        [HttpGet("{pid}")]
        public async Task<IActionResult> View(int pid,
            [FromServices] IProblemFileRepository ioContext)
        {
            var probQuery =
                from a in DbContext.Archives
                where a.PublicId == pid
                join p in DbContext.Problems on a.ProblemId equals p.ProblemId
                select new { p.Title, p.Source, p.ProblemId, a.TagName };

            var prob = await probQuery.SingleOrDefaultAsync();
            if (prob == null) return NotFound();

            var fileInfo = ioContext.GetFileInfo($"p{prob.ProblemId}/view.html");
            var view = await fileInfo.ReadAsync();

            if (string.IsNullOrEmpty(view))
            {
                StatusMessage = "Error no descriptions avaliable now.";
                return RedirectToAction(nameof(List));
            }

            ViewData["Content"] = view;
            ViewData["Id"] = pid;
            ViewData["Source"] = prob.Source;
            ViewData["Tag"] = prob.TagName;
            ViewData["ReadId"] = prob.ProblemId;
            var uid = int.Parse(UserManager.GetUserId(User) ?? "-100");

            var subQuery =
                from s in SubmissionManager.Submissions
                where s.ProblemId == prob.ProblemId
                where s.Author == uid && s.ContestId == 0
                join j in SubmissionManager.Judgings
                    on new { s.SubmissionId, Active = true }
                    equals new { j.SubmissionId, j.Active }
                select new { s, j };
            var subs = await subQuery
                .OrderByDescending(a => a.s.SubmissionId)
                .Take(9)
                .ToListAsync();
            ViewBag.Subs = subs.Select(a => (a.s, a.j));

            return View();
        }


        [HttpGet("{pid}/[action]")]
        [ValidateInAjax]
        [Authorize]
        public async Task<IActionResult> Submissions(int pid)
        {
            var probQuery =
                from a in DbContext.Archives
                where a.PublicId == pid
                join p in DbContext.Problems on a.ProblemId equals p.ProblemId
                select new { p.Title, p.Source, p.ProblemId, a.TagName };

            var prob = await probQuery.SingleOrDefaultAsync();
            if (prob == null) return NotFound();
            var uid = int.Parse(UserManager.GetUserId(User) ?? "-100");

            var subQuery =
                from s in SubmissionManager.Submissions
                where s.ProblemId == prob.ProblemId
                where s.Author == uid && s.ContestId == 0
                join j in SubmissionManager.Judgings
                    on new { s.SubmissionId, Active = true }
                    equals new { j.SubmissionId, j.Active }
                select new { s, j };
            var subs = await subQuery
                .OrderByDescending(a => a.s.SubmissionId)
                .ToListAsync();
            ViewBag.Lang = await DbContext.Languages
                .ToDictionaryAsync(k => k.Id, v => v.Id);
            return View(subs.Select(a => (a.s, a.j)));
        }


        [HttpGet("{pid}/[action]/{sid}")]
        [ValidateInAjax]
        [Authorize]
        public async Task<IActionResult> Submission(int pid, int sid)
        {
            var probQuery =
                from a in DbContext.Archives
                where a.PublicId == pid
                join p in DbContext.Problems on a.ProblemId equals p.ProblemId
                select new { p.Title, p.Source, p.ProblemId, a.TagName };

            var prob = await probQuery.SingleOrDefaultAsync();
            if (prob == null) return NotFound();
            var uid = int.Parse(UserManager.GetUserId(User) ?? "-100");

            var subQuery =
                from s in DbContext.Submissions
                where s.ProblemId == prob.ProblemId
                where s.ContestId == 0 && s.Author == uid
                where s.SubmissionId == sid
                join j in DbContext.Judgings
                    on new { s.SubmissionId, Active = true }
                    equals new { j.SubmissionId, j.Active }
                join l in DbContext.Languages
                    on s.Language equals l.Id
                select new CodeViewModel
                {
                    CompileError = j.CompileError,
                    CodeLength = s.CodeLength,
                    ExecuteMemory = j.ExecuteMemory,
                    ExecuteTime = j.ExecuteTime,
                    Code = s.SourceCode,
                    FileExtensions = l.FileExtension,
                    LanguageName = l.Name,
                    Status = j.Status,
                    JudgingId = j.JudgingId,
                    SubmissionId = s.SubmissionId,
                    DateTime = s.Time,
                };

            var sub = await subQuery.SingleOrDefaultAsync();
            if (sub == null) return NotFound();
            sub.ProblemTitle = prob.Title;

            var detailQuery =
                from t in DbContext.Testcases
                where t.ProblemId == prob.ProblemId
                join d in DbContext.Details
                    on new { t.TestcaseId, sub.JudgingId }
                    equals new { d.TestcaseId, d.JudgingId }
                    into dd
                from d in dd.DefaultIfEmpty()
                select new { t, d };
            var details = await detailQuery.ToListAsync();
            sub.Details = details.Select(a => (a.t, a.d));
            return Window(sub);
        }


        /// <summary>
        /// 展示提交代码的页面。
        /// </summary>
        /// <param name="pid">题目编号</param>
        [HttpGet("{pid}/[action]")]
        [ValidateInAjax]
        [Authorize]
        public async Task<IActionResult> Submit(int pid)
        {
            var probQuery =
                from a in DbContext.Archives
                where a.PublicId == pid
                join p in DbContext.Problems on a.ProblemId equals p.ProblemId
                select new { p.Title, p.AllowSubmit };

            var prob = await probQuery.SingleOrDefaultAsync();
            if (prob == null) return NotFound();

            if (!prob.AllowSubmit)
                return Message(
                    title: $"Submit Problem {pid}",
                    message: $"Problem {pid} is not allowed for submitting.",
                    MessageType.Danger);

            ViewBag.ProblemTitle = prob.Title;
            ViewBag.Language = await LanguagesAsync();

            return Window(new CodeSubmitModel
            {
                Code = "",
                Language = "cpp",
                ProblemId = pid,
            });
        }


        /// <summary>
        /// 提交代码并存入数据库。
        /// </summary>
        /// <param name="pid">问题编号</param>
        /// <param name="model">代码视图模型</param>
        [HttpPost("{pid}/[action]")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int pid, CodeSubmitModel model,
            [FromServices] SubmissionManager subMgr)
        {
            if (model.ProblemId != pid) return BadRequest();

            // check user blocking
            if (User.IsInRole("Blocked"))
                ModelState.AddModelError("xys::blocked",
                    "You are not permitted to submit code.");

            // check problem submit
            var probQuery =
                from a in DbContext.Archives
                where a.PublicId == pid
                join p in DbContext.Problems on a.ProblemId equals p.ProblemId
                select new { p.Title, p.Source, p.ProblemId, p.AllowSubmit };
            var prob = await probQuery.SingleOrDefaultAsync();
            if (prob == null) return NotFound();

            if (!prob.AllowSubmit)
            {
                StatusMessage = $"Problem {pid} is not allowed for submitting.";
                return RedirectToAction(nameof(View));
            }

            // check language blocking
            var lang = await DbContext.Languages
                .Where(l => l.Id == model.Language)
                .SingleOrDefaultAsync();
            if (lang == null)
                ModelState.AddModelError("lang::notfound",
                    "Language is not found.");
            else if (!lang.AllowSubmit)
                ModelState.AddModelError("lang::notallow",
                    "You can't submit solutions with this language.");

            if (ModelState.ErrorCount > 0)
            {
                ViewBag.ProblemTitle = prob.Title;
                ViewBag.Language = await LanguagesAsync();
                return View(model);
            }
            else
            {
                var sub = await subMgr.CreateAsync(
                    code: model.Code,
                    langid: lang,
                    probid: prob.ProblemId,
                    cid: null,
                    uid: int.Parse(UserManager.GetUserId(User)),
                    ipAddr: HttpContext.Connection.RemoteIpAddress,
                    via: "problem-list",
                    username: UserManager.GetUserName(User));

                int id = sub.SubmissionId;

                return RedirectToAction(nameof(View));
            }
        }
    }
}
