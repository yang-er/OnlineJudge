using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    [AuditPoint(AuditlogType.Executable)]
    public class ExecutablesController : Controller3
    {
        private IExecutableStore Store { get; }
        private ILogger<IProblemFacade> Logger { get; }

        public ExecutablesController(IProblemFacade facade)
        {
            Store = facade.ExecutableStore;
            Logger = facade.Logger;
        }


        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View(await Store.ListAsync());
        }


        [HttpGet("{execid}")]
        public async Task<IActionResult> Detail(string execid)
        {
            var exec = await Store.FindAsync(execid);
            if (exec == null) return NotFound();
            ViewBag.Usage = await Store.ListUsageAsync(exec);
            return View(exec);
        }


        [HttpGet("{execid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(string execid)
        {
            var exec = await Store.FindAsync(execid);
            if (exec == null) return NotFound();

            return AskPost(
                title: $"Delete executable {execid} - \"{exec.Description}\"",
                message: $"You're about to delete executable {execid} - \"{exec.Description}\".\n" +
                    "Warning, this will create dangling references in languages.\n" +
                    "Are you sure?",
                area: "Dashboard", ctrl: "Executables", act: "Delete",
                routeValues: new { execid },
                type: MessageType.Danger);
        }


        [HttpGet("{execid}/[action]")]
        public async Task<IActionResult> Edit(string execid)
        {
            var exec = await Store.FindAsync(execid);
            if (exec == null) return NotFound();
            
            return View(new ExecutableEditModel
            {
                ExecId = exec.ExecId,
                Description = exec.Description,
                Type = exec.Type,
            });
        }


        [HttpPost("{execid}/[action]")]
        public async Task<IActionResult> Edit(string execid, ExecutableEditModel model)
        {
            var exec = await Store.FindAsync(execid);
            if (exec == null) return NotFound();
            if (!"compile,compare,run".Split(',').Contains(model.Type))
                return BadRequest();

            if (model.Archive != null)
                (exec.ZipFile, exec.Md5sum) = await model.Archive.ReadAsync();
            exec.ZipSize = exec.ZipFile.Length;
            exec.Description = model.Description ?? execid;
            exec.Type = model.Type;
            await Store.UpdateAsync(exec);
            await HttpContext.AuditAsync("updated", execid);
            return RedirectToAction(nameof(Detail), new { execid });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ExecutableEditModel model)
        {
            // Validations
            string message = null;
            if (model.Archive == null || model.Archive.Length >= 1 << 20)
                message = "Error in executable uploading.";
            else if (model.ExecId == null)
                message = "No executable name specified.";
            else if (model.Description == null)
                message = "No description specified.";
            else if (!"compile,compare,run".Split(',').Contains(model.Type))
                message = "Error type specified.";

            if (message != null)
            {
                ModelState.AddModelError("EXEC", message);
                return View(model);
            }

            // creation
            var (zip, md5) = await model.Archive.ReadAsync();

            var e = await Store.CreateAsync(new Executable
            {
                Description = model.Description,
                ExecId = model.ExecId,
                Type = model.Type,
                ZipFile = zip,
                Md5sum = md5,
                ZipSize = zip.Length,
            });

            StatusMessage = $"Executable {e.ExecId} uploaded successfully.";
            await HttpContext.AuditAsync("created", e.ExecId);
            return RedirectToAction("Detail", new { execid = e.ExecId });
        }


        [HttpGet("{execid}/[action]")]
        [ActionName("Content")]
        public async Task<IActionResult> ViewContent(string execid)
        {
            var exec = await Store.FindAsync(execid);
            if (exec is null) return NotFound();
            ViewBag.Executable = exec;
            var items = await Store.FetchContentAsync(exec);
            return View(items);
        }


        [HttpPost("{execid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string execid, int inajax)
        {
            var exec = await Store.FindAsync(execid);
            if (exec == null) return NotFound();

            try
            {
                await Store.DeleteAsync(exec);
                StatusMessage = $"Executable {execid} deleted successfully.";
                await HttpContext.AuditAsync(execid, "delete");
            }
            catch
            {
                StatusMessage = $"Error deleting executable {execid}, foreign key constraints failed.";
            }

            return RedirectToAction(nameof(List));
        }


        [HttpGet("{execid}/[action]")]
        public async Task<IActionResult> Download(string execid)
        {
            var bytes = await Store.FindAsync(execid);
            if (bytes is null) return NotFound();
            return File(bytes.ZipFile, "application/zip", $"{execid}.zip", false);
        }
    }
}
