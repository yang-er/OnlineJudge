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
        public IActionResult Index() => View();


        [HttpGet("[action]")]
        public async Task<IActionResult> Updates()
        {
            var judgehosts = await DbContext.JudgeHosts
                .Where(jh => jh.PollTime < DateTimeOffset.Now.AddSeconds(-120) && jh.Active)
                .CountAsync();
            var internal_error = await DbContext.InternalErrors
                .Where(ie => ie.Status == InternalErrorStatus.Open)
                .CountAsync();
            return Json(new { judgehosts, internal_error });
        }


        [HttpGet("[action]/{page?}")]
        public async Task<IActionResult> Auditlog(int page = 1)
        {
            if (page <= 0) return BadRequest();

            var query = await DbContext.Auditlogs
                .Where(a => a.ContestId == null)
                .OrderByDescending(a => a.LogId)
                .Skip((page - 1) * 1000)
                .Take(1000)
                .ToListAsync();

            var tot = await DbContext.Auditlogs
                .Where(a => a.ContestId == null)
                .CountAsync();

            ViewBag.Page = page;
            return View((query, (tot + 999) / 1000));
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Config()
        {
            var model = await DbContext.Configures
                .Where(c => c.Public >= 0)
                .ToListAsync();
            return View(model);
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Config(ConfigureEditModel models)
        {
            var items = await DbContext.Configures
                .Where(c => c.Public >= 0)
                .ToListAsync();
            var now = DateTimeOffset.Now;
            
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

                        DbContext.Auditlogs.Add(new Auditlog
                        {
                            DataType = AuditlogType.Configuration,
                            Time = now,
                            DataId = item.Name,
                            Action = "updated",
                            UserName = UserManager.GetUserName(User),
                        });
                    }
                }
            }

            await DbContext.SaveChangesAsync();
            StatusMessage = "Configurations saved successfully.";
            return RedirectToAction(nameof(Config));
        }
    }
}
