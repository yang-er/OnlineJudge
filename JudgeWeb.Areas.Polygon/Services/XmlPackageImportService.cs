using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using JudgeWeb.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

[assembly: Inject(typeof(XmlPackageImportService))]
namespace JudgeWeb.Areas.Polygon.Services
{
    public class XmlPackageImportService : IProblemImportService
    {
        public StringBuilder LogBuffer { get; }
        public AppDbContext DbContext { get; }
        public IMarkdownService Markdown { get; }
        public Problem Problem { get; private set; }
        public ILogger<XmlPackageImportService> Logger { get; }

        static readonly Dictionary<string, string> nodes = new Dictionary<string, string>
        {
            ["description"] = "description.md",
            ["input"] = "inputdesc.md",
            ["output"] = "outputdesc.md",
            ["hint"] = "hint.md",
        };

        static readonly (string, bool)[] testcaseGroups = new[] { ("samples", false), ("test_cases", true) };

        public XmlPackageImportService(
            AppDbContext adbc,
            ILogger<XmlPackageImportService> logger,
            IMarkdownService markdownService)
        {
            LogBuffer = new StringBuilder();
            DbContext = adbc;
            Logger = logger;
            Markdown = markdownService;
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
            var content = await Markdown.ImportWithImagesAsync(mdcontent, tags);
            await File.WriteAllTextAsync($"Problems/{tags}/{fileName}", content);
        }

        public async Task<Problem> ImportAsync(IFormFile zipFile, string username)
        {
            if (zipFile == null) throw new ArgumentNullException(nameof(zipFile));
            XDocument document;

            using (var stream = zipFile.OpenReadStream())
            using (var sr = new StreamReader(stream))
            {
                var content = await sr.ReadToEndAsync();
                document = XDocument.Parse(content);
            }

            var doc = document.Root;

            var p = DbContext.Problems.Add(new Problem
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

            await DbContext.SaveChangesAsync();
            Problem = p.Entity;
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
                    var test = new TestCase(
                        desc: (string)testcase.Element("desc"),
                        input: (string)testcase.Element("input"),
                        output: (string)testcase.Element("output"),
                        point: (int)testcase.Element("point"));

                    tot++;
                    var input = Encoding.UTF8.GetBytes(test.Input);
                    var inputHash = input.ToMD5().ToHexDigest(true);
                    var output = Encoding.UTF8.GetBytes(test.Output);
                    var outputHash = output.ToMD5().ToHexDigest(true);

                    var tcc = DbContext.Testcases.Add(new Testcase
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

                    await DbContext.SaveChangesAsync();
                    await File.WriteAllBytesAsync(
                        path: $"Problems/p{Problem.ProblemId}/t{tcc.Entity.TestcaseId}.in", 
                        bytes: input);
                    await File.WriteAllBytesAsync(
                        path: $"Problems/p{Problem.ProblemId}/t{tcc.Entity.TestcaseId}.out",
                        bytes: output);
                }
            }

            return Problem;
        }
    }
}
