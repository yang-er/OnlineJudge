using JudgeWeb.Areas.Account.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Account.Controllers
{
    [Authorize(Roles = "Administrator")]
    [Area("Account")]
    [Route("[area]/[controller]/[action]")]
    public class AdministrationController : Controller2
    {
        const int ItemsPerPageCount = 15;

        private AppDbContext DbContext { get; }

        private UserManager UserManager { get; }

        public AdministrationController(AppDbContext idbc, UserManager um)
        {
            DbContext = idbc;
            UserManager = um;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AdministratorBootstrap()
        {
            var roleManager = HttpContext.RequestServices
                .GetRequiredService<RoleManager<IdentityRole<int>>>();

            if (await roleManager.RoleExistsAsync("Administrator"))
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            else
            {
                var roleCreation = await roleManager.CreateAsync(new IdentityRole<int>("Administrator"));
                if (!roleCreation.Succeeded) return Json(roleCreation);

                var blockCreation = await roleManager.CreateAsync(new IdentityRole<int>("Blocked"));
                var problemCreation = await roleManager.CreateAsync(new IdentityRole<int>("Problem"));
                var guideCreation = await roleManager.CreateAsync(new IdentityRole<int>("Guide"));

                var firstUser = await DbContext.Users.FirstAsync();
                var roleAttach = await UserManager.AddToRoleAsync(firstUser, "Administrator");
                return Json(new { roleCreation, problemCreation, guideCreation, blockCreation, roleAttach, firstUser.UserName });
            }
        }

        [HttpGet("{pg?}")]
        public async Task<IActionResult> List(int pg = 0)
        {
            if (pg == 0) pg = 1;
            
            var users = await DbContext.Users
                .OrderBy(u => u.Id)
                .Skip((pg - 1) * ItemsPerPageCount)
                .Take(ItemsPerPageCount)
                .ToListAsync();

            int firstUid = users.First().Id;
            int lastUid = users.Last().Id;

            var statQuery =
                from s in DbContext.SubmissionStatistics()
                where s.Author >= firstUid && s.Author <= lastUid && s.ContestId == 0
                group s by s.Author;

            ViewBag.Statistics = statQuery
                .ToDictionary(g => g.Key, g => 
                (
                    g.Sum(s => s.AcceptedSubmission),
                    g.Sum(s => s.TotalSubmission))
                );

            return View(users);
        }
        
        [HttpGet("{uid}")]
        public async Task<IActionResult> Modify(int uid)
        {
            var user = await UserManager.FindByIdAsync(uid.ToString());
            if (user is null) return NotFound();

            var model = new UserModifyModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Password = ""
            };

            return Window(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Modify(UserModifyModel model)
        {
            bool valid = ModelState.IsValid;
            User user = null;

            if (valid)
            {
                user = await UserManager.FindByIdAsync(model.UserId.ToString());

                if (user == null)
                {
                    ModelState.AddModelError("xys::no_user", "No such user was found.");
                    valid = false;
                }
            }

            if (valid && user.UserName != model.UserName)
            {
                var res = await UserManager.SetUserNameAsync(user, model.UserName);

                if (!res.Succeeded)
                {
                    ModelState.AddModelError("xys::mod_uname", res.Errors.First().Description);
                    valid = false;
                }
            }

            if (valid && user.Email != model.Email)
            {
                var res = await UserManager.SetEmailAsync(user, model.Email);

                if (!res.Succeeded)
                {
                    ModelState.AddModelError("xys::mod_email", res.Errors.First().Description);
                    valid = false;
                }
            }

            if (valid && !string.IsNullOrEmpty(model.Password))
            {
                var token = await UserManager.GeneratePasswordResetTokenAsync(user);
                var res = await UserManager.ResetPasswordAsync(user, token, model.Password);

                if (!res.Succeeded)
                {
                    ModelState.AddModelError("xys::mod_pwd", res.Errors.First().Description);
                    valid = false;
                }
            }

            if (valid)
            {
                return Message(
                    $"Modify User #{model.UserId}",
                    "User modified successfully.",
                    MessageType.Success);
            }
            else
            {
                return Window(model);
            }
        }

        [HttpGet("{uid}")]
        public async Task<IActionResult> Role(int uid)
        {
            var user = await UserManager.FindByIdAsync(uid.ToString());

            var model = new UserRoleModel { UserId = user.Id };
            var roles = await UserManager.GetRolesAsync(user);
            
            foreach (var role in roles)
            {
                switch (role)
                {
                    case "Blocked":
                        model.SubmissionBlocked = true;
                        break;
                    case "Administrator":
                        model.SiteAdministrator = true;
                        break;
                    case "Problem":
                        model.ProblemProvider = true;
                        break;
                    case "Guide":
                        model.GuideWriter = true;
                        break;
                }
            }

            return Window(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Role(UserRoleModel model)
        {
            var valid = ModelState.IsValid;
            User user = null;

            if (valid)
            {
                if (model.UserId.ToString() == UserManager.GetUserId(User))
                {
                    ModelState.AddModelError("xys::no_self", "You can't edit your role.");
                    valid = false;
                }
            }

            if (valid)
            {
                user = await UserManager.FindByIdAsync(model.UserId.ToString());

                if (user == null)
                {
                    ModelState.AddModelError("xys::no_user", "No such user was found.");
                    valid = false;
                }
            }

            if (valid)
            {
                var roles = await UserManager.GetRolesAsync(user);
                var oldModel = new UserRoleModel();

                foreach (var role in roles)
                {
                    switch (role)
                    {
                        case "Blocked":
                            oldModel.SubmissionBlocked = true;
                            break;
                        case "Administrator":
                            oldModel.SiteAdministrator = true;
                            break;
                        case "Problem":
                            oldModel.ProblemProvider = true;
                            break;
                        case "Guide":
                            oldModel.GuideWriter = true;
                            break;
                    }
                }

                var toRemoveRole = new List<string>();
                var toAddRole = new List<string>();

                if (oldModel.GuideWriter && !model.GuideWriter)
                    toRemoveRole.Add("Guide");
                else if (!oldModel.GuideWriter && model.GuideWriter)
                    toAddRole.Add("Guide");

                if (oldModel.ProblemProvider && !model.ProblemProvider)
                    toRemoveRole.Add("Problem");
                else if (!oldModel.ProblemProvider && model.ProblemProvider)
                    toAddRole.Add("Problem");

                if (oldModel.SiteAdministrator && !model.SiteAdministrator)
                    toRemoveRole.Add("Administrator");
                else if (!oldModel.SiteAdministrator && model.SiteAdministrator)
                    toAddRole.Add("Administrator");

                if (oldModel.SubmissionBlocked && !model.SubmissionBlocked)
                    toRemoveRole.Add("Blocked");
                else if (!oldModel.SubmissionBlocked && model.SubmissionBlocked)
                    toAddRole.Add("Blocked");

                await UserManager.AddToRolesAsync(user, toAddRole);
                await UserManager.RemoveFromRolesAsync(user, toRemoveRole);
            }

            if (valid)
            {
                return Message(
                    $"Role User #{model.UserId}",
                    "User roled successfully.",
                    MessageType.Success);
            }
            else
            {
                return Window(model);
            }
        }
    }
}
