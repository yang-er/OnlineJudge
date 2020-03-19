using JudgeWeb.Domains.Contests;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    public class ContestController : Controller2
    {
        private IContestStore Store { get; }

        public ContestController(IContestStore store) => Store = store;


        [HttpGet("/contests")]
        public async Task<IActionResult> List()
        {
            if (!int.TryParse(User.GetUserId(), out int uid))
                uid = -100;

            var cts = await Store.ListAsync(gym: false);
            ViewBag.RegisteredContests = await Store.GetRegisteredContestAsync(uid);
            return View(cts);
        }


        [HttpGet("/gyms")]
        public async Task<IActionResult> ListGyms()
        {
            if (!int.TryParse(User.GetUserId(), out int uid))
                uid = -100;

            var cts = await Store.ListAsync(gym: true);
            ViewBag.RegisteredContests = await Store.GetRegisteredContestAsync(uid);
            return View(cts);
        }
    }
}
