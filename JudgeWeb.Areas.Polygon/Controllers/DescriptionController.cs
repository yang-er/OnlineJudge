using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private IFileRepository IoContext { get; }

        public DescriptionController(AppDbContext db, IFileRepository io) : base(db, true)
        {
            IoContext = io;
            io.SetContext("Problems");
        }


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
                ViewData["Content"] = await IoContext.ReadPartAsync($"p{pid}", "view.html");
            }

            ViewData["Id"] = pid;
            return View();
        }


        [HttpGet("{target}")]
        public async Task<IActionResult> Markdown(string target, int pid)
        {
            if (!MarkdownFiles.Contains(target))
                return NotFound();
            var backstore = "p" + pid;
            var lastVersion = await IoContext
                .ReadPartAsync(backstore, target + ".md") ?? "";
            ViewBag.ProblemId = pid;

            return View(new MarkdownModel
            {
                Markdown = lastVersion,
                BackingStore = backstore,
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
            var backstore = "p" + pid;
            if (target != model.Target || backstore != model.BackingStore)
                return BadRequest();
            await IoContext.WritePartAsync(backstore, target + ".md", model.Markdown);
            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> Generate(int pid)
        {
            var content = await GenerateViewAsync();
            await IoContext.WritePartAsync($"p{pid}", "view.html", content);
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
