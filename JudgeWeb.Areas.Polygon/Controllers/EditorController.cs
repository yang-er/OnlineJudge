using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Identity;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]/{pid}/[action]")]
    public class EditorController : Controller3
    {
        [HttpGet("/[area]/{pid}")]
        public async Task<IActionResult> Overview(int pid,
            [FromServices] ITestcaseStore tcs,
            [FromServices] IArchiveStore archs,
            [FromServices] IProblemsetStore cps)
        {
            ViewBag.TestcaseCount = await tcs.CountAsync(Problem);
            ViewBag.Archive = await archs.FindInternalAsync(pid);
            ViewBag.Contests = await cps.ListByProblemAsync(pid);
            ViewBag.Users = await Problems.ListPermittedUserAsync(pid);
            return View(Problem);
        }


        [HttpGet("{execid}")]
        public async Task<IActionResult> Executables(string execid,
            [FromServices] IExecutableStore execs)
        {
            if (execid != Problem.CompareScript && execid != Problem.RunScript)
                return NotFound();
            var bytes = await execs.FindAsync(execid);
            if (bytes is null) return NotFound();

            ViewBag.Executable = bytes;
            var items = await execs.FetchContentAsync(bytes);
            return View(items);
        }


        [HttpGet]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Archive(int pid,
            [FromServices] IArchiveStore archives)
        {
            var arch = await archives.FindInternalAsync(pid);
            return Window(arch ?? new ProblemArchive { ProblemId = pid });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Archive(
            int pid, ProblemArchive model,
            [FromServices] IArchiveStore archives)
        {
            var arch = await archives.FindInternalAsync(pid);

            if (arch == null)
            {
                try
                {
                    model.TagName ??= "";
                    model.ProblemId = pid;
                    await archives.CreateAsync(model);
                    StatusMessage = $"Problem published as {model.PublicId}.";
                }
                catch (Exception ex)
                {
                    StatusMessage = ex.Message;
                }
            }
            else
            {
                arch.TagName = model.TagName;
                await archives.UpdateAsync(arch);
                StatusMessage = "Problem tag updated.";
            }

            return RedirectToAction(nameof(Overview));
        }


        [HttpGet]
        [Authorize(Roles = "Administrator,Problem")]
        public IActionResult Edit()
        {
            return View(new ProblemEditModel
            {
                ProblemId = Problem.ProblemId,
                CompareScript = Problem.CompareScript,
                RunScript = Problem.RunScript,
                RunAsCompare = Problem.CombinedRunCompare,
                CompareArgument = Problem.ComapreArguments,
                Source = Problem.Source,
                MemoryLimit = Problem.MemoryLimit,
                TimeLimit = Problem.TimeLimit,
                OutputLimit = Problem.OutputLimit,
                Title = Problem.Title,
                Shared = Problem.Shared,
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Problem")]
        [AuditPoint(AuditlogType.Problem)]
        public async Task<IActionResult> Edit(
            int pid, ProblemEditModel model,
            [FromServices] IExecutableStore execs)
        {
            if (model.RunScript == "upload" && model.UploadedRun == null)
                ModelState.AddModelError("XYS::RunScript", "No run script was selected.");
            if (model.CompareScript == "upload" && model.UploadedCompare == null)
                ModelState.AddModelError("XYS::CmpScript", "No compare script was selected.");
            if (!new[] { "compare", Problem.CompareScript, "upload" }.Contains(model.CompareScript))
                ModelState.AddModelError("XYS::CmpScript", "Error compare script defined.");
            if (!new[] { "run", Problem.RunScript, "upload" }.Contains(model.RunScript))
                ModelState.AddModelError("XYS::RunScript", "Error run script defined.");

            if (!ModelState.IsValid)
            {
                StatusMessage = "Error validating problem.\n" +
                    string.Join('\n', ModelState.Values
                        .SelectMany(m => m.Errors)
                        .Select(e => e.ErrorMessage));
                return View(model);
            }

            if (model.RunScript == "upload")
            {
                var cont = await model.UploadedRun.ReadAsync();
                var execid = $"p{pid}run";

                var exec = await execs.FindAsync(execid);
                bool newone = exec == null;
                exec ??= new Executable();
                exec.ExecId = execid;
                exec.Description = $"run pipe for p{pid}";
                exec.Md5sum = cont.Item2;
                exec.ZipFile = cont.Item1;
                exec.Type = "run";
                exec.ZipSize = cont.Item1.Length;

                if (newone) await execs.CreateAsync(exec);
                else await execs.UpdateAsync(exec);
                model.RunScript = execid;
            }

            if (model.CompareScript == "upload")
            {
                var cont = await model.UploadedCompare.ReadAsync();
                var execid = $"p{pid}cmp";

                var exec = await execs.FindAsync(execid);
                bool newone = exec == null;
                exec ??= new Executable();
                exec.ExecId = execid;
                exec.Description = $"output validator for p{pid}";
                exec.Md5sum = cont.Item2;
                exec.ZipFile = cont.Item1;
                exec.Type = "compare";
                exec.ZipSize = cont.Item1.Length;

                if (newone) await execs.CreateAsync(exec);
                else await execs.UpdateAsync(exec);
                model.CompareScript = execid;
            }

            Problem.RunScript = model.RunScript;
            Problem.CompareScript = model.CompareScript;
            Problem.ComapreArguments = model.CompareArgument;
            Problem.MemoryLimit = model.MemoryLimit;
            Problem.OutputLimit = model.OutputLimit;
            Problem.TimeLimit = model.TimeLimit;
            Problem.Title = model.Title;
            Problem.Source = model.Source ?? "";
            Problem.CombinedRunCompare = model.RunAsCompare;
            Problem.Shared = model.Shared;
            await Problems.UpdateAsync(Problem);

            await HttpContext.AuditAsync("edit", $"{Problem.ProblemId}");
            return RedirectToAction(nameof(Overview));
        }


        [HttpGet]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator,Problem")]
        public IActionResult Delete()
        {
            return AskPost(
                title: $"Delete problem {Problem.ProblemId} - \"{Problem.Title}\"",
                message: $"You're about to delete problem {Problem.ProblemId} \"{Problem.Title}\". " +
                    "Warning, this will cascade to testcases and submissions. Are you sure?",
                area: "Polygon", ctrl: "Editor", act: "Delete",
                routeValues: new { pid = Problem.ProblemId },
                type: MessageType.Danger);
        }


        [HttpGet]
        [Authorize(Roles = "Administrator,Problem")]
        [AuditPoint(AuditlogType.Problem)]
        public async Task<IActionResult> Export(
            [FromServices] IExportProvider export)
        {
            await HttpContext.AuditAsync("export", $"{Problem.ProblemId}");
            var (stream, mimeType, fileName) = await export.ExportAsync(Problem);
            return File(stream, mimeType, fileName, false);
        }


        [HttpGet("{uid}")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Unauthorize(int uid,
            [FromServices] UserManager userManager)
        {
            var user = await userManager.FindByIdAsync(uid);
            if (user == null) return NotFound();

            return AskPost(
                title: "Unassign authority",
                message: $"Are you sure to unassign user {user.UserName} (u{uid})?",
                area: "Polygon", ctrl: "Editor", act: "Unauthorize");
        }


        [HttpPost("{uid}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Unauthorize(int uid,
            [FromServices] UserManager userManager, bool post = true)
        {
            var user = await userManager.FindByIdAsync(uid);
            if (user == null) return NotFound();
            var roleName = $"AuthorOfProblem{Problem.ProblemId}";
            var result = await userManager.RemoveFromRoleAsync(user, roleName);

            if (!result.Succeeded)
                StatusMessage = "Error " + result.ToString();
            else
                StatusMessage = "Role unassigned.";
            return RedirectToAction(nameof(Overview));
        }


        [HttpGet]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public IActionResult Authorize()
        {
            return Window();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Authorize(string username,
            [FromServices] RoleManager<Role> roleManager,
            [FromServices] UserManager userManager)
        {
            var user = await userManager.FindByNameAsync(username);

            if (user == null)
            {
                StatusMessage = "Error user not found.";
                return RedirectToAction(nameof(Overview));
            }

            var roleName = $"AuthorOfProblem{Problem.ProblemId}";
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new Role { ProblemId = Problem.ProblemId, Name = roleName });
            var result = await userManager.AddToRoleAsync(user, roleName);

            if (!result.Succeeded)
                StatusMessage = "Error " + result.ToString();
            else
                StatusMessage = "Role assigned.";
            return RedirectToAction(nameof(Overview));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubmit(int pid)
        {
            await Problems.ToggleSubmitAsync(pid, !Problem.AllowSubmit);
            return RedirectToAction(nameof(Overview));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJudge(int pid)
        {
            await Problems.ToggleJudgeAsync(pid, !Problem.AllowJudge);
            return RedirectToAction(nameof(Overview));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Problem")]
        [AuditPoint(AuditlogType.Problem)]
        public async Task<IActionResult> Delete(int pid)
        {
            try
            {
                await Problems.DeleteAsync(Problem);
                await HttpContext.AuditAsync("deleted", $"{Problem.ProblemId}");
                StatusMessage = $"Problem {pid} deleted successfully.";
                return RedirectToAction("List", "Problems", new { area = "Dashboard" });
            }
            catch
            {
                StatusMessage = $"Error occurred when deleting Problem {pid}, foreign key constraints failed.";
                return RedirectToAction(nameof(Overview));
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => StatusCodePage(404);
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();
    }
}
