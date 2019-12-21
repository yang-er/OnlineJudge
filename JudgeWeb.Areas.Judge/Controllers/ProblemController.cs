using EntityFrameworkCore.Cacheable;
using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    [Area("Judge")]
    [Route("[controller]s")]
    public partial class ProblemController : Controller2
    {
        const int ItemsPerPage = 50;

        private ProblemManager ProblemManager { get; }

        private AppDbContext DbContext { get; }

        [TempData]
        public string StatusMessage { get; set; }

        public ProblemController(ProblemManager ctx, AppDbContext db)
        {
            ProblemManager = ctx;
            DbContext = db;
        }

        /// <summary>
        /// 题目列表的页面。
        /// </summary>
        /// <param name="pg">页面编号</param>
        [HttpGet("archive/{pg?}")]
        public async Task<IActionResult> List(int pg = 1)
        {
            var maxProb = await DbContext.Archives
                .OrderByDescending(a => a.PublicId)
                .Select(a => new { a.PublicId })
                .Cacheable(System.TimeSpan.FromMinutes(10))
                .FirstOrDefaultAsync();
            int tot2 = maxProb?.PublicId ?? 1000;
            tot2 = (tot2 - 1000) / ItemsPerPage + 1;

            if (pg < 1) pg = 1;
            ViewBag.Page = pg;
            ViewBag.TotalPage = tot2;

            var probsQuery =
                from a in DbContext.Archives
                where a.PublicId <= 1000 + pg * ItemsPerPage
                    && a.PublicId > 1000 + (pg - 1) * ItemsPerPage
                join p in DbContext.Problems on a.ProblemId equals p.ProblemId
                select new ProblemArchive(a.PublicId, p.Title, p.Source, a.TagName);

            return View(await probsQuery.ToListAsync());
        }

        /// <summary>
        /// 展示某一个题目。
        /// </summary>
        /// <param name="pid">题目编号</param>
        [HttpGet("{pid}")]
        public async Task<IActionResult> View(int pid,
            [FromServices] IFileRepository ioContext)
        {
            var probQuery =
                from a in DbContext.Archives
                where a.PublicId == pid
                join p in DbContext.Problems on a.ProblemId equals p.ProblemId
                select new { p.Title, p.Source, p.ProblemId };

            var prob = await probQuery.SingleOrDefaultAsync();
            if (prob == null) return NotFound();

            ioContext.SetContext("Problems");
            var view = await ioContext.ReadPartAsync($"p{prob.ProblemId}", "view.html");

            if (string.IsNullOrEmpty(view))
            {
                StatusMessage = "Error no descriptions avaliable now.";
                return RedirectToAction(nameof(List));
            }

            ViewData["Content"] = view;
            ViewData["Id"] = pid;
            return View();
        }

        /// <summary>
        /// 展示提交代码的页面。
        /// </summary>
        /// <param name="pid">题目编号</param>
        [HttpGet("{pid}/[action]")]
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
            {
                StatusMessage = $"Problem {pid} is not allowed for submitting.";
                return RedirectToAction(nameof(View));
            }

            ViewBag.ProblemTitle = prob.Title;

            ViewBag.Language = await DbContext.Languages
                .Where(t => t.AllowSubmit)
                .Select(l => new SelectListItem(l.Name, l.LangId.ToString()))
                .Cacheable(System.TimeSpan.FromMinutes(5))
                .ToListAsync();

            return View(new CodeSubmitModel
            {
                Code = "",
                Language = 2,
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
            [FromServices] LanguageManager langMgr,
            [FromServices] SubmissionManager subMgr,
            [FromServices] UserManager userMgr)
        {
            if (model.ProblemId != pid) return BadRequest();

            var prob = await ProblemManager.TitleFlagAsync(pid);
            if (!prob.HasValue) return NotFound();
            if (prob.Value.Flag != 0 && !User.IsInRoles("Administrator,AuthorOfProblem" + pid)) return NotFound();

            if (User.IsInRole("Blocked"))
                ModelState.AddModelError("xys::blocked",
                    "You are not permitted to submit code.");

            var lang = langMgr.Get(model.Language);
            if (lang == null)
                ModelState.AddModelError("lang::notfound",
                    "Language is not found.");
            else if (!lang.AllowSubmit)
                ModelState.AddModelError("lang::notallow",
                    "You can't submit solutions with this language.");

            if (ModelState.ErrorCount > 0)
            {
                ViewData["ProblemTitle"] = prob.Value.Title;
                ViewData["ProblemId"] = pid;
                ViewData["Title"] = "Submit Code";
                ViewBag.Language = langMgr.GetAll().Values
                    .Where(kvp => kvp.AllowSubmit)
                    .Select(t => new SelectListItem(t.Name, t.LangId.ToString()));
                ViewBag.DisplayMessage = ModelState.GetErrorStrings();

                return View(model);
            }
            else
            {
                var sub = await subMgr.CreateAsync(
                    code: model.Code,
                    langid: model.Language,
                    probid: model.ProblemId,
                    cid: 0,
                    uid: int.Parse(userMgr.GetUserId(User)),
                    ipAddr: HttpContext.Connection.RemoteIpAddress,
                    via: "problem-list",
                    username: userMgr.GetUserName(User));

                int id = sub.SubmissionId;

                return RedirectToAction("View", "Status", new { area = "Judge", id });
            }
        }
    }
}
