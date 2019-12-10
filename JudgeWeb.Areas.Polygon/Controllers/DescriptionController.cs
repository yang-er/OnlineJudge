using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using JudgeWeb.Features.Problem;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]/{pid}/[controller]/[action]")]
    public class DescriptionController : Controller3
    {
        private IFileRepository IoContext { get; }

        private static readonly string[] AcceptableTarget =
            new[] { "description", "inputdesc", "outputdesc", "hint" };

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

            int pid = Problem.ProblemId;
            var description = await IoContext
                .ReadPartAsync($"p{pid}", "description.md");
            description = description ?? await IoContext
                .ReadPartAsync($"p{pid}", "compact.html");
            description = description ?? "";
            var inputdesc = await IoContext
                .ReadPartAsync($"p{pid}", "inputdesc.md") ?? "";
            var outputdesc = await IoContext
                .ReadPartAsync($"p{pid}", "outputdesc.md") ?? "";
            var hint = await IoContext
                .ReadPartAsync($"p{pid}", "hint.md") ?? "";

            var testcases = await DbContext.Testcases
                .Where(t => t.ProblemId == pid && !t.IsSecret)
                .OrderBy(t => t.Rank)
                .ToListAsync();
            var samples = new List<TestCase>();

            foreach (var item in testcases)
            {
                var input = await IoContext.ReadPartAsync($"p{pid}", $"t{item.TestcaseId}.in");
                var output = await IoContext.ReadPartAsync($"p{pid}", $"t{item.TestcaseId}.out");
                samples.Add(new TestCase(item.Description, input, output, item.Point));
            }

           return generator
                .Build(description, inputdesc, outputdesc, hint, Problem, samples)
                .ToString();
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
            if (!AcceptableTarget.Contains(target))
                return NotFound();
            var backstore = "p" + pid;
            var lastVersion = await IoContext
                .ReadPartAsync(backstore, target + ".md") ?? "";

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
            if (!AcceptableTarget.Contains(target))
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
    }
}
