using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]")]
    public class RootController : Controller3
    {
        public RootController(AppDbContext db) : base(db) { }

        public IActionResult Index() => View();
    }
}
