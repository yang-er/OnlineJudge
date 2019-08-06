using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    [Area("Judge")]
    [Route("[area]/[controller]/[action]")]
    public partial class ProblemController : Controller2
    {
        const string privilege = "Administrator,Problem";
        const int ItemsPerPage = 50;

        private UserManager UserManager { get; }
        private TestcaseManager TestcaseManager { get; }
        private ProblemManager ProblemManager { get; }

        public ProblemController(ProblemManager ctx, UserManager um, TestcaseManager tm)
        {
            ProblemManager = ctx;
            UserManager = um;
            TestcaseManager = tm;
        }

        /// <summary>
        /// 题目列表的页面。
        /// </summary>
        /// <param name="pg">页面编号</param>
        [HttpGet("{pg?}")]
        public async Task<IActionResult> List(int pg = 1)
        {
            if (pg < 1) pg = 1;
            ViewBag.Page = pg;
            ViewBag.TotalPage = ProblemManager.TotalPages;

            var probs = await ProblemManager
                .ListAsync(pg, User.IsInRoles(privilege));

            int uid = int.Parse(UserManager.GetUserId(User) ?? "-1");
            ViewBag.Statistics = ProblemManager.StatisticsByUser(uid,
                grouping: p => p.ProblemId,
                filter: p => p.ProblemId < 1000 + pg * ItemsPerPage
                    && p.ProblemId >= 1000 + (pg - 1) * ItemsPerPage
                    && p.ContestId == 0);

            return View(probs);
        }

        /// <summary>
        /// 展示某一个题目。
        /// </summary>
        /// <param name="pid">题目编号</param>
        [HttpGet("{pid}")]
        public async Task<IActionResult> View(int pid)
        {
            var prob = await ProblemManager.TitleFlagAsync(pid);

            if (!prob.HasValue || prob.Value.Flag != 0 && !User.IsInRoles(privilege))
                return NotFound(); // No such problem or not visible.
            var view = await ProblemManager.GetViewAsync(pid);

            if (string.IsNullOrEmpty(view)) return NotFound();

            ViewData["Title"] = "Problem View";
            ViewData["Content"] = view;
            ViewData["Id"] = pid;
            return View();
        }

        /// <summary>
        /// 展示提交代码的页面。
        /// </summary>
        /// <param name="pid">题目编号</param>
        [HttpGet("{pid}")]
        [Authorize]
        public async Task<IActionResult> Submit(int pid,
            [FromServices] LanguageManager languageManager)
        {
            var prob = await ProblemManager.TitleFlagAsync(pid);
            if (!prob.HasValue) return NotFound();
            if (prob.Value.Flag != 0 && !User.IsInRoles(privilege)) return NotFound();

            ViewData["ProblemTitle"] = prob.Value.Title;
            ViewData["ProblemId"] = pid;
            ViewData["Title"] = "Submit Code";

            ViewBag.Language = languageManager.GetAll().Values
                .Where(kvp => kvp.AllowSubmit)
                .Select(t => new SelectListItem(t.Name, t.LangId.ToString()));
            ViewBag.DisplayMessage = default(string);

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
        [HttpPost("{pid}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int pid, CodeSubmitModel model,
            [FromServices] LanguageManager langMgr,
            [FromServices] SubmissionManager subMgr)
        {
            if (model.ProblemId != pid) return BadRequest();

            var prob = await ProblemManager.TitleFlagAsync(pid);
            if (!prob.HasValue) return NotFound();
            if (prob.Value.Flag != 0 && !User.IsInRoles(privilege)) return NotFound();

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
                int id = await subMgr.CreateAsync(model,
                    HttpContext.Connection.RemoteIpAddress,
                    int.Parse(UserManager.GetUserId(User)),
                    UserManager.GetUserName(User));

                return RedirectToAction("View", "Status", new { area = "Judge", id });
            }
        }
    }
}
