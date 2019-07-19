using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    [Authorize(Roles = privilege)]
    [Route("[controller]/[action]")]
    public class GuideController : Controller
    {
        const string privilege = "Administrator,Guide";

        public IActionResult Index()
        {
            return View();
        }
    }
}
