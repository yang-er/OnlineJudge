﻿using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using JudgeWeb.Features;
using Markdig;
using Markdig.Syntax.Inlines;
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
using FileUtils = System.IO.File;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]/{pid}/[controller]")]
    public class ExportController : Controller3
    {
        IMarkdownService Markdown { get; set; }

        public ExportController(AppDbContext db, IMarkdownService markdown) : base(db, true)
        {
            Markdown = markdown;
        }

        private class ExportSubmission
        {
            public Verdict? ExpectedResult { get; set; }
            public string FileExtension { get; set; }
            public string SourceCode { get; set; }
            public int SubmissionId { get; set; }
        }

        private class ExportTestcase
        {
            public int Rank { get; set; }
            public int TestcaseId { get; set; }
            public int Point { get; set; }
            public string Description { get; set; }
            public bool IsSecret { get; set; }
        }

        private static readonly IReadOnlyDictionary<Verdict, string> KattisVerdict
            = new Dictionary<Verdict, string>
            {
                [Verdict.Accepted] = "accepted",
                [Verdict.WrongAnswer] = "wrong_answer",
                [Verdict.TimeLimitExceeded] = "time_limit_exceeded",
                [Verdict.RuntimeError] = "run_time_error",
                [Verdict.MemoryLimitExceeded] = "run_time_error",
            };

        private Task AttachSubmission(ZipArchive zip, ExportSubmission sub)
        {
            var verd = sub.ExpectedResult ?? Verdict.Unknown;
            string result = KattisVerdict.GetValueOrDefault(verd, "ignore");
            zip.CreateEntryFromByteArray(
                content: Convert.FromBase64String(sub.SourceCode),
                entry: $"submissions/{result}/s{sub.SubmissionId}.{sub.FileExtension}");
            return Task.CompletedTask;
        }

        private Task AttachTestcase(ZipArchive zip, ExportTestcase tc)
        {
            var prefix = $"data/{(tc.IsSecret ? "secret" : "sample")}/{tc.Rank}";
            var localPrefix = $"Problems/p{Problem.ProblemId}/t{tc.TestcaseId}";
            zip.CreateEntryFromFile(localPrefix + ".in", prefix + ".in");
            zip.CreateEntryFromFile(localPrefix + ".out", prefix + ".ans");
            if (tc.Description != $"{tc.Rank}")
                zip.CreateEntryFromString(tc.Description, prefix + ".desc");
            if (tc.Point != 0)
                zip.CreateEntryFromString($"{tc.Point}", prefix + ".point");
            return Task.CompletedTask;
        }

        private async Task AttachMarkdownFile(ZipArchive zip, string mdname)
        {
            var markdownFile = $"Problems/p{Problem.ProblemId}/{mdname}.md";
            if (!FileUtils.Exists(markdownFile)) return;
            var mdContent = await FileUtils.ReadAllTextAsync(markdownFile);
            var news = await Markdown.ExportWithImagesAsync(mdContent);
            zip.CreateEntryFromString(news, $"problem_statement/{mdname}.md");
        }

        private Task AttachExecutable(ZipArchive zip, Executable exec)
        {
            var subdir = $"output_validators/p{Problem.ProblemId}{(exec.Type == "run" ? "run" : "cmp")}/";
            using (var itt = new MemoryStream(exec.ZipFile))
            using (var zpp = new ZipArchive(itt))
                foreach (var ent in zpp.Entries)
                    using (var rds = ent.Open())
                    using (var wrs = zip.CreateEntry(Path.Combine(subdir, ent.Name)).Open())
                        rds.CopyTo(wrs);
            return Task.CompletedTask;
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Problem")]
        public async Task<IActionResult> Export(int pid)
        {
            var testc = await
            (
                from t in DbContext.Testcases
                where t.ProblemId == pid
                orderby t.Rank ascending
                select new ExportTestcase
                {
                    Rank = t.Rank,
                    TestcaseId = t.TestcaseId,
                    Point = t.Point,
                    Description = t.Description,
                    IsSecret = t.IsSecret
                }
            )
            .ToListAsync();

            var subs = await
            (
                from s in DbContext.Submissions
                where s.ProblemId == pid && s.ExpectedResult != null
                join l in DbContext.Languages on s.Language equals l.Id
                select new ExportSubmission
                {
                    ExpectedResult = s.ExpectedResult,
                    FileExtension = l.FileExtension,
                    SourceCode = s.SourceCode,
                    SubmissionId = s.SubmissionId
                }
            )
            .ToListAsync();

            var toFind = new List<string>();
            if (Problem.CompareScript != "compare")
                toFind.Add(Problem.CompareScript);
            if (Problem.RunScript != "run")
                toFind.Add(Problem.RunScript);
            var execs = await DbContext.Executable
                .Where(e => toFind.Contains(e.ExecId))
                .ToListAsync();

            var memStream = new MemoryStream();

            using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, true))
            {
                foreach (var tc in testc)
                    await AttachTestcase(zip, tc);
                foreach (var sub in subs)
                    await AttachSubmission(zip, sub);
                foreach (var st in MarkdownFiles)
                    await AttachMarkdownFile(zip, st);
                foreach (var exec in execs)
                    await AttachExecutable(zip, exec);

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
                if (Problem.RunScript != "run")
                    sb.AppendLine("validation: custom interactive");
                else if (Problem.CompareScript != "compare")
                    sb.AppendLine("validation: custom");
                zip.CreateEntryFromString(sb.ToString(), "problem.yaml");

                zip.CreateEntryFromString(
                    content: $"timelimit = {Problem.TimeLimit / 1000.0}\n",
                    entry: "domjudge-problem.ini");
            }

            memStream.Position = 0;
            return File(memStream, "application/zip", $"p{pid}.zip", false);
        }
    }
}
