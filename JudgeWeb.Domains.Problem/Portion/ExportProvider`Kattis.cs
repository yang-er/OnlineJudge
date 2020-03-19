﻿using JudgeWeb.Data;
using JudgeWeb.Domains.Judgements;
using JudgeWeb.Features;
using JudgeWeb.Features.Storage;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems.Portion
{
    public class KattisExportProvider : IExportProvider
    {
        private IMarkdownService Markdown { get; }

        private IProblemFileRepository ProblemFile { get; }

        private IStaticFileRepository StaticFile { get; }

        private IProblemStore Store { get; }

        private ISubmissionRepository Submissions { get; }

        public KattisExportProvider(
            IProblemStore store,
            IProblemFileRepository io,
            IMarkdownService markdown,
            IStaticFileRepository io2)
        {
            Store = store;
            ProblemFile = io;
            StaticFile = io2;
            Markdown = markdown;
        }

        private class ExportSubmission
        {
            public Verdict? ExpectedResult { get; set; }
            public string FileExtension { get; set; }
            public string SourceCode { get; set; }
            public int SubmissionId { get; set; }
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

        private async Task AttachTestcase(ZipArchive zip, int pid, Testcase tc)
        {
            var prefix = $"data/{(tc.IsSecret ? "secret" : "sample")}/{tc.Rank}";
            var localPrefix = $"p{pid}/t{tc.TestcaseId}";

            var inputFile = ProblemFile.GetFileInfo(localPrefix + ".in");
            using (var inputFile2 = inputFile.CreateReadStream())
                await zip.CreateEntryFromStream(inputFile2, prefix + ".in");
            var outputFile = ProblemFile.GetFileInfo(localPrefix + ".out");
            using (var outputFile2 = outputFile.CreateReadStream())
                await zip.CreateEntryFromStream(outputFile2, prefix + ".ans");
            if (tc.Description != $"{tc.Rank}")
                zip.CreateEntryFromString(tc.Description, prefix + ".desc");
            if (tc.Point != 0)
                zip.CreateEntryFromString($"{tc.Point}", prefix + ".point");
        }

        private async Task AttachMarkdownFile(ZipArchive zip, int pid, string mdname)
        {
            var file = ProblemFile.GetFileInfo($"p{pid}/{mdname}.md");
            if (!file.Exists) return;
            var mdContent = await file.ReadAsync();
            var news = await (Markdown, StaticFile).ExportWithImagesAsync(mdContent);
            zip.CreateEntryFromString(news, $"problem_statement/{mdname}.md");
        }

        private async Task AttachExecutable(ZipArchive zip, int pid, Executable exec)
        {
            var subdir = $"output_validators/p{pid}{(exec.Type == "run" ? "run" : "cmp")}/";
            using var itt = new MemoryStream(exec.ZipFile);
            using var zpp = new ZipArchive(itt);

            foreach (var ent in zpp.Entries)
            {
                using var rds = ent.Open();
                var t = await zip.CreateEntryFromStream(rds, Path.Combine(subdir, ent.Name));
                t.ExternalAttributes = ent.ExternalAttributes;
            }
        }

        public async ValueTask<(Stream stream, string mime, string fileName)> ExportAsync(Problem problem)
        {
            var testc = await Store.ListTestcasesAsync(problem.ProblemId);

            var execs = new List<Executable>();
            if (problem.CompareScript != "compare")
                execs.Add(await Store.FindExecutableAsync(problem.CompareScript));
            if (problem.RunScript != "run")
                execs.Add(await Store.FindExecutableAsync(problem.RunScript));

            var langs = (await Store.ListLanguagesAsync())
                .ToDictionary(l => l.Id, l => l.FileExtension);

            var subs = await Submissions.ListAsync(
                predicate: s => s.ProblemId == problem.ProblemId && s.ExpectedResult != null,
                projection: s => new ExportSubmission
                {
                    ExpectedResult = s.ExpectedResult,
                    SourceCode = s.SourceCode,
                    SubmissionId = s.SubmissionId,
                    FileExtension = langs[s.Language]
                });

            var memStream = new MemoryStream();

            using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, true))
            {
                foreach (var tc in testc)
                    await AttachTestcase(zip, problem.ProblemId, tc);
                foreach (var sub in subs)
                    await AttachSubmission(zip, sub);
                foreach (var st in IProblemStore.MarkdownFiles)
                    await AttachMarkdownFile(zip, problem.ProblemId, st);
                foreach (var exec in execs)
                    await AttachExecutable(zip, problem.ProblemId, exec);

                var sb = new StringBuilder();
                sb.AppendLine("name: " + problem.Title);
                if (!string.IsNullOrEmpty(problem.Source))
                    sb.AppendLine("source: " + problem.Source);
                sb.AppendLine();
                sb.AppendLine("limits:");
                sb.AppendLine("    time: " + (problem.TimeLimit / 1000.0));
                sb.AppendLine("    memory: " + (problem.MemoryLimit / 1024));
                if (problem.OutputLimit != 4096)
                    sb.AppendLine("    output: " + (problem.OutputLimit / 1024));
                sb.AppendLine();
                if (!string.IsNullOrEmpty(problem.ComapreArguments))
                    sb.AppendLine("validator_flags: " + problem.ComapreArguments);
                if (problem.RunScript != "run")
                    sb.AppendLine("validation: custom interactive");
                else if (problem.CompareScript != "compare")
                    sb.AppendLine("validation: custom");
                zip.CreateEntryFromString(sb.ToString(), "problem.yaml");

                zip.CreateEntryFromString(
                    content: $"timelimit = {problem.TimeLimit / 1000.0}\n",
                    entry: "domjudge-problem.ini");
            }

            memStream.Position = 0;
            return (memStream, "application/zip", $"p{problem.ProblemId}.zip");
        }
    }
}
