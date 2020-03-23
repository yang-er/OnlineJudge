using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    public class Controller3 : Controller2
    {
        protected new IActionResult NotFound() => ExplicitNotFound();
    }
}
