using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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

        public override async Task OnActionExecutedAsync(ActionExecutedContext context)
        {
            await base.OnActionExecutedAsync(context);

            if (context.Result is ViewResult && !InAjax)
            {
                ViewBag.ucc = await Service.CountUnansweredClarificationAsync(Contest.ContestId);
                ViewBag.ptc = await Service.CountPendingTeamAsync(Contest.ContestId);
            }
        }
    }
}
