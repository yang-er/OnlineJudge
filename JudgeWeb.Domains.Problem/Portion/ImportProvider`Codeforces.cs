using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using JudgeWeb.Features.Storage;
using Markdig;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

[assembly: Inject(typeof(CodeforcesImportProvider))]
namespace JudgeWeb.Domains.Problems
{
    public class CodeforcesImportProvider : IImportProvider
    {
        const int LINUX644 = -2119958528;

        private ILanguageStore Languages { get; }
        private IExecutableStore Executables { get; }
        private IProblemStore Store { get; }
        private ISubmissionStore Submissions { get; }
        private ILogger<CodeforcesImportProvider> Logger { get; }
        private IStaticFileRepository StaticFiles { get; }

        public StringBuilder LogBuffer { get; }

        public Problem Problem { get; private set; }

        public CodeforcesImportProvider(
            IProblemStore store,
            ILanguageStore languages,
            IExecutableStore executables,
            ISubmissionStore submissions,
            ILogger<CodeforcesImportProvider> logger,
            IStaticFileRepository io2)
        {
            Logger = logger;
            Store = store;
            Languages = languages;
            Executables = executables;
            Submissions = submissions;
            LogBuffer = new StringBuilder();
            StaticFiles = io2;
        }

        private void Log(string log)
        {
            Logger.LogInformation(log);
            LogBuffer.AppendLine(log);
        }

        private async Task<Executable> CreateExecutableAsync(
            byte[] contents, string ext, bool cmp)
        {
            var execName = $"p{Problem.ProblemId}{(cmp ? "cmp" : "run")}";
            var testlib = StaticFiles.GetFileInfo("static/testlib.h");
            if (!testlib.Exists) throw new InvalidOperationException("testlib.h not found");

            var stream = new MemoryStream();
            using (var newzip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                var f = newzip.CreateEntryFromByteArray(contents, "main" + ext);
                f.ExternalAttributes = LINUX644;
                var f2 = newzip.CreateEntryFromFile(testlib.PhysicalPath, "testlib.h");
                f2.ExternalAttributes = LINUX644;
            }

            stream.Position = 0;
            var content = new byte[stream.Length];
            int pos = 0;
            while (pos < stream.Length)
                pos += await stream.ReadAsync(content, pos, (int)stream.Length - pos);

            return await Executables.CreateAsync(new Executable
            {
                Description = $"output validator for p{Problem.ProblemId}",
                ZipFile = content,
                Md5sum = content.ToMD5().ToHexDigest(true),
                ZipSize = pos,
                ExecId = execName,
                Type = cmp ? "compare" : "run",
            });
        }

        private static readonly IReadOnlyDictionary<string, string> Checkers =
            new Dictionary<string, string>
            {
                ["504168fb5f80beb55d90d453633b50ff"] = "case_sensitive space_change_sensitive",
                ["c64791ffeb412ceb0602d51e86eb220d"] = "float_tolerance 1e-4",
                ["1ed169c6f859507746ddc061f06ff7b2"] = "float_tolerance 1e-6",
                ["84e6c5cb24799378d1e04e95d6e961b2"] = "float_tolerance 1e-9",
                ["fd50c77a483254949a69bc07e83a056c"] = null,
                ["28cda0257b7aaedd6c348194b75a0447"] = null,
                ["0509bc8c7a3e4a2219d9ca39e5bf3ce8"] = null,
                ["dd628828076360fb6f12582864ed0625"] = null,
                ["fecbdb15ac0b93226fa74dab3169da9d"] = null,
                ["8e03582d85b4f398f6b36f8dd17fc5a5"] = null,
                ["d6deb0e3c0ae8cd4369d9bf54078a548"] = null,
            };

        private async Task<(string cmp, string args)> GetCheckerAsync(XElement chks, ZipArchive zip)
        {
            var fileName = chks.Element("source").Attribute("path").Value;
            var entry = zip.GetEntry(fileName);
            using var stream = entry.Open();
            var content = new byte[entry.Length];
            int pos = 0;
            while (pos < entry.Length)
                pos += await stream.ReadAsync(content, pos, (int)entry.Length - pos);
            var md5 = content.ToMD5().ToHexDigest(true);

            if (Checkers.ContainsKey(md5))
                return ("compare", Checkers[md5]);
            var e = await CreateExecutableAsync(content, Path.GetExtension(fileName), true);
            return (e.ExecId, null);
        }

        private async Task<(string run, bool comb)> GetInteractorAsync(XElement iacs, ZipArchive zip)
        {
            if (iacs == null) return ("run", false);
            var fileName = iacs.Element("source").Attribute("path").Value;
            var entry = zip.GetEntry(fileName);
            using var stream = entry.Open();

            var content = new byte[entry.Length];
            int pos = 0;
            while (pos < entry.Length)
                pos += await stream.ReadAsync(content, pos, (int)entry.Length - pos);
            var e = await CreateExecutableAsync(content, Path.GetExtension(fileName), false);
            return (e.ExecId, true);
        }

        private static readonly IReadOnlyDictionary<string, Verdict> Verds =
            new Dictionary<string, Verdict>
            {
                ["accepted"] = Verdict.Accepted,
                ["main"] = Verdict.Accepted,
                ["time-limit-exceeded-or-accepted"] = Verdict.TimeLimitExceeded,
                ["time-limit-exceeded"] = Verdict.TimeLimitExceeded,
                ["time-limit-exceeded-or-memory-limit-exceeded"] = Verdict.TimeLimitExceeded,
                ["memory-limit-exceeded"] = Verdict.MemoryLimitExceeded,
                ["rejected"] = Verdict.RuntimeError,
                ["failed"] = Verdict.RuntimeError,
                ["wrong-answer"] = Verdict.WrongAnswer,
            };

        private async Task LoadSubmissionsAsync(XElement sols, ZipArchive zip)
        {
            var langs = await Languages.ListAsync();
            foreach (var sol in sols.Elements("solution"))
            {
                var source = sol.Element("source");
                var fileName = source.Attribute("path").Value;
                var file = zip.GetEntry(fileName);
                var tag = sol.Attribute("tag").Value;

                var lang = langs.FirstOrDefault(l =>
                    "." + l.FileExtension == Path.GetExtension(file.FullName));

                if (lang == null)
                {
                    Log($"No language found for jury solution '{file.FullName}'.");
                }
                else
                {
                    var expected = Verds.GetValueOrDefault(tag);

                    using var stream = file.Open();
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();
                    var sub = await Submissions.CreateAsync(
                        code: content,
                        language: lang.Id,
                        problemId: Problem.ProblemId,
                        contestId: null,
                        userId: 0,
                        ipAddr: System.Net.IPAddress.Parse("127.0.0.1"),
                        via: "polygon-upload",
                        username: "SYSTEM",
                        expected: expected);
                    Log($"Jury solution '{file.FullName}' saved s{sub.SubmissionId}.");
                }
            }
        }

        private static readonly IReadOnlyDictionary<string, string> Names =
            new Dictionary<string, string>
            {
                ["description"] = "legend.tex",
                ["inputdesc"] = "input.tex",
                ["outputdesc"] = "output.tex",
                ["hint"] = "notes.tex",
                ["interact"] = "interaction.tex",
            };

        private async Task LoadStatementAsync(XElement name, ZipArchive zip)
        {
            if (name == null) return;
            var lang = name.Attribute("language").Value;
            Problem.Title = name.Attribute("value").Value;

            foreach (var (mdfile, filename) in Names)
            {
                var entry = zip.GetEntry($"statement-sections/{lang}/{filename}");
                if (entry == null) continue;

                string mdcontent;
                using (var st = entry.Open())
                using (var sw = new StreamReader(st))
                    mdcontent = await sw.ReadToEndAsync();

                await Store.WriteFileAsync(Problem, $"{mdfile}.md", mdcontent);
                Log($"Adding statement section 'statement-sections/{lang}/{filename}'.");
            }
        }

        private static string TryGetName(string orig)
        {
            var name = orig.ToUpper().EndsWith(".ZIP")
                ? orig.Substring(0, orig.Length - 4) : orig;
            if (string.IsNullOrEmpty(name))
                name = "UNTITLED";
            return name;
        }

        private async Task LoadTestsetAsync(XElement name, ZipArchive zip)
        {
            if (name == null) return;
            Problem.TimeLimit = int.Parse(name.Element("time-limit").Value);
            Problem.MemoryLimit = int.Parse(name.Element("memory-limit").Value) >> 10;
            var count = int.Parse(name.Element("test-count").Value);
            var testName = name.Attribute("name").Value;
            var tests = name.Element("tests").Elements("test").ToList();
            if (tests.Count != count)
                throw new InvalidDataException("Zip corrupt.");
            int rank = 0;

            for (int i = 1; i <= count; i++)
            {
                var fileName = $"{testName}/{i:D2}";
                var test = tests[i-1];
                var attr1 = test.Attribute("description");
                var attr2 = test.Attribute("sample");

                var inp = zip.GetEntry(fileName);
                var outp = zip.GetEntry(fileName + ".a");

                if (inp == null || outp == null)
                {
                    Log($"Ignoring {fileName}.*");
                    continue;
                }

                var str = $"{i:D2}";
                if (attr1 != null)
                    str += ": " + attr1.Value;
                if (test.Attribute("method").Value == "generated")
                    str += "; " + test.Attribute("cmd").Value;

                var tc = new Testcase
                {
                    ProblemId = Problem.ProblemId,
                    Description = str,
                    IsSecret = attr2?.Value != "true",
                    InputLength = (int)inp.Length,
                    OutputLength = (int)outp.Length,
                    Rank = ++rank,
                };

                using (var ins = inp.Open())
                    tc.Md5sumInput = ins.ToMD5().ToHexDigest(true);
                using (var outs = outp.Open())
                    tc.Md5sumOutput = outs.ToMD5().ToHexDigest(true);

                await Store.CreateAsync(tc);

                using (var ins = inp.Open())
                    await Store.WriteFileAsync(Problem, $"t{tc.TestcaseId}.in", ins);
                using (var outs = outp.Open())
                    await Store.WriteFileAsync(Problem, $"t{tc.TestcaseId}.out", outs);

                Log($"Adding testcase t{tc.TestcaseId} '{fileName}.{{,a}}'.");
            }
        }

        public async Task<Problem> ImportAsync(Stream stream, string uploadFileName, string username)
        {
            using var zipArchive = new ZipArchive(stream);
            var infoXml = zipArchive.GetEntry("problem.xml");
            XElement info;

            Problem = await Store.CreateAsync(new Problem
            {
                AllowJudge = false,
                AllowSubmit = false,
                Title = TryGetName(uploadFileName),
                CompareScript = "compare",
                RunScript = "run",
                MemoryLimit = 524288,
                OutputLimit = 4096,
                Source = username,
                TimeLimit = 10000,
            });

            Log($"Problem p{Problem.ProblemId} created.");

            using (var stInfo = infoXml.Open())
            {
                using var sr = new StreamReader(stInfo);
                var content = await sr.ReadToEndAsync();
                info = XDocument.Parse(content).Root;
            }

            var names = info.Element("names").Elements("name");
            await LoadStatementAsync(names.FirstOrDefault(), zipArchive);

            var tests = info.Element("judging").Elements("testset");
            var testsCount = tests.Count();
            if (testsCount != 1)
                Log($"!!! Testset count is {testsCount}, using the first set...");
            await LoadTestsetAsync(tests.FirstOrDefault(), zipArchive);
            Log("All testcases has been added.");

            var assets = info.Element("assets");
            await LoadSubmissionsAsync(assets.Element("solutions"), zipArchive);
            Log("All jury solutions has been added.");

            (Problem.CompareScript, Problem.ComapreArguments) =
                await GetCheckerAsync(assets.Element("checker"), zipArchive);
            (Problem.RunScript, Problem.CombinedRunCompare) =
                await GetInteractorAsync(assets.Element("interactor"), zipArchive);

            Problem.AllowJudge = true;
            await Store.UpdateAsync(Problem);
            return Problem;
        }
    }
}
