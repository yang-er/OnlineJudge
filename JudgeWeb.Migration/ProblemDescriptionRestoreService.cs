using JudgeWeb.Areas.Judge.Providers;
using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Migration
{
    public class ProblemDescriptionRestoreService : BackgroundService
    {
        public ILogger<ProblemDescriptionRestoreService> Logger { get; }

        public IServiceProvider ServiceProvider { get; }

        public ProblemDescriptionRestoreService(ILogger<ProblemDescriptionRestoreService> lg, IServiceProvider services)
        {
            Logger = lg;
            ServiceProvider = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(2000);

            /*
            var e = Directory.EnumerateFiles("./htmld/", "*.html");
            foreach (var item in e)
            {
                int fid = int.Parse(item.Replace("./htmld/", "").Replace(".html", ""));
                Directory.CreateDirectory(@"C:\Users\acm\source\repos\yang-er\OnlineJudge\JudgeWeb\Problems\p" + fid);
                File.Move(item, @"C:\Users\acm\source\repos\yang-er\OnlineJudge\JudgeWeb\Problems\p" + fid + "\\compact.html");
                //File.Move(item, );
                Logger.LogInformation(item);
            }
            */

            /*for (int i = 1; i < 100; i++)
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var tm = scope.ServiceProvider.GetRequiredService<ProblemManager>();
                    var l = await tm.ListAsync(i, true);
                    var vp = scope.ServiceProvider.GetRequiredService<IProblemViewProvider>();
                    foreach (var p in l) await tm.GenerateViewAsync(p, vp);
                }
            }*/

            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var tm = scope.ServiceProvider.GetRequiredService<TestcaseManager>();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.ChangeTracker.AutoDetectChangesEnabled = false;

                    for (int i = 1095; i <= 1095; i++)
                    {
                        if (await db.Problems
                            .Where(p => p.ProblemId == i)
                            .Select(p => p.Title)
                            .FirstOrDefaultAsync() == null)
                            continue;
                        var inp = await File.ReadAllBytesAsync(@"\\192.168.250.178\data\" + (i / 100) + @"\" + i + @"\input.txt");
                        var oup = await File.ReadAllBytesAsync(@"\\192.168.250.178\data\" + (i / 100) + @"\" + i + @"\output.txt");
                        var inp5 = inp.ToMD5().ToHexDigest(true);
                        var oup5 = oup.ToMD5().ToHexDigest(true);
                        await tm.CreateAsync(i, (inp, inp5), (oup, oup5), true, "secret0", null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "");
            }

            /*try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var tm = scope.ServiceProvider.GetRequiredService<TestcaseManager>();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.ChangeTracker.AutoDetectChangesEnabled = false;

                    for (int i = 1; i <= 8; i++)
                    {
                        var inp = await File.ReadAllBytesAsync(@"\\192.168.250.178\data\12\1255\song" + i + ".in");
                        var oup = await File.ReadAllBytesAsync(@"\\192.168.250.178\data\12\1255\song" + i + ".diff");
                        var inp5 = inp.ToMD5().ToHexDigest(true);
                        var oup5 = oup.ToMD5().ToHexDigest(true);
                        await tm.CreateAsync(1255, (inp, inp5), (oup, oup5), true, "secret" + (i - 1), null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "");
            }*/

            Logger.LogError("o");
        }
    }
}
