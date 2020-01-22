using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class UsersController : Controller3
    {
        public UsersController(AppDbContext db) : base(db) { }

        [HttpGet]
        public async Task<IActionResult> List(int page = 1)
        {
            if (page < 1) page = 1;

            var users = await DbContext.Users
                .OrderBy(u => u.Id)
                .Skip((page - 1) * 100).Take(100)
                .GroupJoin(
                    inner: DbContext.UserRoles,
                    outerKeySelector: u => u.Id,
                    innerKeySelector: ur => ur.UserId,
                    resultSelector: (u, ur) => new { User = u, Roles = ur.ToArray() })
                .ToListAsync();

            int total = await DbContext.Users.CountAsync();
            var roles = await DbContext.Roles
                .Where(r => r.ShortName != null)
                .ToDictionaryAsync(r => r.Id);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPage = (total + 99) / 100;

            return View(users.Select(a => (
                a.User,
                a.Roles.Where(r => roles.ContainsKey(r.RoleId))
                       .Select(ur => roles[ur.RoleId].ShortName)
                )));
        }

        [HttpGet("{uid}")]
        public async Task<IActionResult> Detail(int uid)
        {
            var user = await DbContext.Users
                .Where(u => u.Id == uid)
                .FirstOrDefaultAsync();
            if (user == null) return NotFound();

            var roleQuery =
                from ur in DbContext.UserRoles
                where ur.UserId == uid
                join r in DbContext.Roles on ur.RoleId equals r.Id
                select r;

            var submitQuery =
                from s in DbContext.Submissions
                where s.ContestId == 0 && s.Author == uid
                orderby s.SubmissionId descending
                join j in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                select new { s, j };

            var teamQuery =
                from t in DbContext.Teams
                where t.UserId == uid
                join c in DbContext.Contests on t.ContestId equals c.ContestId
                join a in DbContext.TeamAffiliations on t.AffiliationId equals a.AffiliationId
                join o in DbContext.TeamCategories on t.CategoryId equals o.CategoryId
                select new { c, t, a, o };

            ViewBag.Roles = await roleQuery.ToListAsync();
            ViewBag.Submissions = (await submitQuery.Take(100).ToListAsync()).Select(a => (a.s, a.j));
            ViewBag.Teams = (await teamQuery.ToListAsync()).Select(a => (a.c, a.t, a.a, a.o));
            return View(user);
        }

        [HttpGet("{uid}/[action]")]
        public async Task<IActionResult> Edit(int uid)
        {
            var user = await DbContext.Users
                .Where(u => u.Id == uid)
                .FirstOrDefaultAsync();
            if (user == null) return NotFound();

            var hasRole = await DbContext.UserRoles
                .Where(u => u.UserId == uid)
                .Select(ur => ur.RoleId)
                .ToArrayAsync();

            var roles = await DbContext.Roles
                .Where(r => r.ShortName != null)
                .ToDictionaryAsync(r => r.Id);
            ViewBag.Roles = roles;

            return View(new UserEditModel
            {
                Email = user.Email,
                NickName = user.NickName,
                UserId = user.Id,
                UserName = user.UserName,
                Roles = roles.Keys.Intersect(hasRole).ToArray()
            });
        }

        [HttpPost("{uid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int uid, UserEditModel model,
            [FromServices] UserManager userManager)
        {
            var user = await userManager.FindByIdAsync($"{uid}");
            if (user == null) return NotFound();

            var msg = "";

            if (model.Password != null)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, token, model.Password);
                if (!result.Succeeded) msg += $"Error in reset password: {result.Errors.First().Description}.\n";
            }

            if (model.Email != null)
            {
                var result = await userManager.SetEmailAsync(user, model.Email);
                if (!result.Succeeded) msg += $"Error in set email: {result.Errors.First().Description}.\n";
            }

            if (model.NickName != null)
            {
                user.NickName = model.NickName;
                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded) msg += $"Error in set nickname: {result.Errors.First().Description}.\n";
            }

            // checking roles
            var hasRole = await DbContext.UserRoles
                .Where(u => u.UserId == uid)
                .Select(ur => ur.RoleId)
                .ToArrayAsync();
            var roles = await DbContext.Roles
                .Where(r => r.ShortName != null)
                .ToDictionaryAsync(r => r.Id);
            hasRole = roles.Keys.Intersect(hasRole).ToArray();
            model.Roles = roles.Keys.Intersect(model.Roles ?? Enumerable.Empty<int>()).ToArray();
            if (userManager.GetUserName(User) == user.UserName)
                model.Roles = model.Roles.Append(-1).Distinct().ToArray();
            var r1 = await userManager.AddToRolesAsync(user,
                model.Roles.Except(hasRole).Select(i => roles[i].Name));
            var r2 = await userManager.RemoveFromRolesAsync(user,
                hasRole.Except(model.Roles).Select(i => roles[i].Name));
            await userManager.SlideExpirationAsync(user);
            if (!r1.Succeeded) msg += $"Error in adding roles: {r1.Errors.First().Description}.\n";
            if (!r2.Succeeded) msg += $"Error in removing roles: {r2.Errors.First().Description}.\n";

            if (msg == "") msg = null;
            StatusMessage = msg;
            return RedirectToAction(nameof(Detail), new { uid });
        }
    }
}
