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
        protected AppDbContext DbContext { get; }

        [TempData]
        public string StatusMessage { get; set; }

        private bool ProblemLoad { get; }

        public Controller3(AppDbContext db, bool load)
        {
            DbContext = db;
            ProblemLoad = load;
        }

        public Problem Problem { get; set; }

        private async Task<IActionResult> ValidateAsync()
        {
            if (ProblemLoad)
            {
                if (!RouteData.Values.TryGetValue("pid", out var pid))
                    return BadRequest();
                if (!User.IsInRoles("Administrator,AuthorOfProblem" + (string)pid))
                    return Forbid();
                if (!int.TryParse((string)pid, out int ppid))
                    return BadRequest();
                Problem = await DbContext.Problems
                    .Where(p => p.ProblemId == ppid)
                    .FirstOrDefaultAsync();
                return Problem == null
                    ? NotFound() : null;
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
