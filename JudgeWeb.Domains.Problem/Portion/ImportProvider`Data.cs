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

[assembly: Inject(typeof(DataImportProvider))]
namespace JudgeWeb.Domains.Problems
{
    public class DataImportProvider : IImportProvider
    {
        const int LINUX644 = -2119958528;

        private ILanguageStore Languages { get; }
        private IExecutableStore Executables { get; }
        private IProblemStore Store { get; }
        private ISubmissionStore Submissions { get; }
        private ILogger<DataImportProvider> Logger { get; }
        private IStaticFileRepository StaticFiles { get; }

        public StringBuilder LogBuffer { get; }

        public Problem Problem { get; private set; }

        public DataImportProvider(
            IProblemStore store,
            ILanguageStore languages,
            IExecutableStore executables,
            ISubmissionStore submissions,
            ILogger<DataImportProvider> logger,
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

        private static string TryGetName(string orig)
        {
            var name = orig.ToUpper().EndsWith(".ZIP") ? orig[0..^4] : orig;
            if (string.IsNullOrEmpty(name))
                name = "UNTITLED";
            return name;
        }

        private async Task CreateTestcasesAsync(ZipArchive zip)
        {
            int rank = 0;
            var prefix = string.Empty;
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
                    IsSecret = true,
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

                Log($"Adding testcase t{tc.TestcaseId} '{file}.{{{usedParts}}}'.");
            }
        }

        public async Task<List<Problem>> ImportAsync(Stream stream, string uploadFileName, string username)
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
            await CreateTestcasesAsync(zipArchive);

            Problem.AllowJudge = true;
            await Store.UpdateAsync(Problem);
            return new List<Problem> { Problem };
        }
    }
}
