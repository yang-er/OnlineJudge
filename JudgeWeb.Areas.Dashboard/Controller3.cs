using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    public class Controller3 : Controller2
    {
        protected AppDbContext DbContext { get; private set; }

        protected UserManager UserManager { get; private set; }

        [TempData]
        public string StatusMessage { get; set; }

        protected new IActionResult NotFound() => ExplicitNotFound();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            DbContext = HttpContext.RequestServices
                .GetRequiredService<AppDbContext>();
            UserManager = HttpContext.RequestServices
                .GetRequiredService<UserManager>();
            base.OnActionExecuting(context);
        }
    }
}
