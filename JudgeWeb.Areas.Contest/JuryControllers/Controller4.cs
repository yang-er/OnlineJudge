using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Authorize]
    public abstract class JuryControllerBase : Controller3
    {
        public override Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            // check the permission
            if (!ViewData.ContainsKey("IsJury"))
            {
                context.Result = Forbid();
                return Task.CompletedTask;
            }

            ViewData["InJury"] = true;
            return base.OnActionExecutingAsync(context);
        }

        protected new IActionResult NotFound() => ExplicitNotFound();

        protected IActionResult GoBackHome(string str)
        {
            StatusMessage = str;
            return RedirectToAction("Home", "Jury");
        }

        protected async Task<IEnumerable<ListSubmissionModel>> ListSubmissionsByJuryAsync(
            int cid, int? teamid = null, bool all = true)
        {
            Expression<Func<Submission, bool>> cond =
                s => s.ContestId == cid;
            if (teamid.HasValue)
                cond = cond.Combine(s => s.Author == teamid);
            int? limit = all ? default(int?) : 75;

            ViewBag.TeamNames = await Facade.Teams.ListNamesAsync(cid);
            return await HttpContext.RequestServices
                .GetRequiredService<ISubmissionStore>()
                .ListWithJudgingAsync(cond, true, limit);
        }
    }
}
