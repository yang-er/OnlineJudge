using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Inject(typeof(ProblemImportService))]
namespace JudgeWeb.Areas.Polygon.Services
{
    public class ProblemImportService
    {
        #region Static Visitors

        delegate void Visitor(string token, ProblemImportService node);

        private static readonly Dictionary<string, Visitor> iniParser;
        private static readonly Dictionary<string, Visitor> yamlParser;

        private static readonly string[] statements
            = new[] { "description.md", "hint.md", "inputdesc.md", "outputdesc.md" };

        static ProblemImportService()
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
                if (double.TryParse(token, out var time))
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
                if (int.TryParse(token, out int mem))
                {
                    if (mem > 40)
                    {
                        mem = 40;
                        node.Log("output limit has been cut to 1GB.");
                    }

                    if (mem < 4)
                    {
                        mem = 4;
                        node.Log("output limit has been enlarged to 32MB.");
                    }

                    node.Problem.OutputLimit = mem << 10;
                }
            });
        }

        #endregion

        #region Basic Definitions

        private readonly AppDbContext dbContext;
        private readonly SubmissionManager submissionManager;
        private readonly ILogger<ProblemImportService> logger;

        public StringBuilder LogBuffer { get; }

        public Problem Problem { get; private set; }

        public ProblemImportService(
            AppDbContext db, SubmissionManager sub,
            ILogger<ProblemImportService> logger)
        {
            dbContext = db;
            submissionManager = sub;
            this.logger = logger;
            LogBuffer = new StringBuilder();
        }

        private void Log(string log)
        {
            logger.LogInformation(log);
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
                foreach (var file in list)
                    using (var fs = file.Open())
                        await newzip.CreateEntryFromStream(fs, file.FullName.Substring(prefix.Length));
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
            fileNames.Sort();

            foreach (var file in fileNames)
            {
                var inp = zip.GetEntry(prefix + file + ".in");
                var outp = zip.GetEntry(prefix + file + ".ans");
                var desc = zip.GetEntry(prefix + file + ".desc");
                var point = zip.GetEntry(prefix + file + ".point");

                if (inp == null || outp == null)
                {
                    Log($"Ignoring {prefix}{file}.*");
                    continue;
                }

                var guid = Guid.NewGuid();
                inp.ExtractToFile($"{guid}.in", true);
                outp.ExtractToFile($"{guid}.ans", true);

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
                }

                var inp2 = await File.ReadAllBytesAsync($"{guid}.in");
                tc.Md5sumInput = inp2.ToMD5().ToHexDigest(true);
                inp2 = await File.ReadAllBytesAsync($"{guid}.ans");
                tc.Md5sumOutput = inp2.ToMD5().ToHexDigest(true);
                var t = dbContext.Testcases.Add(tc);
                await dbContext.SaveChangesAsync();
                File.Move($"{guid}.in", $"Problems/p{Problem.ProblemId}/t{t.Entity.TestcaseId}.in");
                File.Move($"{guid}.ans", $"Problems/p{Problem.ProblemId}/t{t.Entity.TestcaseId}.out");
                Log($"Adding testcase t{t.Entity.TestcaseId} 'data/{cat}/{file}.{{in,ans}}.");
            }
        }

        private async Task LoadSubmissionsAsync(ZipArchive zip)
        {
            var prefix = "submissions/";
            var files = zip.Entries
                .Where(z => z.FullName.StartsWith(prefix) && !z.FullName.EndsWith('/'))
                .ToList();
            var langs = await dbContext.Languages.ToListAsync();

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

                    using (var stream = file.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        var content = await reader.ReadToEndAsync();
                        var sub = await submissionManager.CreateAsync(
                            code: content,
                            langid: lang.LangId,
                            probid: Problem.ProblemId,
                            cid: 0,
                            uid: 0,
                            ipAddr: System.Net.IPAddress.Parse("127.0.0.1"),
                            via: "polygon-upload",
                            username: "SYSTEM",
                            expected: expected);
                        Log($"Jury solution '{file.FullName}' saved s{sub.SubmissionId}.");
                    }
                }
            }
        }

        public async Task<Problem> ImportAsync(IFormFile zipFile, string username)
        {
            if (zipFile == null) throw new ArgumentNullException(nameof(zipFile));
            using (var stream = zipFile.OpenReadStream())
            using (var zipArchive = new ZipArchive(stream))
            {
                var p = dbContext.Problems.Add(new Problem
                {
                    AllowJudge = true,
                    AllowSubmit = false,
                    Title = TryGetName(zipFile.FileName),
                    CompareScript = "compare",
                    RunScript = "run",
                    MemoryLimit = 524288,
                    OutputLimit = 4096,
                    Source = username,
                    TimeLimit = 10000,
                });

                await dbContext.SaveChangesAsync();
                Problem = p.Entity;
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
                        dbContext.Executable.Add(exec);
                        await dbContext.SaveChangesAsync();

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

                dbContext.Problems.Update(Problem);
                await dbContext.SaveChangesAsync();

                foreach (var mdfile in statements)
                    zipArchive.GetEntry("problem_statement/" + mdfile)
                        ?.ExtractToFile($"Problems/p{Problem.ProblemId}/{mdfile}", true);

                await CreateTestcasesAsync(zipArchive, "sample", false);
                await CreateTestcasesAsync(zipArchive, "secret", true);
                Log("All testcases has been added.");

                await LoadSubmissionsAsync(zipArchive);
                Log("All jury solutions has been added.");
                return Problem;
            }
        }
    }
}
