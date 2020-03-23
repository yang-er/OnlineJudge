using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]/{pid}/[action]")]
    public class EditorController : Controller3
    {
        [HttpGet("/[area]/{pid}")]
        public async Task<IActionResult> Overview(int pid)
        {
            ViewBag.TestcaseCount = await Facade.Testcases.CountAsync(Problem);
            ViewBag.Archive = await Facade.Archives.FindInternalAsync(pid);
            return View(Problem);
        }


        [HttpGet("{execid}")]
        public async Task<IActionResult> Executables(string execid)
        {
            if (execid != Problem.CompareScript && execid != Problem.RunScript)
                return NotFound();
            var bytes = await Facade.Executables.FindAsync(execid);
            if (bytes is null) return NotFound();

            ViewBag.Executable = bytes;
            var items = await Facade.Executables.FetchContentAsync(bytes);
            return View(items);
        }


        [HttpGet]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Archive(int pid)
        {
            var arch = await Facade.Archives.FindInternalAsync(pid);
            return Window(arch ?? new ProblemArchive { ProblemId = pid });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Archive(int pid, ProblemArchive model)
        {
            var arch = await Facade.Archives.FindInternalAsync(pid);

            if (arch == null)
            {
                try
                {
                    model.TagName ??= "";
                    model.ProblemId = pid;
                    await Facade.Archives.CreateAsync(model);
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
                await Facade.Archives.UpdateAsync(arch);
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
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Edit(int pid, ProblemEditModel model)
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
                TempData["StatusMessage"] = "Error validating problem.\n" +
                    string.Join('\n', ModelState.Values
                        .SelectMany(m => m.Errors)
                        .Select(e => e.ErrorMessage));
                return View(model);
            }

            if (model.RunScript == "upload")
            {
                var cont = await model.UploadedRun.ReadAsync();
                var execid = $"p{pid}run";

                var exec = await Facade.Executables.FindAsync(execid);
                bool newone = exec == null;
                exec ??= new Executable();
                exec.ExecId = execid;
                exec.Description = $"run pipe for p{pid}";
                exec.Md5sum = cont.Item2;
                exec.ZipFile = cont.Item1;
                exec.Type = "run";
                exec.ZipSize = cont.Item1.Length;

                if (newone) await Facade.Executables.CreateAsync(exec);
                else await Facade.Executables.UpdateAsync(exec);
                model.RunScript = execid;
            }

            if (model.CompareScript == "upload")
            {
                var cont = await model.UploadedCompare.ReadAsync();
                var execid = $"p{pid}cmp";

                var exec = await Facade.Executables.FindAsync(execid);
                bool newone = exec == null;
                exec ??= new Executable();
                exec.ExecId = execid;
                exec.Description = $"output validator for p{pid}";
                exec.Md5sum = cont.Item2;
                exec.ZipFile = cont.Item1;
                exec.Type = "compare";
                exec.ZipSize = cont.Item1.Length;

                if (newone) await Facade.Executables.CreateAsync(exec);
                else await Facade.Executables.UpdateAsync(exec);
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
            await Facade.Problems.UpdateAsync(Problem);

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
                    "Warning, this will cascade to testcases. Are you sure?",
                area: "Polygon",
                ctrl: "Editor",
                act: "Delete",
                routeValues: new Dictionary<string, string> { ["pid"] = $"{Problem.ProblemId}" },
                type: MessageType.Danger);
        }


        [HttpGet]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Export(IExportProvider export)
        {
            var (stream, mimeType, fileName) = await export.ExportAsync(Problem);
            return File(stream, mimeType, fileName, false);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubmit(int pid)
        {
            await Facade.Problems.ToggleAsync(pid, p => p.AllowSubmit, !Problem.AllowSubmit);
            return RedirectToAction(nameof(Overview));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJudge(int pid)
        {
            await Facade.Problems.ToggleAsync(pid, p => p.AllowJudge, !Problem.AllowJudge);
            return RedirectToAction(nameof(Overview));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Delete(int pid)
        {
            await Facade.Problems.DeleteAsync(Problem);
            StatusMessage = $"Problem {pid} deleted successfully.";
            return RedirectToAction("List", "Root");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => ExplicitNotFound();
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();
    }
}
