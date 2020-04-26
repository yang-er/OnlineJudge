using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("/dashboard/problems")]
    [Authorize(Roles = "Administrator,Problem")]
    public class RootController : Controller2
    {
        private UserManager<User> UserManager { get; }

        private IProblemStore Store { get; }

        public RootController(UserManager<User> um, IProblemStore db)
        {
            UserManager = um;
            Store = db;
        }


        [HttpGet]
        public async Task<IActionResult> List(int page = 1)
        {
            if (page < 1) return NotFound();
            var uid = User.IsInRole("Administrator")
                ? (int?)null
                : int.Parse(User.GetUserId());
            var (model, totPage) = await Store.ListAsync(uid, page, 50);

            ViewBag.Page = page;
            ViewBag.TotalPage = totPage;
            return View(model);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Create(
            [FromServices] RoleManager<Role> roleManager,
            [FromServices] SignInManager<User> signInManager)
        {
            var p = await Store.CreateAsync(new Problem
            {
                AllowJudge = true,
                AllowSubmit = false,
                CompareScript = "compare",
                RunScript = "run",
                Title = "UNTITLED",
                MemoryLimit = 524288,
                OutputLimit = 4096,
                Source = "",
                TimeLimit = 10000,
            });

            if (!User.IsInRole("Administrator"))
            {
                var i1 = await roleManager.CreateAsync(new Role
                {
                    ProblemId = p.ProblemId,
                    Name = "AuthorOfProblem" + p.ProblemId
                });

                if (!i1.Succeeded)
                {
                    StatusMessage = "Error creating roles. Please contact XiaoYang.";
                    return RedirectToAction(nameof(List));
                }

                var u = await UserManager.GetUserAsync(User);
                var i2 = await UserManager.AddToRoleAsync(u, "AuthorOfProblem" + p.ProblemId);

                if (!i2.Succeeded)
                {
                    StatusMessage = "Error assigning roles. Please contact XiaoYang.";
                    return RedirectToAction(nameof(List));
                }
            }

            return RedirectToAction(
                actionName: "Overview",
                controllerName: "Editor",
                routeValues: new { pid = p.ProblemId });
        }


        [HttpGet("[action]/{jid}")]
        public async Task<IActionResult> ByJudgingId(
            [FromRoute] int jid,
            [FromServices] ISubmissionStore submission)
        {
            var item = await submission.FindByJudgingAsync(jid);
            if (item == null) return NotFound();

            if (item.ContestId == 0)
                return RedirectToAction(
                    actionName: "Detail",
                    controllerName: "Submissions",
                    new { sid = item.SubmissionId, jid, pid = item.ProblemId });
            else
                return RedirectToAction(
                    actionName: "Detail",
                    controllerName: "Submissions",
                    new { area = "Contest", cid = item.ContestId, sid = item.SubmissionId, jid });
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
        public async Task<IActionResult> Import(IFormFile file, string type,
            [FromServices] RoleManager<Role> roleManager)
        {
            try
            {
                if (!IImportProvider.ImportServiceKinds.TryGetValue(type, out var importType))
                    return BadRequest();
                var importer = (IImportProvider)
                    HttpContext.RequestServices.GetService(importType);

                List<Problem> probs;
                using (var stream = file.OpenReadStream())
                    probs = await importer.ImportAsync(
                        stream: stream,
                        streamFileName: file.FileName,
                        username: User.GetUserName());

                if (probs.Count > 1)
                    importer.LogBuffer.AppendLine("Uploading multiple problems, showing first.");

                if (probs.Count == 0)
                    throw new InvalidOperationException("No problems are uploaded.");

                StatusMessage = importer.LogBuffer.ToString();

                foreach (var prob in probs)
                {
                    await roleManager.CreateAsync(new Role
                    {
                        ProblemId = prob.ProblemId,
                        Name = "AuthorOfProblem" + prob.ProblemId
                    });

                    var u = await UserManager.GetUserAsync(User);
                    await UserManager.AddToRoleAsync(u, "AuthorOfProblem" + prob.ProblemId);
                }

                return RedirectToAction(
                    actionName: "Overview",
                    controllerName: "Editor",
                    routeValues: new { pid = probs[0].ProblemId });
            }
            catch (Exception ex)
            {
                return Message("Problem Import",
                    "Import failed. Please contact XiaoYang immediately. " + ex,
                    MessageType.Danger);
            }
        }


        [HttpGet("/dashboard/status")]
        public async Task<IActionResult> Status(
            [FromServices] ISubmissionStore submissions,
            [FromQuery] int page = 1)
        {
            if (page <= 0) return BadRequest();

            var (model, totPage) = await submissions
                .ListWithJudgingAsync(pagination: (page, 50));
            foreach (var item in model)
                item.AuthorName = item.ContestId == 0
                    ? $"u{item.AuthorId}"
                    : $"c{item.ContestId}t{item.AuthorId}";

            totPage = (totPage - 1) / 50 + 1;
            ViewBag.Page = page;
            ViewBag.TotalPage = totPage;
            return View(model);
        }


        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => StatusCodePage(404);
        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();
    }
}
