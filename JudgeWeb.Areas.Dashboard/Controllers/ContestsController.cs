using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator,Teacher")]
    [Route("[area]/[controller]")]
    [AuditPoint(AuditlogType.Contest)]
    public class ContestsController : Controller3
    {
        [HttpGet]
        public async Task<IActionResult> List(
            [FromServices] IContestFacade facade)
        {
            ViewBag.Teams = await facade.StatisticsTeamAsync();
            ViewBag.Problems = await facade.StatisticsProblemAsync();
            return View(await facade.Contests.ListAsync());
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Add(bool isgym,
            [FromServices] RoleManager<Role> roleManager,
            [FromServices] UserManager userManager,
            [FromServices] IContestStore store)
        {
            var c = await store.CreateAsync(new Contest
            {
                IsPublic = false,
                RegisterDefaultCategory = 0,
                ShortName = "DOMjudge",
                Name = "Round 1",
                Gym = isgym,
            });

            await HttpContext.AuditAsync("added", $"{c.ContestId}");

            int cid = c.ContestId;
            var roleName = $"JuryOfContest{cid}";
            var result = await roleManager.CreateAsync(new Role(roleName) { ContestId = cid });
            if (!result.Succeeded) return Json(result);

            var firstUser = await userManager.GetUserAsync(User);
            var roleAttach = await userManager.AddToRoleAsync(firstUser, roleName);
            if (!roleAttach.Succeeded) return Json(roleAttach);
            return RedirectToAction("Home", "Jury", new { area = "Contest", cid });
        }
    }
}
