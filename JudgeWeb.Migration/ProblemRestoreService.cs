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
    public class ProblemRestoreService : BackgroundService
    {
        public OldDbContext DbContext { get; }

        public AppDbContext AppDbContext { get; }

        public ILogger<ProblemRestoreService> Logger { get; }

        public ProblemRestoreService(AppDbContext app, OldDbContext odbc, ILogger<ProblemRestoreService> lg)
        {
            AppDbContext = app;
            DbContext = odbc;
            Logger = lg;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(2000);

            var probs = await DbContext.Problem
                .OrderBy(p => p.Pid)
                .ToListAsync();

            foreach (var item in probs)
            {
                item.Intro = Encoding.GetEncoding(936).GetString(Encoding.GetEncoding("ISO8859-1").GetBytes(item.Intro));
                item.Name = Encoding.GetEncoding(936).GetString(Encoding.GetEncoding("ISO8859-1").GetBytes(item.Name));
                item.Source = Encoding.GetEncoding(936).GetString(Encoding.GetEncoding("ISO8859-1").GetBytes(item.Source));
                if (item.Source == "none") item.Source = "JOJ";
                await System.IO.File.WriteAllTextAsync(item.Pid + ".html", item.Intro);
                item.Memorylimit = Math.Max(item.Memorylimit, 131072);

                AppDbContext.Problems.Add(new Problem
                {
                    ProblemId = item.Pid,
                    MemoryLimit = item.Memorylimit,
                    TimeLimit = item.Timelimit * 1000,
                    RunScript = "run",
                    ComapreArguments = null,
                    CompareScript = "compare",
                    CombinedRunCompare = false,
                    Title = item.Name,
                    Source = item.Source,
                    Flag = item.SpecJudge == "Y" ? 2 : item.Visible == "show" ? 0 : 1
                });

                await AppDbContext.SaveChangesAsync();
            }
        }
    }
}
