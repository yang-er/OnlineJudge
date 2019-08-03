using EntityFrameworkCore.Cacheable;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    [Area("Judge")]
    [Route("[area]/[controller]/[action]")]
    public partial class ProblemController : Controller2
    {
        const int itemsPerPage = 50;
        const string privilege = "Administrator,Problem";

        private AppDbContext DbContext { get; }
        private UserManager UserManager { get; }
        private IFileRepository IoContext { get; }

        public ProblemController(AppDbContext jdbc, UserManager um, IFileRepository pe)
        {
            DbContext = jdbc;
            UserManager = um;
            IoContext = pe;
            pe.SetContext("Problems");
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

            var list = DbContext.Problems
                .OrderByDescending(p => p.ProblemId)
                .Select(p => new { p.ProblemId })
                .Cacheable(System.TimeSpan.FromMinutes(5))
                .FirstOrDefault();
            ViewBag.TotalPage = ((list?.ProblemId ?? 1000) - 1000) / itemsPerPage + 1;

            var query = DbContext.Problems
                .Where(p => p.ProblemId < 1000 + pg * itemsPerPage
                    && p.ProblemId >= 1000 + (pg - 1) * itemsPerPage);
            if (!User.IsInRoles(privilege))
                query = query.Where(p => p.Flag == 0);
            var probs = await query.OrderBy(p => p.ProblemId).ToListAsync();

            int uid = int.Parse(UserManager.GetUserId(User) ?? "-1");
            ViewBag.Statistics = DbContext.SubmissionStatistics
                .Where(p => p.ProblemId < 1000 + pg * itemsPerPage
                    && p.ProblemId >= 1000 + (pg - 1) * itemsPerPage
                    && p.ContestId == 0)
                .GroupBy(p => p.ProblemId)
                .ToDictionary(
                    keySelector: g => g.Key,
                    elementSelector: g => (
                        g.Sum(s => s.AcceptedSubmission),
                        g.Sum(s => s.TotalSubmission),
                        g.Sum(s => s.Author == uid ? s.TotalSubmission : 0),
                        g.Sum(s => s.Author == uid ? s.AcceptedSubmission : 0)));

            return View(probs);
        }

        /// <summary>
        /// 展示某一个题目。
        /// </summary>
        /// <param name="pid">题目编号</param>
        [HttpGet("{pid}")]
        public async Task<IActionResult> View(int pid)
        {
            var prob = await DbContext.Problems
                .Where(p => p.ProblemId == pid)
                .Select(p => new { p.Flag })
                .FirstOrDefaultAsync();

            if (prob is null || prob.Flag != 0 && !User.IsInRoles(privilege))
                return NotFound(); // No such problem or not visible.
            var view = await IoContext.ReadPartAsync($"p{pid}", $"view.html");

            if (!string.IsNullOrEmpty(view))
            {
                ViewData["Title"] = "Problem View";
                ViewData["Content"] = view;
                ViewData["Id"] = pid;
                return View();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
