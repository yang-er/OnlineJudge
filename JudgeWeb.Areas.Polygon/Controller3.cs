using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    public abstract class Controller3 : Controller2
    {
        protected IProblemFacade Facade { get; private set; }

        public new Problem Problem { get; set; }

        protected new IActionResult NotFound() => ExplicitNotFound();

        private async Task<IActionResult> ValidateAsync()
        {
            if (!RouteData.Values.TryGetValue("pid", out var pid))
                return base.NotFound();
            if (!User.IsInRoles("Administrator,AuthorOfProblem" + (string)pid))
                return Forbid();
            if (!int.TryParse((string)pid, out int ppid))
                return base.NotFound();
            Problem = await Facade.Problems.FindAsync(ppid);
            return Problem == null
                ? base.NotFound() : null;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            Facade = context.HttpContext.RequestServices
                .GetRequiredService<IProblemFacade>();
            context.Result = await ValidateAsync();
            await base.OnActionExecutionAsync(context, next);
        }
    }
}
