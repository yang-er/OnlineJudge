using JudgeWeb.Data;
using EFCore.BulkExtensions;
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
        public JudgehostsController(AppDbContext db) : base(db) { }

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
                .Where(g => g.ServerId == host.ServerId)
                .CountAsync();

            ViewBag.Judgings = await DbContext.Judgings
                .Where(j => j.ServerId == host.ServerId)
                .OrderByDescending(g => g.JudgingId)
                .Take(100)
                .ToListAsync();

            return View();
        }
        
        [HttpGet("{hostname}/{tobe}")]
        public async Task<IActionResult> Toggle(string hostname, string tobe)
        {
            var cur = await DbContext.JudgeHosts
                .Where(l => l.ServerName == hostname)
                .FirstOrDefaultAsync();
            if (cur == null) return NotFound();

            bool active = tobe == "activate";
            if (!active && tobe != "deactivate") return NotFound();
            cur.Active = active;
            DbContext.JudgeHosts.Update(cur);
            await DbContext.SaveChangesAsync();

            return RedirectToAction(nameof(List));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateAll()
        {
            await DbContext.JudgeHosts
                .BatchUpdateAsync(jh => new JudgeHost { Active = true });
            return RedirectToAction(nameof(List));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAll()
        {
            await DbContext.JudgeHosts
                .BatchUpdateAsync(jh => new JudgeHost { Active = false });
            return RedirectToAction(nameof(List));
        }
    }
}
