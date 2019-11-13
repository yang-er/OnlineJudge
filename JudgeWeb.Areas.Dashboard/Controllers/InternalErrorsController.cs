using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;
using InternalErrorStatus = JudgeWeb.Data.InternalError.ErrorStatus;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class InternalErrorsController : Controller3
    {
        public InternalErrorsController(AppDbContext db) : base(db) { }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var model = await DbContext.InternalErrors
                .Select(
                    e => new InternalError
                    {
                        ErrorId = e.ErrorId,
                        Status = e.Status,
                        Time = e.Time,
                        Description = e.Description,
                    })
                .OrderByDescending(e => e.ErrorId)
                .ToListAsync();
            return View(model);
        }

        [HttpGet("{eid}/{todo}")]
        public async Task<IActionResult> Mark(int eid, string todo)
        {
            var ie = await DbContext.InternalErrors
                .Where(i => i.ErrorId == eid)
                .FirstOrDefaultAsync();
            if (ie is null) return NotFound();

            if (ie.Status == InternalErrorStatus.Open)
            {
                ie.Status = todo == "resolve"
                          ? InternalErrorStatus.Resolved
                          : todo == "ignore"
                          ? InternalErrorStatus.Ignored
                          : InternalErrorStatus.Open;
                if (ie.Status == InternalErrorStatus.Open)
                    return NotFound();

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

            return RedirectToAction(nameof(Detail), new { eid });
        }

        [HttpGet("{eid}")]
        public async Task<IActionResult> Detail(int eid)
        {
            var ie = await DbContext.InternalErrors
                .Where(i => i.ErrorId == eid)
                .FirstOrDefaultAsync();
            if (ie is null) return NotFound();
            return View(ie);
        }
    }
}
