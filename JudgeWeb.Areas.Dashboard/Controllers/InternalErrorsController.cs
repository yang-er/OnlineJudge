using EFCore.BulkExtensions;
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
    [Route("[area]/[controller]")]
    public class InternalErrorsController : Controller3
    {
        private Task AuditlogAsync(int eid, string act)
        {
            DbContext.Add(new Auditlog
            {
                Action = act,
                DataId = $"{eid}",
                DataType = AuditlogType.InternalError,
                UserName = UserManager.GetUserName(User),
                Time = System.DateTimeOffset.Now,
            });

            return DbContext.SaveChangesAsync();
        }


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
                DbContext.InternalErrors.Update(ie);
                await DbContext.SaveChangesAsync();

                if (ie.Status == InternalErrorStatus.Resolved)
                {
                    var toDisable = ie.Disabled.AsJson<Api.Models.InternalErrorDisable>();
                    var kind = toDisable.kind;

                    if (kind == "language")
                    {
                        var langid = toDisable.langid;
                        var lang = await DbContext.Languages
                            .Where(l => l.Id == langid)
                            .BatchUpdateAsync(l => new Language { AllowJudge = true });
                    }
                    else if (kind == "judgehost")
                    {
                        var hostname = toDisable.hostname;
                        var host = await DbContext.JudgeHosts
                            .Where(h => h.ServerName == hostname)
                            .BatchUpdateAsync(h => new JudgeHost { Active = true });
                    }
                    else if (kind == "problem")
                    {
                        var probid = toDisable.probid.Value;
                        var prob = await DbContext.Problems
                            .Where(p => p.ProblemId == probid)
                            .BatchUpdateAsync(p => new Problem { AllowJudge = true });
                    }
                }

                await AuditlogAsync(eid, $"mark as {todo}d");
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
