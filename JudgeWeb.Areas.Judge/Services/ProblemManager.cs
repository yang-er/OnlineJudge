using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Providers;
using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using JudgeWeb.Features.Problem;
using JudgeWeb.Features.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

[assembly: Inject(typeof(ProblemManager))]
namespace JudgeWeb.Areas.Judge.Services
{
    public class ProblemManager
    {
        protected AppDbContext DbContext { get; }

        protected UserManager UserManager { get; }

        protected IFileRepository IoContext { get; }

        public int ItemsPerPage { get; set; } = 50;

        public ProblemManager(AppDbContext adbc, UserManager um, IFileRepository io)
        {
            DbContext = adbc;
            UserManager = um;
            IoContext = io;
            io.SetContext("Problems");
        }

        public int TotalPages
        {
            get
            {
                if (GlobalCache.Instance.TryGetValue("prob::totcount", out var tot))
                    return (int)tot;

                var pid = DbContext.Problems
                    .OrderByDescending(p => p.ProblemId)
                    .Select(p => new { p.ProblemId })
                    .FirstOrDefault();
                int tot2 = pid?.ProblemId ?? 1000;
                tot2 = (tot2 - 1000) / ItemsPerPage + 1;
                GlobalCache.Instance.Set("prob::totcount", tot2, TimeSpan.FromMinutes(10));
                return tot2;
            }
        }

        public IEnumerable<SubmissionStatistics> Statistics(bool expired = false)
        {
            if (!expired && GlobalCache.Instance.TryGet<List<SubmissionStatistics>>(out var stats))
                return stats;

            stats = DbContext.Query<SubmissionStatistics>()
                .FromSql(SubmissionStatistics.QueryString)
                .AsNoTracking().ToList();

            GlobalCache.Instance.Set(stats, TimeSpan.FromMinutes(30));
            return stats;
        }

        public Dictionary<T, (int, int, int, int)> StatisticsByUser<T>(
            int uid, Func<SubmissionStatistics, T> grouping,
            Func<SubmissionStatistics, bool> filter = null)
        {
            return Statistics().Where(filter ?? (s => true))
                .GroupBy(grouping)
                .ToDictionary(
                    keySelector: g => g.Key,
                    elementSelector: g => (
                        g.Sum(s => s.AcceptedSubmission),
                        g.Sum(s => s.TotalSubmission),
                        g.Sum(s => s.Author == uid ? s.TotalSubmission : 0),
                        g.Sum(s => s.Author == uid ? s.AcceptedSubmission : 0)));
        }

        public async Task<string> ExportXmlAsync(int pid)
        {
            var bs = $"p{pid}";

            if (!IoContext.ExistPart(bs, "export.xml"))
            {
                var prob = await DbContext.Problems
                    .Where(p => p.ProblemId == pid)
                    .FirstOrDefaultAsync();
                if (prob is null) return null;

                var description = await IoContext.ReadPartAsync(bs, "description.md")
                    ?? await IoContext.ReadPartAsync(bs, "compact.html") ?? "";
                var inputdesc = await IoContext.ReadPartAsync(bs, "inputdesc.md") ?? "";
                var outputdesc = await IoContext.ReadPartAsync(bs, "outputdesc.md") ?? "";
                var hint = await IoContext.ReadPartAsync(bs, "hint.md") ?? "";

                var xmlObj = new ProblemSet
                {
                    Description = description,
                    InputHint = inputdesc,
                    OutputHint = outputdesc,
                    HintAndNote = hint,
                    Author = prob.Source ?? "",
                    Title = prob.Title ?? "",
                    ProblemId = prob.ProblemId,
                    ExecuteTimeLimit = prob.TimeLimit,
                    MemoryLimit = prob.MemoryLimit,
                    RunScript = prob.RunScript,
                    CompareScript = prob.CompareScript,
                    JudgeType = prob.ComapreArguments,
                };

                var tcs = await DbContext.Testcases
                    .Where(t => t.ProblemId == pid)
                    .Select(t => new { t.TestcaseId, t.IsSecret, t.Description, t.Point })
                    .ToListAsync();

                foreach (var tc in tcs)
                {
                    var target = tc.IsSecret ? xmlObj.TestCases : xmlObj.Samples;
                    var input = await IoContext.ReadBinaryAsync(bs, $"t{tc.TestcaseId}.in");
                    var output = await IoContext.ReadBinaryAsync(bs, $"t{tc.TestcaseId}.out");

                    target.Add(new TestCase
                    (
                        tc.Description,
                        Encoding.UTF8.GetString(input),
                        Encoding.UTF8.GetString(output),
                        tc.Point
                    ));
                }

                var sw = new StringWriter();
                sw.NewLine = "\n";
                await xmlObj.ToXml().SaveAsync(sw, SaveOptions.None, default);
                await IoContext.WritePartAsync(bs, "export.xml", sw.ToString());
            }

            return $"Problems/p{pid}/export.xml";
        }

        public async Task<Problem> EditAsync(int pid, ProblemEditModel model)
        {
            var prob = await DbContext.Problems
                .Where(p => p.ProblemId == pid)
                .FirstOrDefaultAsync();
            if (prob == null) return null;

            prob.MemoryLimit = model.MemoryLimit;
            prob.TimeLimit = model.TimeLimit;
            prob.RunScript = model.RunScript;
            prob.CompareScript = model.CompareScript;
            prob.ComapreArguments = model.CompareArgument;
            prob.CombinedRunCompare = model.RunAsCompare;
            prob.Flag = model.IsActive ? 0 : 1;
            prob.Source = model.Source ?? "";
            prob.Title = model.Title;
            DbContext.Problems.Update(prob);
            await DbContext.SaveChangesAsync();
            IoContext.RemovePart($"p{pid}", "export.xml");
            return prob;
        }

