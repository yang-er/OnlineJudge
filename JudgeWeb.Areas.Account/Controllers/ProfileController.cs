using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Account.Controllers
{
    [Area("Account")]
    [Route("[area]/[controller]/[action]")]
    public class ProfileController : Controller
    {
        AppDbContext DbContext { get; }

        public ProfileController(AppDbContext adbc)
        {
            DbContext = adbc;
        }

        [HttpGet("{uid}")]
        public async Task<IActionResult> View(int uid)
        {
            var user = await DbContext.Users
                .Where(u => u.Id == uid)
                .FirstOrDefaultAsync();
            if (user is null) return NotFound();
            ViewBag.User = user;

            ViewBag.Stat = DbContext.SubmissionStatistics
                .Where(s => s.Author == uid);
            return View();
        }
    }
}
