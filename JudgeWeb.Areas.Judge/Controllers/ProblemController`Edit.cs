using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Providers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    public partial class ProblemController
    {
        [ProblemAuthorize("ppid")]
        [HttpGet("{ppid}")]
        public async Task<IActionResult> Edit(string ppid)
        {
            if (ppid == "add")
            {
                int pid = await ProblemManager.CreateAsync();

                var roleManager = HttpContext.RequestServices
                    .GetRequiredService<RoleManager<IdentityRole<int>>>();
                var rmr = await roleManager.CreateAsync(
                    new IdentityRole<int>("AuthorOfProblem" + pid));

                if (!rmr.Succeeded)
                {
                    return Content("Error creating user role, please contact XiaoYang. Please do not retry.");
                }
                else
                {
                    var user = await UserManager.GetUserAsync(User);
                    await UserManager.AddToRoleAsync(user, "AuthorOfProblem" + pid);

                    var signInManager = HttpContext.RequestServices
                        .GetRequiredService<SignInManager<Data.User>>();
                    await signInManager.RefreshSignInAsync(user);
                }

                return RedirectToAction(nameof(Edit), new { ppid = pid.ToString() });
            }
            else if (int.TryParse(ppid, out int pid))
            {
                var prob = await ProblemManager.GetEditModelAsync(pid);
                if (prob is null) return NotFound();
                ViewBag.Testcase = await TestcaseManager.EnumerateAsync(pid);
                return View(prob);
            }
            else
            {
                return BadRequest();
            }
        }
        
        [ProblemAuthorize("pid")]
        [HttpPost("{pid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int pid, ProblemEditModel model,
            [FromServices] IProblemViewProvider viewGenerator)
        {
            if (ModelState.ErrorCount == 0)
            {
                var prob = await ProblemManager.EditAsync(pid, model);
                if (prob == null) return NotFound();
                await ProblemManager.GenerateViewAsync(prob, viewGenerator);

                ViewData["MsgType"] = "success";
                ViewData["Message"] = "Problem modified successfully.";
            }
            else
            {
                var errors = new StringBuilder();
                foreach (var msg in ModelState)
                    foreach (var item in msg.Value.Errors)
                        errors.AppendLine(item.ErrorMessage);

                ViewData["MsgType"] = "danger";
                ViewData["Message"] = errors;
            }

            ViewBag.Testcase = await TestcaseManager.EnumerateAsync(pid);
            return View(model);
        }

        [ProblemAuthorize("pid")]
        [HttpGet("p{pid}/{target}")]
        public async Task<IActionResult> Markdown(string target, int pid)
        {
            var backstore = "p" + pid;
            var lastVersion = await ProblemManager
                .ReadMarkdownAsync(backstore, target) ?? "";

            return View(new ProblemMarkdownModel
            {
                Markdown = lastVersion,
                BackingStore = backstore,
                Target = target
            });
        }

        [ProblemAuthorize("pid")]
        [HttpGet("p{pid}/{target}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Markdown(string target,
            int pid, ProblemMarkdownModel model)
        {
            var backstore = "p" + pid;
            if (target != model.Target || backstore != model.BackingStore)
                return BadRequest();
            await ProblemManager.SaveMarkdownAsync(backstore, target, model.Markdown);
            return View(model);
        }
    }
}
