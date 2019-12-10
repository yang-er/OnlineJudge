using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Export(int pid)
        {
            var testc = await DbContext.Testcases
                .Where(t => t.ProblemId == pid)
                .Select(t => new { t.Rank, t.TestcaseId, t.Point, t.Description, t.IsSecret })
                .OrderBy(t => t.Rank)
                .ToListAsync();

            var subs = await DbContext.Submissions
                .Where(s => s.ProblemId == pid && s.ExpectedResult != null)
                .Join(
                    inner: DbContext.Languages,
                    outerKeySelector: s => s.Language,
                    innerKeySelector: l => l.LangId,
                    resultSelector: (s, l) => new { s.ExpectedResult, l.FileExtension, s.SourceCode, s.SubmissionId })
                .ToListAsync();

            Executable cmp = null, run = null;

            if (Problem.CompareScript != "compare")
            {
                var cmps = Problem.CompareScript;
                cmp = await DbContext.Executable.Where(e => e.ExecId == cmps).FirstAsync();
            }

            if (Problem.RunScript != "run")
            {
                var runs = Problem.CompareScript;
                run = await DbContext.Executable.Where(e => e.ExecId == runs).FirstAsync();
            }

            var stat = new[] { "description", "inputdesc", "outputdesc", "hint" };
            var memStream = new MemoryStream();

            using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, true))
            {
                foreach (var tc in testc)
                {
                    var prefix = $"data/{(tc.IsSecret ? "secret" : "sample")}/{tc.Rank}";
                    zip.CreateEntryFromFile($"Problems/p{pid}/t{tc.TestcaseId}.in", prefix + ".in");
                    zip.CreateEntryFromFile($"Problems/p{pid}/t{tc.TestcaseId}.out", prefix + ".ans");
                    if (tc.Description != $"{tc.Rank}")
                        zip.CreateEntryFromString(tc.Description, prefix + ".desc");
                    if (tc.Point != 0)
                        zip.CreateEntryFromString($"{tc.Point}", prefix + ".point");
                }

                foreach (var sub in subs)
                {
                    var verd = sub.ExpectedResult ?? Verdict.Unknown;
                    string result;
                    if (verd == Verdict.Accepted) result = "accepted";
                    else if (verd == Verdict.WrongAnswer) result = "wrong_answer";
                    else if (verd == Verdict.TimeLimitExceeded) result = "time_limit_exceeded";
                    else if (verd == Verdict.RuntimeError) result = "run_time_error";
                    else if (verd == Verdict.MemoryLimitExceeded) result = "run_time_error";
                    else result = "ignore";

                    zip.CreateEntryFromByteArray(
                        content: Convert.FromBase64String(sub.SourceCode),
                        entry: $"submissions/{result}/s{sub.SubmissionId}.{sub.FileExtension}");
                }

                foreach (var st in stat)
                {
                    if (System.IO.File.Exists($"Problems/p{pid}/{st}.md"))
                        zip.CreateEntryFromFile($"Problems/p{pid}/{st}.md", $"problem_statement/{st}.md");
                }

                if (System.IO.File.Exists($"Problems/p{pid}/view.html"))
                    zip.CreateEntryFromFile($"Problems/p{pid}/view.html", $"problem.html");

                if (cmp != null)
                {
                    var prefix = $"output_validators/p{pid}cmp/";
                    using (var itt = new MemoryStream(cmp.ZipFile))
                    using (var zpp = new ZipArchive(itt))
                        foreach (var ent in zpp.Entries)
                            using (var rds = ent.Open())
                            using (var wrs = zip.CreateEntry(prefix + ent.Name).Open())
                                rds.CopyTo(wrs);
                }

                if (run != null)
                {
                    var prefix = $"output_validators/p{pid}run/";
                    using (var itt = new MemoryStream(run.ZipFile))
                    using (var zpp = new ZipArchive(itt))
                        foreach (var ent in zpp.Entries)
                            using (var rds = ent.Open())
                            using (var wrs = zip.CreateEntry(prefix + ent.Name).Open())
                                rds.CopyTo(wrs);
                }

                var sb = new StringBuilder();
                sb.AppendLine("name: " + Problem.Title);
                if (!string.IsNullOrEmpty(Problem.Source))
                    sb.AppendLine("source: " + Problem.Source);
                sb.AppendLine();
                sb.AppendLine("limits:");
                sb.AppendLine("    time: " + (Problem.TimeLimit / 1000.0));
                sb.AppendLine("    memory: " + (Problem.MemoryLimit / 1024));
                if (Problem.OutputLimit != 4096)
                    sb.AppendLine("    output: " + (Problem.OutputLimit / 1024));
                sb.AppendLine();
                if (!string.IsNullOrEmpty(Problem.ComapreArguments))
                    sb.AppendLine("validator_flags: " + Problem.ComapreArguments);
                if (run != null)
                    sb.AppendLine("validation: custom interactive");
                else if (cmp != null)
                    sb.AppendLine("validation: custom");
                zip.CreateEntryFromString(sb.ToString(), "problem.yaml");

                zip.CreateEntryFromString(
                    content: $"timelimit = {Problem.TimeLimit / 1000.0}\n",
                    entry: "domjudge-problem.ini");
            }

            memStream.Position = 0;
            return File(memStream, "application/zip", $"p{pid}.zip", false);
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
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Delete(int pid)
        {
            DbContext.Problems.Remove(Problem);
            await DbContext.SaveChangesAsync();
            StatusMessage = $"Problem {pid} deleted successfully.";
            return RedirectToAction("List", "Root");
        }
    }
}
