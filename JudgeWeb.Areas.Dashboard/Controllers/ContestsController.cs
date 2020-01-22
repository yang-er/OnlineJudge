using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class ContestsController : Controller3
    {
        public ContestsController(AppDbContext db) : base(db) { }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            ViewBag.Teams = await DbContext.Teams
                .Where(t => t.Status == 1)
                .GroupBy(t => t.ContestId)
                .Select(g => new { ContestId = g.Key, TeamCount = g.Count() })
                .ToDictionaryAsync(k => k.ContestId, v => v.TeamCount);
            ViewBag.Problems = await DbContext.ContestProblem
                .GroupBy(t => t.ContestId)
                .Select(g => new { ContestId = g.Key, ProblemCount = g.Count() })
                .ToDictionaryAsync(k => k.ContestId, v => v.ProblemCount);
            return View(await DbContext.Contests.ToListAsync());
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Add(
            [FromServices] RoleManager<Role> roleManager,
            [FromServices] UserManager userManager)
        {
            var username = userManager.GetUserName(User);

            var c = DbContext.Contests.Add(new Contest
            {
                IsPublic = false,
                RegisterDefaultCategory = 0,
                ShortName = "",
                Name = "",
            });

            await DbContext.SaveChangesAsync();

            if (username != null)
            {
                DbContext.AuditLogs.Add(new AuditLog
                {
                    Comment = "created",
                    UserName = username,
                    ContestId = c.Entity.ContestId,
                    EntityId = c.Entity.ContestId,
                    Resolved = true,
                    Time = DateTimeOffset.Now,
                    Type = AuditLog.TargetType.Contest,
                });

                await DbContext.SaveChangesAsync();
            }

            int cid = c.Entity.ContestId;
            var roleName = $"JuryOfContest{cid}";
            var result = await roleManager.CreateAsync(new Role(roleName));
            if (!result.Succeeded) return Json(result);

            var firstUser = await userManager.GetUserAsync(User);
            var roleAttach = await userManager.AddToRoleAsync(firstUser, roleName);
            await userManager.SlideExpirationAsync(firstUser);
            if (!roleAttach.Succeeded) return Json(roleAttach);
            return RedirectToAction("Home", "Jury", new { area = "Contest", cid });
        }
    }
}
