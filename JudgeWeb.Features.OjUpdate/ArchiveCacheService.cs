using EFCore.BulkExtensions;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.OjUpdate
{
    public class ArchiveCacheService : BackgroundService
    {
        public IServiceProvider ServiceProvider { get; }
        public ILogger<ArchiveCacheService> Logger { get; }

        public ArchiveCacheService(
            ILogger<ArchiveCacheService> logger,
            IServiceProvider serviceProvider)
        {
            Logger = logger;
            ServiceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogDebug("Fetch service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateAsync(stoppingToken);
                    await Task.Delay(1000 * 60 * 60 * 6, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    Logger.LogWarning("Fetch timer was interrupted.");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unknown error happended.");
                }
            }

            Logger.LogDebug("Fetch service stopped.");
        }

        private async Task UpdateAsync(CancellationToken stoppingToken)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();
                var arch = await dbContext.Archives.ToListAsync();

                foreach (var item in arch)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var statQuery =
                        from s in dbContext.Submissions
                        where s.ContestId == 0 && s.ProblemId == item.ProblemId
                        join j in dbContext.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                        group 1 by j.Status into g
                        select new { g.Key, Count = g.Count() };

                    var result = await statQuery.ToListAsync();
                    result.Add(new { Key = Verdict.Accepted, Count = 0 });
                    var tot = result.Sum(a => a.Count);
                    var ac = result.Where(a => a.Key == Verdict.Accepted).Sum(a => a.Count);
                    int pid = item.PublicId;

                    await dbContext.Archives
                        .Where(a => a.PublicId == pid)
                        .BatchUpdateAsync(
                            updateExpression: a => new ProblemArchive
                            {
                                Total = tot,
                                Accepted = ac
                            });
                }
            }
        }
    }
}
