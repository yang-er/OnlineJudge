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

[assembly: Inject(typeof(KattisImportProvider))]
namespace JudgeWeb.Domains.Problems
{
    public class KattisImportProvider : IImportProvider
    {
        #region Static Visitors

        delegate void Visitor(string token, KattisImportProvider node);

        private static readonly Dictionary<string, Visitor> iniParser;
        private static readonly Dictionary<string, Visitor> yamlParser;

        static KattisImportProvider()
        {
            iniParser = new Dictionary<string, Visitor>();
            yamlParser = new Dictionary<string, Visitor>();

            iniParser.Add("name", (token, node) =>
            {
                if (!string.IsNullOrEmpty(token))
                    node.Problem.Title = token;
            });

            iniParser.Add("timelimit", (token, node) =>
            {
                if (double.TryParse(token.Trim('"', '\''), out var time))
                {
                    int time2 = (int)Math.Round(time * 1000);
                    if (time2 > 15000 || time2 < 500)
                        node.Log($"Error timelimit: '{time2}' out of range.");
                    else
                        node.Problem.TimeLimit = time2;
                }
                else
                {
                    node.Log($"Error timelimit: parsing '{token}'.");
                }
            });

            yamlParser.Add("name", iniParser["name"]);

            yamlParser.Add("source", (token, node) =>
            {
                if (!string.IsNullOrEmpty(token))
                    node.Problem.Source = token;
            });

            yamlParser.Add("validator_flags", (token, node) =>
            {
                if (!string.IsNullOrEmpty(token))
                    node.Problem.ComapreArguments = token;
            });

            yamlParser.Add("validation", (token, node) =>
            {
                if (token == "custom")
                    node.ValidationFlag = 1;
                else if (token == "custom interactive")
                    node.ValidationFlag = 2;
            });

            yamlParser.Add("memory", (token, node) =>
            {
                if (int.TryParse(token, out int mem))
                {
                    if (mem > 1024)
                    {
                        mem = 1024;
                        node.Log("memory limit has been cut to 1GB.");
                    }

                    if (mem < 32)
                    {
                        mem = 32;
                        node.Log("memory limit has been enlarged to 32MB.");
                    }

                    node.Problem.MemoryLimit = mem << 10;
                }
            });

            yamlParser.Add("output", (token, node) =>
            {
                if (int.TryParse(token, out int output_limit))
                {
                    if (output_limit > 40)
                    {
                        output_limit = 40;
                        node.Log("output limit has been cut to 40MB.");
                    }

                    if (output_limit < 4)
                    {
                        output_limit = 4;
                        node.Log("output limit has been enlarged to 4MB.");
                    }

                    node.Problem.OutputLimit = output_limit << 10;
                }
            });
        }

        #endregion

        #region Basic Definitions

        const int LINUX755 = -2115174400;
        const int LINUX644 = -2119958528;

        private ILanguageStore Languages { get; }
        private IExecutableStore Executables { get; }
        private IProblemStore Store { get; }
        private ISubmissionStore Submissions { get; }
        private ILogger<KattisImportProvider> Logger { get; }
        private IMarkdownService Markdown { get; }
        private IStaticFileRepository StaticFiles { get; }

        public StringBuilder LogBuffer { get; }

        public Problem Problem { get; private set; }

        public KattisImportProvider(
            IProblemStore store,
            ILanguageStore languages,
            IExecutableStore executables,
            ISubmissionStore submissions,
            ILogger<KattisImportProvider> logger,
            IMarkdownService markdownService,
            IStaticFileRepository io2)
        {
            Logger = logger;
            Store = store;
            Languages = languages;
            Executables = executables;
            Submissions = submissions;
            LogBuffer = new StringBuilder();
            Markdown = markdownService;
            StaticFiles = io2;
        }

        private void Log(string log)
        {
            Logger.LogInformation(log);
            LogBuffer.AppendLine(log);
        }

        #endregion

        private int ValidationFlag { get; set; }

        private int rank;

        private async Task ReadLinesAsync(ZipArchiveEntry entry,
            Dictionary<string, Visitor> parser, char comment, char equal)
        {
            if (entry == null) return;

            using (var stream = entry.Open())
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    int cmt = line.IndexOf(comment);
                    if (cmt != -1) line = line.Substring(0, cmt);
                    cmt = line.IndexOf(equal);
                    if (cmt == -1) continue;
                    var startToken = line.Substring(0, cmt).Trim();
                    if (parser.TryGetValue(startToken, out var visitor))
                        visitor.Invoke(line.Substring(cmt + 1).Trim(), this);
                }
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

