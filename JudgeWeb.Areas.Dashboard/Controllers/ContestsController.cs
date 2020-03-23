using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    [AuditPoint(AuditlogType.Contest)]
    public class ContestsController : Controller3
    {
        [HttpGet]
        public async Task<IActionResult> List(
            [FromServices] IContestStore store)
        {
            ViewBag.Teams = new Dictionary<int, int>();// await DbContext.Teams
                //.Where(t => t.Status == 1)
                //.GroupBy(t => t.ContestId)
                //.Select(g => new { ContestId = g.Key, TeamCount = g.Count() })
                //.ToDictionaryAsync(k => k.ContestId, v => v.TeamCount);
            ViewBag.Problems = new Dictionary<int, int>(); //await DbContext.ContestProblem
                //.GroupBy(t => t.ContestId)
                //.Select(g => new { ContestId = g.Key, ProblemCount = g.Count() })
                //.ToDictionaryAsync(k => k.ContestId, v => v.ProblemCount);
            return View(await store.ListAsync());
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
