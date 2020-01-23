using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]")]
    public class RootController : Controller3
    {
        public RootController(AppDbContext db) : base(db) { }

        public IActionResult Index() => View();

        [HttpGet("[action]")]
        public async Task<IActionResult> Updates()
        {
            var judgehosts = await DbContext.JudgeHosts
                .Where(jh => jh.PollTime < DateTimeOffset.Now.AddSeconds(-120) && jh.Active)
                .CountAsync();
            var internal_error = await DbContext.InternalErrors
                .Where(ie => ie.Status == InternalError.ErrorStatus.Open)
                .CountAsync();
            return Json(new { judgehosts, internal_error });
        }

        [HttpGet("[action]/{page?}")]
        public async Task<IActionResult> Auditlog(int page = 1)
        {
            if (page <= 0) return BadRequest();

            var query = await DbContext.AuditLogs
                .Where(a => a.ContestId == 0)
                .OrderByDescending(a => a.LogId)
                .Skip((page - 1) * 1000)
                .Take(1000)
                .ToListAsync();

            var tot = await DbContext.AuditLogs
                .Where(a => a.ContestId == 0)
                .CountAsync();

            ViewBag.Page = page;
            return View((query, (tot + 999) / 1000));
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Config()
        {
            return View(await DbContext.Configures.ToListAsync());
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Config(ConfigureEditModel models)
        {
            var items = await DbContext.Configures.ToListAsync();
            
            foreach (var item in items)
            {
                if (models.Config.ContainsKey(item.Name)
                    && models.Config[item.Name] != null)
                {
                    var newVal = models.Config[item.Name];
                    if (item.Type == "string") newVal = newVal.ToJson();
                    if (newVal != item.Value)
                    {
                        item.Value = newVal;
                        DbContext.Configures.Update(item);
                    }
                }
            }

            await DbContext.SaveChangesAsync();
            StatusMessage = "Configurations saved successfully.";
            return RedirectToAction(nameof(Config));
        }
    }
}
