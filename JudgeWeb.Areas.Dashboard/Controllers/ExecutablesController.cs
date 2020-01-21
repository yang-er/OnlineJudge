using JudgeWeb.Areas.Dashboard.Models;
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

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class ExecutablesController : Controller3
    {
        public ExecutablesController(AppDbContext db) : base(db) { }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var execs = await DbContext.Executable
                .Select(e => new Executable
                {
                    ExecId = e.ExecId,
                    Description = e.Description,
                    Md5sum = e.Md5sum,
                    Type = e.Type,
                    ZipSize = e.ZipSize,
                })
                .ToListAsync();

            return View(execs);
        }

        [HttpGet("{execid}")]
        public async Task<IActionResult> Detail(string execid)
        {
            var exec = await DbContext.Executable
                .Where(e => e.ExecId == execid)
                .Select(e => new Executable
                {
                    ExecId = e.ExecId,
                    Description = e.Description,
                    Md5sum = e.Md5sum,
                    Type = e.Type,
                    ZipSize = e.ZipSize,
                })
                .FirstOrDefaultAsync();
            if (exec == null) return NotFound();

            ViewBag.AsCompile = await DbContext.Languages
                .Where(l => l.CompileScript == execid)
                .Select(l => l.Id)
                .ToListAsync();
            ViewBag.AsRun = await DbContext.Problems
                .Where(p => p.RunScript == execid)
                .Select(p => p.ProblemId)
                .ToListAsync();
            ViewBag.AsCompare = await DbContext.Problems
                .Where(p => p.CompareScript == execid)
                .Select(p => p.ProblemId)
                .ToListAsync();

            return View(exec);
        }

        [HttpGet("{execid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(string execid)
        {
            var desc = await DbContext.Executable
                .Where(e => e.ExecId == execid)
                .Select(e => e.Description)
                .FirstOrDefaultAsync();
            if (desc == null) return NotFound();

            return AskPost(
                title: $"Delete executable {execid} - \"{desc}\"",
                message: $"You're about to delete executable {execid} - \"{desc}\".\n" +
                    "Warning, this will create dangling references in languages.\n" +
                    "Are you sure?",
                area: "Dashboard",
                ctrl: "Executables",
                act: "Delete",
                routeValues: new Dictionary<string, string> { { "execid", execid } },
                type: MessageType.Danger);
        }

        [HttpGet("{execid}/[action]")]
        public async Task<IActionResult> Edit(string execid)
        {
            var exec = await DbContext.Executable
                .Where(e => e.ExecId == execid)
                .Select(e => new ExecutableEditModel
                {
                    ExecId = e.ExecId,
                    Description = e.Description,
                    Type = e.Type,
                })
                .FirstOrDefaultAsync();
            if (exec == null) return NotFound();
            return View(exec);
        }

        [HttpPost("{execid}/[action]")]
        public async Task<IActionResult> Edit(string execid, ExecutableEditModel model)
        {
            var exec = await DbContext.Executable
                .Where(e => e.ExecId == execid)
                .FirstOrDefaultAsync();
            if (exec == null) return NotFound();
            if (!"compile,compare,run".Split(',').Contains(model.Type))
                return BadRequest();

            if (model.Archive != null)
                (exec.ZipFile, exec.Md5sum) = await model.Archive.ReadAsync();
            exec.ZipSize = exec.ZipFile.Length;
            exec.Description = model.Description ?? execid;
            exec.Type = model.Type;
            DbContext.Executable.Update(exec);
            await DbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { execid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ExecutableEditModel model)
        {
            string execid = null;
            if (model.Archive == null || model.Archive.Length >= 1 << 20)
            {
                StatusMessage = "Error in executable uploading.";
            }
            else if (model.ExecId == null)
            {
                StatusMessage = "Error no executable name specified.";
            }
            else if (model.Description == null)
            {
                StatusMessage = "Error no description specified.";
            }
            else if (!"compile,compare,run".Split(',').Contains(model.Type))
            {
                StatusMessage = "Error type specified.";
            }
            else
            {
                try
                {
                    var (zip, md5) = await model.Archive.ReadAsync();

                    var e = DbContext.Executable.Add(new Executable
                    {
                        Description = model.Description,
                        ExecId = model.ExecId,
                        Type = model.Type,
                        ZipFile = zip,
                        Md5sum = md5,
                        ZipSize = zip.Length,
                    });

                    await DbContext.SaveChangesAsync();
                    execid = e.Entity.ExecId;
                    StatusMessage = $"Executable {execid} uploaded successfully.";
                }
                catch (DbUpdateException ex)
                {
                    StatusMessage = "Error: " + ex.Message;
                }
            }

            if (execid != null) return RedirectToAction("Detail", new { execid });
            else return RedirectToAction("List");
        }

        [HttpGet("{execid}/[action]")]
        [ActionName("Content")]
        public async Task<IActionResult> ViewContent(string execid)
        {
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

        [HttpPost("{execid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string execid, int inajax)
        {
            var exec = await DbContext.Executable
                .Where(e => e.ExecId == execid)
                .FirstOrDefaultAsync();
            if (exec == null) return NotFound();

            DbContext.Executable.Remove(exec);

            try
            {
                await DbContext.SaveChangesAsync();
                StatusMessage = $"Executable {execid} deleted successfully.";
            }
            catch (DbUpdateException)
            {
                StatusMessage = $"Error deleting executable {execid}, foreign key constraints failed.";
            }

            return RedirectToAction(nameof(List));
        }

        [HttpGet("{execid}/[action]")]
        public async Task<IActionResult> Download(string execid)
        {
            var bytes = await DbContext.Executable
                .Where(e => e.ExecId == execid)
                .Select(e => e.ZipFile)
                .FirstOrDefaultAsync();
            if (bytes is null) return NotFound();
            return File(bytes, "application/zip", $"{execid}.zip", false);
        }
    }
}
