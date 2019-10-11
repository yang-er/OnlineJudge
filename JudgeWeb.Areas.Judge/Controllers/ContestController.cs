using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    [Area("Judge")]
    [Route("[area]/[controller]/[action]")]
    public class ContestController : Controller2
    {
        private JudgeManager JudgeManager { get; }

        private UserManager UserManager { get; }

        public ContestController(JudgeManager jm, UserManager um)
        {
            JudgeManager = jm;
            UserManager = um;
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(
            [FromServices] RoleManager<IdentityRole<int>> roleMgr)
        {
            int cid = await JudgeManager.CreateContestAsync(UserManager.GetUserName(User));

            var roleName = $"JuryOfContest{cid}";
            var result = await roleMgr.CreateAsync(new IdentityRole<int>(roleName));
            if (!result.Succeeded) return Json(result);

            var firstUser = await UserManager.GetUserAsync(User);
            var roleAttach = await UserManager.AddToRoleAsync(firstUser, roleName);
            if (!roleAttach.Succeeded) return Json(roleAttach);
            return RedirectToAction("Home", "Jury", new { area = "Contest", cid });
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            int.TryParse(UserManager.GetUserId(User), out int uid);
            return View(await JudgeManager.GetContestsAsync(uid));
        }
    }
}
