using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Identity;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    [AuditPoint(AuditlogType.User)]
    public class UsersController : Controller3
    {
        public UserManager UserManager { get; }

        public UsersController(UserManager userManager)
        {
            UserManager = userManager;
        }


        [HttpGet]
        public async Task<IActionResult> List(int page = 1)
        {
            if (page < 1) page = 1;

            var (users1, total) = await UserManager.ListUsersAsync(page, 100);
            if (users1.Count == 0) return NotFound();
            var userRoles = await UserManager.ListUserRolesAsync(users1.First().Id, users1.Last().Id);
            var users =
                from u in users1
                join ur in userRoles on u.Id equals ur.UserId into urs
                select new { User = u, Roles = urs.ToArray() };
            
            var roles = await UserManager.ListNamedRolesAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPage = total;

            return View(users.Select(a => (
                a.User,
                a.Roles.Where(r => roles.ContainsKey(r.RoleId))
                       .Select(ur => roles[ur.RoleId].Name)
                )));
        }


        [HttpGet("{uid}")]
        public async Task<IActionResult> Detail(int uid,
            [FromServices] ISubmissionStore submissions,
            [FromServices] ITeamStore contests)
        {
            var user = await UserManager.FindByIdAsync(uid);
            if (user == null) return NotFound();

            ViewBag.Roles = await UserManager.ListRolesAsync(user);

            (ViewBag.Submissions, _) = await submissions.ListWithJudgingAsync(
                pagination: (1, 100),
                predicate: s => s.ContestId == 0 && s.Author == uid);

            ViewBag.Student = user.StudentId.HasValue
                ? await UserManager.FindStudentAsync(user.StudentId.Value)
                : null;

            ViewBag.Teams = await contests.ListRegisteredWithDetailAsync(uid);
            return View(user);
        }


        [HttpGet("{uid}/[action]")]
        public async Task<IActionResult> Edit(int uid)
        {
            var user = await UserManager.FindByIdAsync(uid);
            if (user == null) return NotFound();

            var hasRole = await UserManager.ListUserRolesAsync(uid, uid);
            var roles = await UserManager.ListNamedRolesAsync();
            ViewBag.Roles = roles;

            return View(new UserEditModel
            {
                Email = user.Email,
                NickName = user.NickName,
                UserId = user.Id,
                UserName = user.UserName,
                Roles = hasRole.Select(ur => ur.RoleId).Intersect(roles.Keys).ToArray()
            });
        }


        [HttpPost("{uid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int uid, UserEditModel model)
        {
            var user = await UserManager.FindByIdAsync(uid);
            if (user == null) return NotFound();

            var msg = "";

            if (model.Password != null)
            {
                var token = await UserManager.GeneratePasswordResetTokenAsync(user);
                var result = await UserManager.ResetPasswordAsync(user, token, model.Password);
                if (!result.Succeeded) msg += $"Error in reset password: {result.Errors.First().Description}.\n";
            }

            if (model.Email != null && user.Email != model.Email)
            {
                var result = await UserManager.SetEmailAsync(user, model.Email);
                if (!result.Succeeded) msg += $"Error in set email: {result.Errors.First().Description}.\n";
            }

            if (model.NickName != null)
            {
                user.NickName = model.NickName;
                var result = await UserManager.UpdateAsync(user);
                if (!result.Succeeded) msg += $"Error in set nickname: {result.Errors.First().Description}.\n";
            }

            // checking roles
            var hasRoles = await UserManager.ListUserRolesAsync(uid, uid);
            var roles = await UserManager.ListNamedRolesAsync();
            var hasRole = hasRoles.Select(u => u.RoleId).Intersect(roles.Keys).ToArray();
            model.Roles = roles.Keys.Intersect(model.Roles ?? Enumerable.Empty<int>()).ToArray();
            if (UserManager.GetUserName(User) == user.UserName)
                model.Roles = model.Roles.Append(-1).Distinct().ToArray();
            var r1 = await UserManager.AddToRolesAsync(user,
                model.Roles.Except(hasRole).Select(i => roles[i].Name));
            var r2 = await UserManager.RemoveFromRolesAsync(user,
                hasRole.Except(model.Roles).Select(i => roles[i].Name));
            if (!r1.Succeeded) msg += $"Error in adding roles: {r1.Errors.First().Description}.\n";
            if (!r2.Succeeded) msg += $"Error in removing roles: {r2.Errors.First().Description}.\n";

            if (string.IsNullOrWhiteSpace(msg)) msg = null;
            StatusMessage = msg ?? $"User u{uid} updated successfully.";
            await HttpContext.AuditAsync("updated", $"{uid}");
            return RedirectToAction(nameof(Detail), new { uid });
        }
    }
}
