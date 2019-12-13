using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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

        [TempData]
        public string StatusMessage { get; set; }

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


        [HttpGet("[action]")]
        public async Task<IActionResult> Create()
        {
            var p = DbContext.Problems.Add(new Problem
            {
                AllowJudge = true,
                AllowSubmit = true,
                CompareScript = "compare",
                RunScript = "compare",
                Title = "UNTITLED",
                MemoryLimit = 524288,
                OutputLimit = 4096,
                Source = "",
                TimeLimit = 10000,
            });

            await DbContext.SaveChangesAsync();
            return RedirectToAction(
                actionName: "Overview",
                controllerName: "Editor",
                routeValues: new { pid = p.Entity.ProblemId });
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Import()
        {
            return Window();
        }


        [HttpPost("[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(1 << 30)]
        [RequestFormLimits2(1 << 30)]
        public async Task<IActionResult> Import(IFormFile file,
            [FromServices] ProblemImportService importer)
        {
            try
            {
                var prob = await importer.ImportAsync(
                    zipFile: file,
                    username: UserManager.GetUserName(User));

                StatusMessage = importer.LogBuffer.ToString();

                return RedirectToAction(
                    actionName: "Overview",
                    controllerName: "Editor",
                    routeValues: new { pid = prob.ProblemId });
            }
            catch (Exception ex)
            {
                return Message("Problem Import",
                    "Import failed. Please contact XiaoYang immediately. " + ex,
                    MessageType.Danger);
            }
        }
    }
}