        private async Task<Executable> GetOutputValidatorAsync(ZipArchive zip)
        {
            var list = zip.Entries
                .Where(z => z.FullName.StartsWith("output_validators/") && !z.FullName.EndsWith('/'))
                .ToList();
            if (list.Count == 0)
            {
                Log("No output validator found.");
                return null;
            }

            var fileNames = list.FirstOrDefault().FullName.Split(new[] { '/' }, 3);
            if (fileNames.Length != 3)
            {
                Log($"Wrong file found: '{list[0].FullName}', ignoring output validator.");
                return null;
            }

            var prefix = fileNames[0] + '/' + fileNames[1] + '/';
            if (list.Any(z => !z.FullName.StartsWith(prefix)))
            {
                Log($"More than 1 output validator are found, ignoring.");
                return null;
            }

            var stream = new MemoryStream();
            using (var newzip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (var file in list)
                {
                    using var fs = file.Open();
                    var fileName = file.FullName.Substring(prefix.Length);
                    var f = await newzip.CreateEntryFromStream(fs, fileName);
                    if (fileName == "build" || fileName == "run")
                        f.ExternalAttributes = LINUX755;
                    else
                        f.ExternalAttributes = LINUX644;
                }
            }
            
            stream.Position = 0;
            var content = new byte[stream.Length];
            int pos = 0;
            while (pos < stream.Length)
                pos += await stream.ReadAsync(content, pos, (int)stream.Length - pos);

            return new Executable
            {
                Description = $"output validator for p{Problem.ProblemId}",
                ZipFile = content,
                Md5sum = content.ToMD5().ToHexDigest(true),
                ZipSize = pos,
            };
        }

        private async Task CreateTestcasesAsync(ZipArchive zip, string cat, bool secret)
        {
            var prefix = $"data/{cat}/";
            var fileNames = zip.Entries
                .Where(z => z.FullName.StartsWith(prefix) && !z.FullName.EndsWith('/'))
                .Select(z => Path.GetFileNameWithoutExtension(z.FullName.Substring(prefix.Length)))
                .Distinct()
                .ToList();

            fileNames.Sort((x, y) =>
            {
                // check with prefix numbers
                int lenA = 0, lenB = 0;
                for (; lenA < x.Length; lenA++)
                    if (x[lenA] > '9' || x[lenA] < '0') break;
                for (; lenB < y.Length; lenB++)
                    if (y[lenB] > '9' || y[lenB] < '0') break;
                if (lenA == 0 || lenB == 0)
                    return x.CompareTo(y);
                if (lenA != lenB)
                    return lenA.CompareTo(lenB);
                return x.CompareTo(y);
            });

            foreach (var file in fileNames)
            {
                var inp = zip.GetEntry(prefix + file + ".in");
                var outp = zip.GetEntry(prefix + file + ".ans");
                var desc = zip.GetEntry(prefix + file + ".desc");
                var point = zip.GetEntry(prefix + file + ".point");

                string usedParts = "in,out";
                if (outp == null)
                    outp = zip.GetEntry(prefix + file + ".out");
                else
                    usedParts = "in,ans";

                if (inp == null || outp == null)
                {
                    Log($"Ignoring {prefix}{file}.*");
                    continue;
                }

                var tc = new Testcase
                {
                    ProblemId = Problem.ProblemId,
                    Description = file,
                    IsSecret = secret,
                    InputLength = (int)inp.Length,
                    OutputLength = (int)outp.Length,
                    Rank = ++rank,
                };

                if (desc != null)
                {
                    using (var stream = desc.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        var content = await reader.ReadToEndAsync();
                        tc.Description = string.IsNullOrWhiteSpace(content) ? file : content.Trim();
                    }

                    usedParts += ",desc";
                }

                if (point != null)
                {
                    using (var stream = point.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        var content = await reader.ReadToEndAsync();
                        if (int.TryParse(content.Trim(), out int pnt))
                            tc.Point = pnt;
                    }

                    usedParts += ",point";
                }

                using (var ins = inp.Open())
                    tc.Md5sumInput = ins.ToMD5().ToHexDigest(true);
                using (var outs = outp.Open())
                    tc.Md5sumOutput = outs.ToMD5().ToHexDigest(true);

                await Store.CreateAsync(tc);

                using (var ins = inp.Open())
                    await Store.WriteFileAsync(Problem, $"t{tc.TestcaseId}.in", ins);
                using (var outs = outp.Open())
                    await Store.WriteFileAsync(Problem, $"t{tc.TestcaseId}.out", outs);

                Log($"Adding testcase t{tc.TestcaseId} 'data/{cat}/{file}.{{{usedParts}}}'.");
            }
        }

