using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/[controller]")]
    public class AnalysisController : JuryControllerBase
    {
        private ISubmissionStore Store { get; }

        public AnalysisController(ISubmissionStore store)
        {
            Store = store;
        }

        public Dictionary<int, (string name, string aff)> Teams { get; set; }


        public override async Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            await base.OnActionExecutingAsync(context);
            if (context.Result == null && Contest.GetState() == ContestState.NotScheduled)
            {
                context.Result = StatusCodePage(410);
                return;
            }
            
            var time = Contest.EndTime - Contest.StartTime;
            if (time.HasValue && time.Value.TotalMilliseconds >= 14400)
            {
                context.Result = StatusCodePage(503);
                return;
            }

            var lst = await Facade.Teams.ListAsync(Contest.ContestId,
                selector: t => new { t.TeamName, t.TeamId, t.Affiliation.ExternalId },
                predicate: t => t.Category.IsPublic,
                cacheTag: ($"cid`c{Contest.ContestId}`analysis", TimeSpan.FromMinutes(1)));
            Teams = lst.ToDictionary(a => a.TeamId, a => (a.TeamName, a.ExternalId));
            ViewBag.TeamNames2 = Teams;
        }


        [HttpGet]
        public async Task<IActionResult> Overview()
        {
            return View(await AnalysisOneModel.AnalysisAsync(Store, Contest, Teams));
        }


        [HttpGet("[action]/{pid}")]
        public async Task<IActionResult> Problem(int pid)
        {
            var prob = Problems.Find(pid);
            if (prob == null) return NotFound();
            return View(await AnalysisTwoModel.AnalysisAsync(Store, Contest, prob, Teams));
        }
    }
}
