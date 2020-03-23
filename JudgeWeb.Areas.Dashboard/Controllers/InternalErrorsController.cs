using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    [AuditPoint(AuditlogType.InternalError)]
    public class InternalErrorsController : Controller3
    {
        private IInternalErrorStore Store { get; }

        private IJudgementFacade Facade { get; }

        private ILogger<IJudgementFacade> Logger { get; }

        public InternalErrorsController(IJudgementFacade facade)
        {
            Facade = facade;
            Store = facade.InternalErrorStore;
            Logger = facade.Logger;
        }


        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View(await Store.ListAsync());
        }

        
        [HttpGet("{eid}/{todo}")]
        public async Task<IActionResult> Mark(int eid, string todo,
            [FromServices] IProblemFacade problems)
        {
            var ie = await Store.FindAsync(eid);
            if (ie is null) return NotFound();

            if (ie.Status == InternalErrorStatus.Open)
            {
                var nstat = todo == "resolve"
                          ? InternalErrorStatus.Resolved
                          : todo == "ignore"
                          ? InternalErrorStatus.Ignored
                          : InternalErrorStatus.Open;
                if (nstat == InternalErrorStatus.Open)
                    return NotFound();

                var toDisable = await Store.ResolveAsync(ie, nstat);

                if (toDisable != null)
                {
                    var kind = toDisable.kind;

                    if (kind == "language")
                    {
                        await problems.Languages.UpdateAsync(
                            predicate: l => l.Id == toDisable.langid,
                            update: l => new Language { AllowJudge = true });
                    }
                    else if (kind == "judgehost")
                    {
                        await Facade.Judgehosts.ToggleAsync(toDisable.hostname, true);
                    }
                    else if (kind == "problem")
                    {
                        await problems.Problems.ToggleAsync(
                            pid: toDisable.probid.Value,
                            expression: p => p.AllowJudge, tobe: true);
                    }
                }

                await HttpContext.AuditAsync($"mark as {todo}d", $"{eid}");
            }

            return RedirectToAction(nameof(Detail), new { eid });
        }


        [HttpGet("{eid}")]
        public async Task<IActionResult> Detail(int eid)
        {
            var ie = await Store.FindAsync(eid);
            if (ie is null) return NotFound();
            return View(ie);
        }
    }
}
