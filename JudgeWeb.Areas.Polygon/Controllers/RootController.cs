using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("/dashboard/problems")]
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
        public async Task<IActionResult> Create(
            [FromServices] RoleManager<Role> roleManager,
            [FromServices] SignInManager<User> signInManager)
        {
            var p = DbContext.Problems.Add(new Problem
            {
                AllowJudge = true,
                AllowSubmit = true,
                CompareScript = "compare",
                RunScript = "run",
                Title = "UNTITLED",
                MemoryLimit = 524288,
                OutputLimit = 4096,
                Source = "",
                TimeLimit = 10000,
            });

            await DbContext.SaveChangesAsync();

            var i1 = await roleManager.CreateAsync(new Role
            {
                ProblemId = p.Entity.ProblemId,
                Name = "AuthorOfProblem" + p.Entity.ProblemId
            });

            if (!i1.Succeeded)
            {
                StatusMessage = "Error creating roles. Please contact XiaoYang.";
                return RedirectToAction(nameof(List));
            }

            var u = await UserManager.GetUserAsync(User);
            var i2 = await UserManager.AddToRoleAsync(u, "AuthorOfProblem" + p.Entity.ProblemId);
            await UserManager.SlideExpirationAsync(u);

            if (!i2.Succeeded)
            {
                StatusMessage = "Error assigning roles. Please contact XiaoYang.";
                return RedirectToAction(nameof(List));
            }

            await signInManager.RefreshSignInAsync(u);
            return RedirectToAction(
                actionName: "Overview",
                controllerName: "Editor",
                routeValues: new { pid = p.Entity.ProblemId });
        }


        [HttpGet("[action]/{jid}")]
        public async Task<IActionResult> ByJudgingId(int jid)
        {
            var query =
                from j in DbContext.Judgings
                where j.JudgingId == jid
                join s in DbContext.Submissions on j.SubmissionId equals s.SubmissionId
                select new { sid = s.SubmissionId, pid = s.ProblemId, jid = j.JudgingId, cid = s.ContestId };

            var item = await query.FirstOrDefaultAsync();
            if (item == null) return NotFound();
            if (item.cid == 0)
                return RedirectToAction("Detail", "Submissions", new { item.sid, item.jid, item.pid });
            else
                return RedirectToAction("Detail", "Submissions", new { area = "Contest", item.cid, item.sid, item.jid });
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Import()
        {
            return Window();
        }


        static readonly Dictionary<string, Type> ImportServiceKinds =
            new Dictionary<string, Type>
            {
                ["kattis"] = typeof(KattisPackageImportService),
                ["xysxml"] = typeof(XmlPackageImportService),
            };


        [HttpPost("[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(1 << 30)]
        [RequestFormLimits2(1 << 30)]
        public async Task<IActionResult> Import(IFormFile file, string type,
            [FromServices] RoleManager<Role> roleManager)
        {
            try
            {
                if (!ImportServiceKinds.TryGetValue(type, out var importType))
                    return BadRequest();
                var importer = (IProblemImportService)
                    HttpContext.RequestServices.GetService(importType);

                var prob = await importer.ImportAsync(
                    zipFile: file,
                    username: UserManager.GetUserName(User));

                StatusMessage = importer.LogBuffer.ToString();

                await roleManager.CreateAsync(new Role
                {
                    ProblemId = prob.ProblemId,
                    Name = "AuthorOfProblem" + prob.ProblemId
                });

                var u = await UserManager.GetUserAsync(User);
                await UserManager.AddToRoleAsync(u, "AuthorOfProblem" + prob.ProblemId);
                await UserManager.SlideExpirationAsync(u);

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


        [HttpGet("/dashboard/status")]
        public async Task<IActionResult> Status(int page = 1)
        {
            if (page <= 0) return BadRequest();

            var statusQuery =
                from s in DbContext.Submissions
                join j in DbContext.Judgings
                    on new { s.SubmissionId, Active = true }
                    equals new { j.SubmissionId, j.Active }
                orderby s.SubmissionId descending
                select new StatusListModel
                {
                    SubmissionId = s.SubmissionId,
                    Verdict = j.Status,
                    CodeLength = s.CodeLength,
                    ExecutionMemory = j.ExecuteMemory,
                    ExecutionTime = j.ExecuteTime,
                    Language = s.Language,
                    ProblemId = s.ProblemId,
                    Time = s.Time,
                    Author = s.Author,
                    ContestId = s.ContestId,
                    JudgingId = j.JudgingId,
                };

            var model = await statusQuery
                .Skip((page - 1) * 50)
                .Take(50)
                .ToListAsync();

            ViewBag.Page = page;
            return View(model);
        }


        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => ExplicitNotFound();
        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();
    }
}
