using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Migration
{
    public class SubmissionRestoreService : BackgroundService
    {
        public OldDbContext DbContext { get; }

        public AppDbContext AppDbContext { get; }

        public ILogger<SubmissionRestoreService> Logger { get; }

        public SubmissionRestoreService(AppDbContext app, OldDbContext odbc, ILogger<SubmissionRestoreService> lg)
        {
            AppDbContext = app;
            AppDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            DbContext = odbc;
            Logger = lg;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(2000);

            var subsQuery =
                from s in DbContext.SubmitHide
                orderby s.Sid
                join c1 in DbContext.RunSource on s.Sid equals c1.Sid into s1
                from c1 in s1.DefaultIfEmpty()
                join c2 in DbContext.SourceLarge on s.Sid equals c2.Sid into s2
                from c2 in s2.DefaultIfEmpty()
                select new { s, c1, c2 };

            var subs = await subsQuery
                .AsNoTracking()
                .ToListAsync();

            var lang = new Dictionary<string, int>
            {
                ["C++"] = 2,
                ["PAS"] = 6,
                ["Java"] = 3,
                ["AnsiC"] = 1,
            };

            var verd = new Dictionary<int, Verdict>
            {
                [1] = Verdict.Accepted,
                [2] = Verdict.WrongAnswer,
                [3] = Verdict.PresentationError,
                [4] = Verdict.RuntimeError,
                [5] = Verdict.TimeLimitExceeded,
                [6] = Verdict.CompileError,
                [8] = Verdict.OutputLimitExceeded,
                [9] = Verdict.MemoryLimitExceeded,
                [100] = Verdict.UndefinedError,
            };

            var dt = DateTimeOffset.Now;
            int qwq = 0;

            foreach (var sss in subs)
            {
                var item = sss.s;
                var src = sss.c1?.Source ?? sss.c2?.Source ?? "";
                var source = Encoding.GetEncoding(936)
                    .GetString(Encoding.GetEncoding("ISO8859-1").GetBytes(src));

                var oldlen = source.Length;

                if (source.Length > 5000000)
                    source = source.Substring(0, 4999900) + "\n\n\n[ Too long, truncated. ]";

                AppDbContext.Submissions.Add(new Submission
                {
                    SourceCode = source.ToBase64(),
                    ContestId = item.Cid,
                    Time = new DateTimeOffset(item.Sdate, TimeSpan.FromHours(8)),
                    SubmissionId = item.Sid,
                    CodeLength = oldlen,
                    Ip = "-",
                    Language = lang[item.Lang],
                    ProblemId = item.Pid,
                    Author = item.Uid,
                });

                AppDbContext.Judgings.Add(new Judging
                {
                    ServerId = item.JudgeServer,
                    RejudgeId = 0,
                    Active = true,
                    StartTime = new DateTimeOffset(item.Sdate, TimeSpan.FromHours(8)),
                    StopTime = new DateTimeOffset(item.Sdate, TimeSpan.FromHours(8)),
                    CompileError = "",
                    Status = verd[item.Status],
                    SubmissionId = item.Sid,
                    JudgingId = item.Sid,
                    FullTest = false,
                    ExecuteMemory = item.Spendmem1 + item.Spendmem2,
                    ExecuteTime = (int)(item.Spendtime * 1000 + 0.5)
                });

                qwq++;

                if (qwq >= 200)
                {
                    await AppDbContext.SaveChangesAsync();
                    qwq = 0;
                }
            }

            await AppDbContext.SaveChangesAsync();
            var used = DateTimeOffset.Now - dt;
            Logger.LogWarning("Elapsed {0}ms", used.TotalMilliseconds);
        }
    }
}
