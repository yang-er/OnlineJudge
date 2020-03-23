using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    [AuditPoint(AuditlogType.Judgehost)]
    public class JudgehostsController : Controller3
    {
        private IJudgehostStore Store { get; }
        public JudgehostsController(IJudgementFacade facade)
            => Store = facade.JudgehostStore;


        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View(await Store.ListAsync());
        }


        [HttpGet("{hostname}")]
        public async Task<IActionResult> Detail(string hostname)
        {
            var host = await Store.FindAsync(hostname);
            if (host is null) return NotFound();
            ViewBag.Host = host;
            ViewBag.Count = await Store.CountJudgingsAsync(hostname);
            ViewBag.Judgings = await Store.FetchJudgingsAsync(hostname, 100);
            return View();
        }
        

        [HttpGet("{hostname}/{tobe}")]
        public async Task<IActionResult> Toggle(string hostname, string tobe)
        {
            bool active = tobe == "activate";
            if (!active && tobe != "deactivate") return NotFound();

            var affected = await Store.ToggleAsync(hostname, active);
            if (affected == 0) return NotFound();

            await HttpContext.AuditAsync($"mark {(active ? "" : "in")}active", hostname);
            return RedirectToAction(nameof(List));
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateAll()
        {
            await Store.ToggleAsync(null, true);
            await HttpContext.AuditAsync("marked all active", null);
            return RedirectToAction(nameof(List));
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAll()
        {
            await Store.ToggleAsync(null, false);
            await HttpContext.AuditAsync("marked all inactive", null);
            return RedirectToAction(nameof(List));
        }
    }
}
