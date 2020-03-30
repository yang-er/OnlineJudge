using JudgeWeb.Domains.Contests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    public class ApiControllerBase : ControllerBase, IAsyncActionFilter
    {
        public Data.Contest Contest { get; private set; }

        public Data.ContestProblem[] Problems { get; private set; }

        public int MaxEventId { get; private set; }


        [NonAction]
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            context.Result = NotFound();
            if (context.RouteData.Values.TryGetValue("cid", out object __cid)
                && int.TryParse((string)__cid, out int cid))
            {
                var store = HttpContext.RequestServices
                    .GetRequiredService<IContestStore>();
                Contest = await store.FindAsync(cid);
                if (Contest == null || !Contest.StartTime.HasValue) return;
                Problems = await HttpContext.RequestServices
                    .GetRequiredService<IProblemsetStore>()
                    .ListAsync(cid);
                MaxEventId = await store.MaxEventIdAsync(cid);
                HttpContext.Items[nameof(cid)] = cid;
                
                context.Result = null;
                var executed = await next();
                if (executed.Result is ObjectResult objres)
                {
                    if (objres.Value == null)
                        objres.StatusCode = 404;
                }
            }
        }
    }
}
