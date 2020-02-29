using EFCore.BulkExtensions;
using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public EditorController(AppDbContext db) : base(db, true) { }


        [HttpGet("/[area]/{pid}")]
        public async Task<IActionResult> Overview(int pid)
        {
            ViewData["TestcaseCount"] =
                await DbContext.Testcases
                    .Where(t => t.ProblemId == pid)
                    .CountAsync();
            var arch = await DbContext.Archives
                .Where(a => a.ProblemId == pid)
                .SingleOrDefaultAsync();
            ViewBag.Archive = arch;
            return View(Problem);
        }


        [HttpGet("{execid}")]
        public async Task<IActionResult> Executables(string execid)
        {
            if (execid != Problem.CompareScript && execid != Problem.RunScript)
                return NotFound();
            var bytes = await DbContext.Executable
                .Where(e => e.ExecId == execid)
                .FirstOrDefaultAsync();
            if (bytes is null) return NotFound();

            ViewBag.Executable = bytes;
            var items = new List<ExecutableViewContentModel>();
            using (var stream = new MemoryStream(bytes.ZipFile, false))
            using (var zipArchive = new ZipArchive(stream))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    var fileName = entry.FullName;
                    var fileExt = Path.GetExtension(fileName);
                    fileExt = string.IsNullOrEmpty(fileExt) ? "dummy.sh" : "dummy" + fileExt;
                    var fileContent = new byte[entry.Length];

                    using (var entryStream = entry.Open())
                        await entryStream.ReadAsync(fileContent);
                    var fileContent2 = Encoding.UTF8.GetString(fileContent);

                    items.Add(new ExecutableViewContentModel
                    {
                        FileName = fileName,
                        FileContent = fileContent2,
                        Language = fileExt,
                    });
                }
            }

            return View(items);
        }


        [HttpGet]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Archive(int pid)
        {
            var arch = await DbContext.Archives
                .Where(a => a.ProblemId == pid)
                .SingleOrDefaultAsync();
            return Window(arch ?? new ProblemArchive { ProblemId = pid });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Archive(int pid, ProblemArchive model)
        {
            if (!await DbContext.Archives.AnyAsync(a => a.ProblemId == pid))
            {
                if (model.PublicId == 0)
                {
                    model.PublicId = await DbContext.Archives
                        .MaxAsync(p => p.PublicId);
                    if (model.PublicId == 0) model.PublicId = 1001;
                    else model.PublicId++;
                }

                model.ProblemId = pid;
                var existed = await DbContext.Archives
                    .Where(a => a.PublicId == model.PublicId)
                    .AnyAsync();

                if (existed)
                {
                    StatusMessage = "Error public id was set.";
                }
                else
                {
                    model.TagName = model.TagName ?? "";
                    DbContext.Archives.Add(model);
                    await DbContext.SaveChangesAsync();
                    StatusMessage = $"Problem published as {model.PublicId}.";
                }
            }
            else
            {
                var item = await DbContext.Archives
                    .Where(a => a.ProblemId == pid)
                    .FirstOrDefaultAsync();
                if (item == null) return BadRequest();
                item.TagName = model.TagName;
                DbContext.Archives.Update(item);
                await DbContext.SaveChangesAsync();
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

                var exec = await DbContext.Executable
                    .Where(e => e.ExecId == execid)
                    .FirstOrDefaultAsync();
                bool newone = exec == null;
                exec = exec ?? new Executable();
                exec.ExecId = execid;
                exec.Description = $"run pipe for p{pid}";
                exec.Md5sum = cont.Item2;
                exec.ZipFile = cont.Item1;
                exec.Type = "run";
                exec.ZipSize = cont.Item1.Length;

                if (newone) DbContext.Executable.Add(exec);
                else DbContext.Executable.Update(exec);
                await DbContext.SaveChangesAsync();
                model.RunScript = execid;
            }

            if (model.CompareScript == "upload")
            {
                var cont = await model.UploadedCompare.ReadAsync();
                var execid = $"p{pid}cmp";

                var exec = await DbContext.Executable
                    .Where(e => e.ExecId == execid)
                    .FirstOrDefaultAsync();
                bool newone = exec == null;
                exec = exec ?? new Executable();
                exec.ExecId = execid;
                exec.Description = $"output validator for p{pid}";
                exec.Md5sum = cont.Item2;
                exec.ZipFile = cont.Item1;
                exec.Type = "compare";
                exec.ZipSize = cont.Item1.Length;

                if (newone) DbContext.Executable.Add(exec);
                else DbContext.Executable.Update(exec);
                await DbContext.SaveChangesAsync();
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
            DbContext.Problems.Update(Problem);
            await DbContext.SaveChangesAsync();

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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubmit(int pid)
        {
            await DbContext.Problems
                .Where(p => p.ProblemId == pid)
                .BatchUpdateAsync(p =>
                    new Problem { AllowSubmit = !p.AllowSubmit });

            return RedirectToAction(nameof(Overview));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJudge(int pid)
        {
            await DbContext.Problems
                .Where(p => p.ProblemId == pid)
                .BatchUpdateAsync(p =>
                    new Problem { AllowJudge = !p.AllowJudge });

            return RedirectToAction(nameof(Overview));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Delete(int pid)
        {
            DbContext.Problems.Remove(Problem);
            await DbContext.SaveChangesAsync();
            StatusMessage = $"Problem {pid} deleted successfully.";
            return RedirectToAction("List", "Root");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => ExplicitNotFound();
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();
    }
}
