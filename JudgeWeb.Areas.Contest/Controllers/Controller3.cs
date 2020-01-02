using JudgeWeb.Areas.Contest.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    public class Controller3 : Controller2
    {
        protected ContestManager Service { get; private set; }

        protected UserManager UserManager { get; private set; }

        protected Data.Contest Contest { get; private set; }

        [TempData]
        public string DisplayMessage { get; set; }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // check the contest info
            if (!context.RouteData.Values.TryGetValue("cid", out var __cid)
                || !int.TryParse(__cid.ToString(), out int cid))
            {
                context.Result = BadRequest();
                return;
            }

            // parse the base service
            Service = HttpContext.RequestServices
                .GetRequiredService<ContestManager>();
            UserManager = HttpContext.RequestServices
                .GetRequiredService<UserManager>();

            Service.AuditlogUserName = UserManager.GetUserName(User);

            // check the existence
            Contest = await Service.GetContestAsync(cid);
            if (Contest == null)
            {
                context.Result = NotFound();
                return;
            }

            // check the permission
            if (User.IsInRoles($"Administrator,JuryOfContest{Contest.ContestId}"))
            {
                ViewData["IsJury"] = true;
            }

            if (int.TryParse(UserManager?.GetUserId(User) ?? "-1", out int uid) && uid > 0)
            {
                var team = await Service.FindTeamByUserAsync(Contest.ContestId, uid);
                ViewBag.Team = team;
                ViewData["HasTeam"] = true;
            }

            await OnActionExecutingAsync(context);
            ViewBag.Contest = Contest;
            ViewData["ContestId"] = cid;

            if (context.Result == null)
                await OnActionExecutedAsync(await next());
        }

        [NonAction]
        public virtual Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            OnActionExecuting(context);
            return Task.CompletedTask;
        }

        [NonAction]
        public virtual Task OnActionExecutedAsync(ActionExecutedContext context)
        {
            OnActionExecuted(context);
            return Task.CompletedTask;
        }
    }
}
