using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using JudgeWeb.Features.Storage;
using Markdig;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

[assembly: Inject(typeof(FpsImportProvider))]
namespace JudgeWeb.Domains.Problems
{
    public class FpsImportProvider : IImportProvider
    {
        public StringBuilder LogBuffer { get; }
        public IProblemStore Store { get; }
        public ILanguageStore Languages { get; }
        public IMarkdownService Markdown { get; }
        public Problem Problem { get; private set; }
        public ILogger<FpsImportProvider> Logger { get; }
        public IStaticFileRepository StaticFiles { get; }
        public ISubmissionStore Submits { get; }

        static readonly Dictionary<string, string> nodes = new Dictionary<string, string>
        {
            ["description"] = "description.md",
            ["input"] = "inputdesc.md",
            ["output"] = "outputdesc.md",
            ["hint"] = "hint.md",
        };

        static readonly (string, bool, string)[] testcaseGroups = new[]
        {
            ("sample_input", false, "sample_output"),
            ("test_input", true, "test_output")
        };

        public FpsImportProvider(
            IProblemStore store,
            ILogger<FpsImportProvider> logger,
            IMarkdownService markdownService,
            IStaticFileRepository io2,
            ILanguageStore langs,
            ISubmissionStore submits)
        {
            LogBuffer = new StringBuilder();
            Store = store;
            Logger = logger;
            Markdown = markdownService;
            StaticFiles = io2;
            Languages = langs;
            Submits = submits;
        }

        private void Log(string log)
        {
            Logger.LogInformation(log);
            LogBuffer.AppendLine(log);
        }

        private async Task LoadStatementsAsync(XElement element, string fileName)
        {
            if (string.IsNullOrEmpty(element?.Value)) return;
            var content = element.Value;
            await Store.WriteFileAsync(Problem, fileName, content);
        }

        public async Task<Problem> ImportAsync(Stream stream, string uploadFileName, string username)
        {
            XDocument document;

            using (var sr = new StreamReader(stream))
            {
                var content = await sr.ReadToEndAsync();
                document = XDocument.Parse(content);
            }

            var doc2 = document.Root;

            if (doc2.Elements("item").Count() == 0)
                throw new Exception("No problems.");
            if (doc2.Elements("item").Count() > 1)
                Log("Uploading multiple problems, showing last only.");

            var langs = await Languages.ListAsync();

            foreach (var doc in doc2.Elements("item"))
            {
                Problem = await Store.CreateAsync(new Problem
                {
                    Title = ((string)doc.Element("title")) ?? uploadFileName,
                    MemoryLimit = int.Parse(((string)doc.Element("memory_limit")) ?? "128") * 1024,
                    TimeLimit = int.Parse(((string)doc.Element("time_limit")) ?? "10") * 1000,
                    AllowJudge = false,
                    AllowSubmit = false,
                    CompareScript = "compare",
                    RunScript = "run",
                    OutputLimit = 4096,
                    Source = ((string)doc.Element("source")) ?? username,
                });

                Log($"Problem p{Problem.ProblemId} created.");
                Directory.CreateDirectory($"Problems/p{Problem.ProblemId}");

                // Write all markdown files into folders.
                foreach (var (nodeName, fileName) in nodes)
                    await LoadStatementsAsync(doc.Element(nodeName), fileName);

                // Add testcases.
                int tot = 0;
                foreach (var (tcgName, isSecret, nextEleName) in testcaseGroups)
                {
                    foreach (XElement inputNode in doc.Elements(tcgName))
                    {
                        var outputNodeE = inputNode.NextNode;
                        if (!(outputNodeE is XElement outputNode) || outputNode.Name != nextEleName)
                        {
                            Log($"Unknown node at {tot}.");
                            continue;
                        }

                        var test = new MemoryTestCase(
                            desc: $"{tot + 1}",
                            input: (string)inputNode,
                            output: (string)outputNode,
                            point: 0);

                        tot++;
                        var input = Encoding.UTF8.GetBytes(test.Input);
                        var inputHash = input.ToMD5().ToHexDigest(true);
                        var output = Encoding.UTF8.GetBytes(test.Output);
                        var outputHash = output.ToMD5().ToHexDigest(true);

                        var tc = await Store.CreateAsync(new Testcase
                        {
                            InputLength = input.Length,
                            OutputLength = output.Length,
                            Point = test.Point,
                            ProblemId = Problem.ProblemId,
                            Rank = tot,
                            Description = test.Description,
                            IsSecret = isSecret,
                            Md5sumInput = inputHash,
                            Md5sumOutput = outputHash,
                        });

                        await Store.WriteFileAsync(Problem, $"t{tc.TestcaseId}.in", input);
                        await Store.WriteFileAsync(Problem, $"t{tc.TestcaseId}.out", output);
                    }
                }

                // Add solutions
                foreach (var submission in doc.Elements("solution"))
                {
                    var langName = submission.Attribute("language").Value;
                    var lang = langs.FirstOrDefault(l => l.Name == langName);
                    if (lang == null) lang = langs.FirstOrDefault();

                    var content = submission.Value;
                    var s = await Submits.CreateAsync(
                        code: content,
                        language: lang.Id,
                        problemId: Problem.ProblemId,
                        contestId: null,
                        userId: 0,
                        ipAddr: System.Net.IPAddress.Parse("127.0.0.1"),
                        via: "polygon-page",
                        username: username,
                        expected: Verdict.Unknown);

                    Log($"Submission s{s.SubmissionId} created.");
                }

                Problem.AllowJudge = true;
                await Store.UpdateAsync(Problem);
            }

            return Problem;
        }
    }
}
