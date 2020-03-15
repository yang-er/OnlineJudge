using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class JudgehostsController : Controller3
    {
        private Task AuditlogAsync(string id, string act)
        {
            DbContext.Auditlogs.Add(new Auditlog
            {
                Action = act,
                DataId = id,
                DataType = AuditlogType.Judgehost,
                Time = System.DateTimeOffset.Now,
                UserName = UserManager.GetUserName(User),
            });

            return DbContext.SaveChangesAsync();
        }


        [HttpGet]
        public async Task<IActionResult> List()
        {
            var hosts = await DbContext.JudgeHosts.ToListAsync();
            return View(hosts);
        }


        [HttpGet("{hostname}")]
        public async Task<IActionResult> Detail(string hostname)
        {
            var host = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();
            if (host is null) return NotFound();
            ViewBag.Host = host;

            ViewData["Count"] = await DbContext.Judgings
                .Where(g => g.Server == hostname)
                .CountAsync();

            ViewBag.Judgings = await DbContext.Judgings
                .Where(j => j.Server == hostname)
                .OrderByDescending(g => g.JudgingId)
                .Take(100)
                .ToListAsync();

            return View();
        }
        

        [HttpGet("{hostname}/{tobe}")]
        public async Task<IActionResult> Toggle(string hostname, string tobe)
        {
            bool active = tobe == "activate";
            if (!active && tobe != "deactivate") return NotFound();

            var affected = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .BatchUpdateAsync(h => new JudgeHost { Active = active });
            if (affected == 0) return NotFound();

            await AuditlogAsync(hostname, $"mark {(active ? "" : "in")}active");
            return RedirectToAction(nameof(List));
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateAll()
        {
            await DbContext.JudgeHosts
                .BatchUpdateAsync(jh => new JudgeHost { Active = true });
            await AuditlogAsync(null, "marked all active");
            return RedirectToAction(nameof(List));
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAll()
        {
            await DbContext.JudgeHosts
                .BatchUpdateAsync(jh => new JudgeHost { Active = false });
            await AuditlogAsync(null, "marked all inactive");
            return RedirectToAction(nameof(List));
        }
    }
}
