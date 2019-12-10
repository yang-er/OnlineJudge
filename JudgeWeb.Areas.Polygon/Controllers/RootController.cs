using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]")]
    [Authorize(Roles = "Administrator,Problem")]
    public class RootController : Controller2
    {
        private UserManager UserManager { get; }

        private AppDbContext DbContext { get; }

        public RootController(UserManager um, AppDbContext db)
        {
            UserManager = um;
            DbContext = db;
        }


        [HttpGet]
        public async Task<IActionResult> List(int page = 1)
        {
            IQueryable<Problem> problemSource;
            if (page < 1) page = 1;

            if (User.IsInRole("Administrator"))
            {
                problemSource = DbContext.Problems;
            }
            else
            {
                var uid = int.Parse(UserManager.GetUserId(User));
                
                problemSource =
                    from ur in DbContext.UserRoles
                    where ur.UserId == uid
                    join r in DbContext.Roles on ur.RoleId equals r.Id
                    join p in DbContext.Problems on r.ProblemId equals p.ProblemId
                    select p;
            }

            int total = await problemSource.CountAsync();
            int totPage = (total - 1) / 50 + 1;
            if (page > totPage) page = totPage;
            ViewBag.Page = page;
            ViewBag.TotalPage = totPage;

            var src2 =
                from p in problemSource
                join pa in DbContext.Archives on p.ProblemId equals pa.ProblemId into pas
                from pa in pas.DefaultIfEmpty()
                select new { p, id = (int?)pa.PublicId, tag = pa.TagName };

            var model = await src2
                .OrderBy(p => p.p.ProblemId)
                .Skip(50 * (page - 1))
                .Take(50)
                .ToListAsync();

            return View(model.Select(a => (a.p, a.id, a.tag)));
        }
    }
}
