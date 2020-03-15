using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private IProblemFileRepository IoContext { get; }

        public DescriptionController(AppDbContext db, IProblemFileRepository io)
            : base(db, true) => IoContext = io;


        [NonAction]
        public async Task<string> GenerateViewAsync()
        {
            var generator = HttpContext.RequestServices
                .GetRequiredService<IProblemViewProvider>();
            var statement = await generator
                .LoadStatement(Problem, DbContext.Testcases);
           return generator.BuildHtml(statement).ToString();
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
                var fileInfo = IoContext.GetFileInfo($"p{pid}/view.html");
                ViewData["Content"] = await fileInfo.ReadAsync() ?? "";
            }

            ViewData["Id"] = pid;
            return View();
        }


        [HttpGet("{target}")]
        public async Task<IActionResult> Markdown(string target, int pid)
        {
            if (!MarkdownFiles.Contains(target))
                return NotFound();
            var fileInfo = IoContext.GetFileInfo($"p{pid}/{target}.md");
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
            if (!MarkdownFiles.Contains(target))
                return NotFound();
            if (target != model.Target || $"p{pid}" != model.BackingStore)
                return BadRequest();
            await IoContext.WriteStringAsync($"p{pid}/{target}.md", model.Markdown);
            StatusMessage = "Description saved.";
            return RedirectToAction();
        }


        [HttpGet]
        public async Task<IActionResult> Generate(int pid)
        {
            var content = await GenerateViewAsync();
            await IoContext.WriteStringAsync($"p{pid}/view.html", content);
            StatusMessage = "Problem description saved successfully.";
            return RedirectToAction(nameof(Preview), new { @new = false });
        }


        [HttpGet]
        public async Task<IActionResult> GenerateLatex(int pid,
            [FromServices] IProblemViewProvider generator)
        {
            var statement = await generator.LoadStatement(Problem, DbContext.Testcases);
            var memstream = new MemoryStream();
            using (var zip = new ZipArchive(memstream, ZipArchiveMode.Create, true))
                generator.BuildLatex(zip, statement);
            memstream.Position = 0;
            return File(memstream, "application/zip", $"p{pid}-statements.zip");
        }
    }
}
