using JudgeWeb.Data;
using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Features;
using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    public partial class ProblemController
    {
        private async Task LoadTestcaseInfo(int pid)
        {
            ViewData["Testcase"] = await DbContext.Testcases
                .Where(t => t.ProblemId == pid)
                .OrderBy(t => t.Rank)
                .WithoutBlob()
                .ToListAsync();
        }

        [Authorize(Roles = privilege)]
        [HttpGet("{ppid}")]
        public async Task<IActionResult> Edit(string ppid)
        {
            if (ppid == "add")
            {
                var probcnt = await DbContext.Problems.CountAsync();

                var model = new Problem
                {
                    ProblemId = probcnt + 1001,
                    Source = "",
                    CompareScript = "compare",
                    RunScript = "run",
                    Flag = 1,
                    MemoryLimit = 262144,
                    TimeLimit = 5000,
                    Title = "未发布问题",
                };

                DbContext.Problems.Add(model);
                await DbContext.SaveChangesAsync();
                return RedirectToAction("Edit", "Problem", new { area = "Judge", ppid = model.ProblemId.ToString() });
            }
            else if (int.TryParse(ppid, out int pid))
            {
                var prob = await DbContext.Problems
                    .Where(p => p.ProblemId == pid)
                    .Select(p => new ProblemEditModel
                    {
                        ProblemId = p.ProblemId,
                        CompareScript = p.CompareScript,
                        RunScript = p.RunScript,
                        Source = p.Source,
                        MemoryLimit = p.MemoryLimit,
                        TimeLimit = p.TimeLimit,
                        Title = p.Title,
                        IsActive = p.Flag == 0,
                    })
                    .FirstOrDefaultAsync();
                if (prob is null) return NotFound();

                await LoadTestcaseInfo(pid);
                return View(prob);
            }
            else
            {
                return BadRequest();
            }
        }

        [Authorize(Roles = privilege)]
        [HttpPost("{pid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int pid, ProblemEditModel model)
        {
            if (ModelState.ErrorCount == 0)
            {
                var prob = await DbContext.Problems
                    .Where(p => p.ProblemId == pid)
                    .FirstOrDefaultAsync();

                prob.MemoryLimit = model.MemoryLimit;
                prob.TimeLimit = model.TimeLimit;
                prob.RunScript = model.RunScript;
                prob.CompareScript = model.CompareScript;
                prob.Flag = model.IsActive ? 0 : 1;
                prob.Source = model.Source;
                prob.Title = model.Title;
                DbContext.Problems.Update(prob);
                await DbContext.SaveChangesAsync();

                ViewData["MsgType"] = "success";
                ViewData["Message"] = "Problem modified successfully.";

                var description = await IoContext
                    .ReadPartAsync($"p{pid}", "description.md") ?? "";
                var inputdesc = await IoContext
                    .ReadPartAsync($"p{pid}", "inputdesc.md") ?? "";
                var outputdesc = await IoContext
                    .ReadPartAsync($"p{pid}", "outputdesc.md") ?? "";
                var hint = await IoContext
                    .ReadPartAsync($"p{pid}", "hint.md") ?? "";

                var mdpl = HttpContext.RequestServices
                    .GetRequiredService<IMarkdownService>();

                var htmlBuilder = new StringBuilder();
                htmlBuilder.AppendLine($"<h1>{model.Title}</h1>");

                htmlBuilder.AppendLine("<ul class=\"list-unstyled\">");
                htmlBuilder.AppendLine("  <li>Input file: <em>standard input</em></li>");
                htmlBuilder.AppendLine("  <li>Output file: <em>standard output</em></li>");
                htmlBuilder.AppendLine($"  <li>Time limit: {model.TimeLimit}ms</li>");
                htmlBuilder.AppendLine($"  <li>Memory limit: {model.MemoryLimit}k</li>");
                //htmlBuilder.AppendLine("  <li>Submissions: {SubmissionDetails}</li>");
                htmlBuilder.AppendLine("</ul>");
                htmlBuilder.AppendLine();

                htmlBuilder.AppendLine("<div id=\"problem-descibe\">");

                if (!string.IsNullOrEmpty(description))
                {
                    htmlBuilder.AppendLine("<h3>Description</h3>");
                    htmlBuilder.AppendLine(mdpl.Render(description));
                }

                if (!string.IsNullOrEmpty(inputdesc))
                {
                    htmlBuilder.AppendLine("<h3>Input</h3>");
                    htmlBuilder.AppendLine(mdpl.Render(inputdesc));
                }

                if (!string.IsNullOrEmpty(outputdesc))
                {
                    htmlBuilder.AppendLine("<h3>Output</h3>");
                    htmlBuilder.AppendLine(mdpl.Render(outputdesc));
                }

                var samples = await DbContext.Testcases
                    .Where(t => !t.IsSecret && t.ProblemId == pid)
                    .Select(t => new { t.Input, t.Output })
                    .ToListAsync();

                if (samples.Count > 0)
                {
                    htmlBuilder.AppendLine("<h3>Sample</h3>");

                    foreach (var item in samples)
                    {
                        var _in = Encoding.UTF8.GetString(item.Input);
                        var _out = Encoding.UTF8.GetString(item.Output);
                        htmlBuilder.AppendLine("<div class=\"samp\">");

                        if (!string.IsNullOrEmpty(_in))
                        {
                            htmlBuilder.AppendLine("<div class=\"input\">");
                            htmlBuilder.AppendLine("<div class=\"title\">Input</div>");
                            htmlBuilder.AppendLine($"<pre>{_in}</pre>");
                            htmlBuilder.AppendLine("</div>");
                        }

                        htmlBuilder.AppendLine("<div class=\"output\">");
                        htmlBuilder.AppendLine("<div class=\"title\">Output</div>");
                        htmlBuilder.AppendLine($"<pre>{_out}</pre>");
                        htmlBuilder.AppendLine("</div>");
                        htmlBuilder.AppendLine("</div>");
                        htmlBuilder.AppendLine();
                    }
                }

                if (!string.IsNullOrEmpty(hint))
                {
                    htmlBuilder.AppendLine("<h3>Hint</h3>");
                    htmlBuilder.AppendLine(mdpl.Render(hint));
                    htmlBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(model.Source))
                {
                    htmlBuilder.AppendLine("<h3>Source</h3>");
                    htmlBuilder.AppendLine($"<p>{model.Source}</p>");
                }

                htmlBuilder.AppendLine("</div>");
                var content = htmlBuilder.ToString();
                await IoContext.WritePartAsync($"p{model.ProblemId}", "view.html", content);
                IoContext.RemovePart($"p{pid}", "export.xml");

                await LoadTestcaseInfo(pid);
                return View(model);
            }
            else
            {
                ViewData["MsgType"] = "danger";

                var errors = new StringBuilder();
                foreach (var msg in ModelState)
                {
                    foreach (var item in msg.Value.Errors)
                    {
                        errors.AppendLine(item.ErrorMessage);
                    }
                }

                ViewData["Message"] = errors;
                await LoadTestcaseInfo(pid);
                return View(model);
            }
        }
    }
}
