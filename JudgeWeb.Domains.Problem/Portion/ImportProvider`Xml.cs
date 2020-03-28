using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using JudgeWeb.Features.Storage;
using Markdig;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

[assembly: Inject(typeof(XmlImportProvider))]
namespace JudgeWeb.Domains.Problems
{
    public class XmlImportProvider : IImportProvider
    {
        public StringBuilder LogBuffer { get; }
        public IProblemStore Store { get; }
        public IMarkdownService Markdown { get; }
        public Problem Problem { get; private set; }
        public ILogger<XmlImportProvider> Logger { get; }
        public IStaticFileRepository StaticFiles { get; }

        static readonly Dictionary<string, string> nodes = new Dictionary<string, string>
        {
            ["description"] = "description.md",
            ["input"] = "inputdesc.md",
            ["output"] = "outputdesc.md",
            ["hint"] = "hint.md",
        };

        static readonly (string, bool)[] testcaseGroups = new[] { ("samples", false), ("test_cases", true) };

        public XmlImportProvider(
            IProblemStore store,
            ILogger<XmlImportProvider> logger,
            IMarkdownService markdownService,
            IStaticFileRepository io2)
        {
            LogBuffer = new StringBuilder();
            Store = store;
            Logger = logger;
            Markdown = markdownService;
            StaticFiles = io2;
        }

        private void Log(string log)
        {
            Logger.LogInformation(log);
            LogBuffer.AppendLine(log);
        }

        private async Task LoadStatementsAsync(XElement element, string fileName)
        {
            if (string.IsNullOrEmpty(element?.Value)) return;
            string mdcontent = element.Value;
            var tags = $"p{Problem.ProblemId}";
            var content = await (Markdown, StaticFiles).ImportWithImagesAsync(mdcontent, tags);
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

            var doc = document.Root;

            Problem = await Store.CreateAsync(new Problem
            {
                Title = doc.Element("title").Value,
                MemoryLimit = int.Parse(doc.Element("memory_limit").Value),
                TimeLimit = int.Parse(doc.Element("time_limit").Value),
                AllowJudge = true,
                AllowSubmit = false,
                CompareScript = "compare",
                RunScript = "run",
                OutputLimit = 4096,
                Source = doc.Element("author")?.Value ?? username,
            });

            Log($"Problem p{Problem.ProblemId} created.");
            Directory.CreateDirectory($"Problems/p{Problem.ProblemId}");

            // Write all markdown files into folders.
            foreach (var (nodeName, fileName) in nodes)
                await LoadStatementsAsync(doc.Element(nodeName), fileName);

            // Add testcases.
            int tot = 0;
            foreach (var (tcgName, isSecret) in testcaseGroups)
            {
                foreach (XElement testcase in doc.Element(tcgName).Elements())
                {
                    var test = new MemoryTestCase(
                        desc: (string)testcase.Element("desc"),
                        input: (string)testcase.Element("input"),
                        output: (string)testcase.Element("output"),
                        point: (int)testcase.Element("point"));

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

            return Problem;
        }
    }
}
