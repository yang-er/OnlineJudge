using JudgeWeb.Areas.Contest.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    public class ApiControllerBase : ControllerBase, IAsyncActionFilter
    {
        protected AppDbContext DbContext { get; private set; }

        public Data.Contest Contest { get; private set; }

        protected ContestManager Service { get; private set; }

        [NonAction]
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            Service = context.HttpContext.RequestServices
                .GetRequiredService<ContestManager>();

            context.Result = NotFound();
            if (context.RouteData.Values.TryGetValue("cid", out object __cid)
                && int.TryParse((string)__cid, out int cid))
            {
                Contest = await Service.GetContestAsync(cid);

                if (Contest != null)
                {
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
}
