using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]/{pid}/[controller]/[action]")]
    public class DescriptionController : Controller3
    {
        public IProblemViewProvider Generator { get; }

        public DescriptionController(IProblemViewProvider generator)
        {
            Generator = generator;
        }


        [NonAction]
        public async Task<string> GenerateViewAsync()
        {
            var statement = await Problems.StatementAsync(Problem);
            return Generator.BuildHtml(statement).ToString();
        }


        [HttpGet]
        public async Task<IActionResult> Preview(int pid, bool @new = false)
        {
            if (@new)
            {
                ViewData["Title"] = "Preview";
                ViewData["Content"] = await GenerateViewAsync();
            }
            else
            {
                ViewData["Title"] = "View";
                var fileInfo = Problems.GetFile(Problem, "view.html");
                ViewData["Content"] = await fileInfo.ReadAsync() ?? "";
            }

            ViewData["Id"] = pid;
            return View();
        }


        [HttpGet("{target}")]
        public async Task<IActionResult> Markdown(string target, int pid)
        {
            if (!IProblemStore.MarkdownFiles.Contains(target))
                return NotFound();
            var fileInfo = Problems.GetFile(Problem, $"{target}.md");
            var lastVersion = await fileInfo.ReadAsync() ?? "";
            ViewBag.ProblemId = pid;

            return View(new MarkdownModel
            {
                Markdown = lastVersion,
                BackingStore = "p" + pid,
                Target = target
            });
        }


        [HttpPost("{target}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Markdown(
            string target, int pid, MarkdownModel model)
        {
            if (!IProblemStore.MarkdownFiles.Contains(target))
                return NotFound();
            if (target != model.Target || $"p{pid}" != model.BackingStore)
                return BadRequest();
            await Problems.WriteFileAsync(Problem, $"{target}.md", model.Markdown);
            StatusMessage = "Description saved.";
            return RedirectToAction();
        }


        [HttpGet]
        public async Task<IActionResult> Generate(int pid)
        {
            var content = await GenerateViewAsync();
            await Problems.WriteFileAsync(Problem, "view.html", content);
            StatusMessage = "Problem description saved successfully.";
            return RedirectToAction(nameof(Preview), new { @new = false });
        }


        [HttpGet]
        public async Task<IActionResult> GenerateLatex(int pid)
        {
            var statement = await Problems.StatementAsync(Problem);
            var memstream = new MemoryStream();
            using (var zip = new ZipArchive(memstream, ZipArchiveMode.Create, true))
                Generator.BuildLatex(zip, statement);
            memstream.Position = 0;
            return File(memstream, "application/zip", $"p{pid}-statements.zip");
        }
    }
}
