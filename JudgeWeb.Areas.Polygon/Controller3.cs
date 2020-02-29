using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    public abstract class Controller3 : Controller2
    {
        internal static readonly string[] MarkdownFiles = new[]
        {
            "description",
            "inputdesc",
            "outputdesc",
            "hint",
            "interact"
        };

        protected AppDbContext DbContext { get; }

        [TempData]
        public string StatusMessage { get; set; }

        protected new IActionResult NotFound() => ExplicitNotFound();

        private bool ProblemLoad { get; }

        public Controller3(AppDbContext db, bool load)
        {
            DbContext = db;
            ProblemLoad = load;
        }

        public new Problem Problem { get; set; }

        private async Task<IActionResult> ValidateAsync()
        {
            if (ProblemLoad)
            {
                if (!RouteData.Values.TryGetValue("pid", out var pid))
                    return base.NotFound();
                if (!User.IsInRoles("Administrator,AuthorOfProblem" + (string)pid))
                    return Forbid();
                if (!int.TryParse((string)pid, out int ppid))
                    return base.NotFound();
                Problem = await DbContext.Problems
                    .Where(p => p.ProblemId == ppid)
                    .FirstOrDefaultAsync();
                return Problem == null
                    ? base.NotFound() : null;
            }
            else
            {
                return !User.IsInRoles("Administrator,Problem")
                    ? Forbid() : null;
            }
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            context.Result = await ValidateAsync();
            await base.OnActionExecutionAsync(context, next);
        }
    }
}