        private async Task LoadSubmissionsAsync(ZipArchive zip)
        {
            var prefix = "submissions/";
            var files = zip.Entries
                .Where(z => z.FullName.StartsWith(prefix) && !z.FullName.EndsWith('/'))
                .ToList();
            var langs = await Languages.ListAsync();

            foreach (var file in files)
            {
                if (file.Length > 65536)
                {
                    Log($"Too big for jury solution '{file.FullName}'");
                    continue;
                }

                var lang = langs.FirstOrDefault(l =>
                    "." + l.FileExtension == Path.GetExtension(file.FullName));

                if (lang == null)
                {
                    Log($"No language found for jury solution '{file.FullName}'.");
                }
                else
                {
                    var expected = Verdict.Unknown;
                    if (file.FullName.StartsWith("submissions/accepted/"))
                        expected = Verdict.Accepted;
                    if (file.FullName.StartsWith("submissions/wrong_answer/"))
                        expected = Verdict.WrongAnswer;
                    if (file.FullName.StartsWith("submissions/time_limit_exceeded/"))
                        expected = Verdict.TimeLimitExceeded;
                    if (file.FullName.StartsWith("submissions/run_time_error/"))
                        expected = Verdict.RuntimeError;

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

        private async Task LoadStatementsAsync(ZipArchive zip, string mdfile)
        {
            var entry = zip.GetEntry("problem_statement/" + mdfile + ".md");
            if (entry == null) return;

            string mdcontent;
            using (var st = entry.Open())
            using (var sw = new StreamReader(st))
                mdcontent = await sw.ReadToEndAsync();

            var tags = $"p{Problem.ProblemId}";
            var content = await (Markdown, StaticFiles).ImportWithImagesAsync(mdcontent, tags);
            await Store.WriteFileAsync(Problem, $"{mdfile}.md", content);

            Log($"Adding statement section 'problem_statement/{mdfile}.md'.");
        }

        public async Task<Problem> ImportAsync(Stream stream, string uploadFileName, string username)
        {
            using var zipArchive = new ZipArchive(stream);

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
            Directory.CreateDirectory($"Problems/p{Problem.ProblemId}");

            await ReadLinesAsync(
                entry: zipArchive.GetEntry("domjudge-problem.ini"),
                parser: iniParser,
                comment: ';',
                equal: '=');

            await ReadLinesAsync(
                entry: zipArchive.GetEntry("problem.yaml"),
                parser: yamlParser,
                comment: '#',
                equal: ':');

            if (ValidationFlag != 0)
            {
                var exec = await GetOutputValidatorAsync(zipArchive);
                if (exec != null)
                {
                    exec.ExecId = $"p{Problem.ProblemId}{(ValidationFlag == 1 ? "cmp" : "run")}";
                    exec.Type = ValidationFlag == 1 ? "compare" : "run";
                    await Executables.CreateAsync(exec);

                    if (ValidationFlag == 1)
                    {
                        Problem.CompareScript = exec.ExecId;
                    }
                    else
                    {
                        Problem.RunScript = exec.ExecId;
                        Problem.CombinedRunCompare = true;
                    }
                }
            }

            await Store.UpdateAsync(Problem);

            foreach (var mdfile in IProblemStore.MarkdownFiles)
                await LoadStatementsAsync(zipArchive, mdfile);

            await CreateTestcasesAsync(zipArchive, "sample", false);
            await CreateTestcasesAsync(zipArchive, "secret", true);
            Log("All testcases has been added.");

            await LoadSubmissionsAsync(zipArchive);
            Log("All jury solutions has been added.");

            Problem.AllowJudge = true;
            await Store.UpdateAsync(Problem);
            return Problem;
        }
    }
}