        public Task<string> GetViewAsync(int pid)
        {
            return IoContext.ReadPartAsync($"p{pid}", "view.html");
        }

        public Task<List<Problem>> ListAsync(int pg)
        {
            return DbContext.Problems
                .Where(p => p.ProblemId <= 1000 + pg * ItemsPerPage
                    && p.ProblemId > 1000 + (pg - 1) * ItemsPerPage)
                .OrderBy(p => p.ProblemId).ToListAsync();
        }

        private async Task<int> NewIdAsync()
        {
            var probcnt = await DbContext.Problems
                .OrderByDescending(p => p.ProblemId)
                .Select(p => new { p.ProblemId })
                .FirstOrDefaultAsync();
            probcnt = probcnt ?? new { ProblemId = 999 };
            return probcnt.ProblemId + 1;
        }

        public async Task<int> CreateAsync()
        {
            var model = new Problem
            {
                ProblemId = await NewIdAsync(),
                Source = "",
                CompareScript = "compare",
                RunScript = "run",
                Flag = 1,
                MemoryLimit = 262144,
                TimeLimit = 5000,
                Title = "未发布问题",
            };

            DbContext.Problems.Add(model);
            await DbContext.SaveChangesAsync();
            return model.ProblemId;
        }

        public async Task<Problem> ImportAsync(Stream stream, ClaimsPrincipal user)
        {
            var st = ProblemSet.FromStream(stream, true, true);
            var probId = await NewIdAsync();

            // add problem entity
            var p = DbContext.Problems.Add(new Problem
            {
                RunScript = st.RunScript,
                CompareScript = st.CompareScript,
                Source = st.Author,
                Flag = 1,
                MemoryLimit = st.MemoryLimit,
                TimeLimit = st.ExecuteTimeLimit,
                ProblemId = probId,
                Title = st.Title
            });

            await DbContext.SaveChangesAsync();

            // Write all markdown files into folders.
            var backstore = $"p{probId}";
            await IoContext.WritePartAsync(backstore, "description.md", st.Description);
            await IoContext.WritePartAsync(backstore, "inputdesc.md", st.InputHint);
            await IoContext.WritePartAsync(backstore, "outputdesc.md", st.OutputHint);
            await IoContext.WritePartAsync(backstore, "hint.md", st.HintAndNote);

            // Add testcases.
            int i = 0;
            var testcases = Enumerable.Concat(
                st.Samples.Select(t => (t, false)),
                st.TestCases.Select(t => (t, true)));

            foreach (var test in testcases)
            {
                i++;
                var input = Encoding.UTF8.GetBytes(test.t.Input);
                var inputHash = input.ToMD5().ToHexDigest(true);
                var output = Encoding.UTF8.GetBytes(test.t.Output);
                var outputHash = output.ToMD5().ToHexDigest(true);

                var tcc = DbContext.Testcases.Add(new Testcase
                {
                    InputLength = input.Length,
                    OutputLength = output.Length,
                    Point = test.t.Point,
                    ProblemId = probId,
                    Rank = i,
                    Description = test.t.Description,
                    IsSecret = test.Item2,
                    Md5sumInput = inputHash,
                    Md5sumOutput = outputHash,
                });

                await DbContext.SaveChangesAsync();

                await IoContext.WriteBinaryAsync($"p{probId}", $"t{tcc.Entity.TestcaseId}.in", input);
                await IoContext.WriteBinaryAsync($"p{probId}", $"t{tcc.Entity.TestcaseId}.out", output);
            }

            DbContext.AuditLogs.Add(new AuditLog
            {
                Comment = "uploaded by xml",
                EntityId = probId,
                Resolved = true,
                ContestId = 0,
                Time = DateTimeOffset.Now,
                Type = AuditLog.TargetType.Problem,
                UserName = UserManager.GetUserName(user),
            });

            await DbContext.SaveChangesAsync();
            return p.Entity;
        }

        public async Task<ProblemEditModel> GetEditModelAsync(int pid)
        {
            var prob = await DbContext.Problems
                .Where(p => p.ProblemId == pid)
                .Select(p =>
                    new ProblemEditModel
                    {
                        ProblemId = p.ProblemId,
                        CompareScript = p.CompareScript,
                        RunScript = p.RunScript,
                        RunAsCompare = p.CombinedRunCompare,
                        CompareArgument = p.ComapreArguments,
                        Source = p.Source,
                        MemoryLimit = p.MemoryLimit,
                        TimeLimit = p.TimeLimit,
                        Title = p.Title,
                        Flag = p.Flag,
                    })
                .FirstOrDefaultAsync();

            if (prob is null) return null;
            prob.IsActive = prob.Flag == 0;
            return prob;
        }

        public async Task<(string Title, int Flag)?> TitleFlagAsync(int pid)
        {
            var prob = await DbContext.Problems
                .Where(p => p.ProblemId == pid)
                .Select(p => new { p.Title, p.Flag })
                .FirstOrDefaultAsync();
            if (prob == null) return null;
            return (prob.Title, prob.Flag);
        }
    }
}
