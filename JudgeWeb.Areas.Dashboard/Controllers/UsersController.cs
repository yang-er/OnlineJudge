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

            return View(new UserListModel
            {
                CurrentPage = page,
                TotalPage = (total + 99) / 100,
                List = users.Select(a => (
                    a.User,
                    a.Roles.Where(r => roles.ContainsKey(r.RoleId))
                           .Select(ur => roles[ur.RoleId].ShortName)
                    )),
                Roles = roles
            });
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
    }
}
