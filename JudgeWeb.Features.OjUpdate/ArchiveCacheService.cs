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
            bool firstRun = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!firstRun) await UpdateAsync(stoppingToken);
                    firstRun = false;
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

        private static async Task UpdateStatistics(DbContext store)
        {
            var source =
                from s in store.Set<Submission>()
                join j in store.Set<Judging>() on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                group j.Status by new { s.ProblemId, s.Author, s.ContestId } into g
                select new SubmissionStatistics
                {
                    ProblemId = g.Key.ProblemId,
                    Author = g.Key.Author,
                    ContestId = g.Key.ContestId,
                    TotalSubmission = g.Count(),
                    AcceptedSubmission = g.Sum(v => v == Verdict.Accepted ? 1 : 0)
                };

            await store.Set<SubmissionStatistics>().MergeAsync(
                sourceTable: source,
                targetKey: ss => new { ss.Author, ss.ContestId, ss.ProblemId },
                sourceKey: ss => new { ss.Author, ss.ContestId, ss.ProblemId },
                delete: true,

                updateExpression: (_, ss) => new SubmissionStatistics
                {
                    AcceptedSubmission = ss.AcceptedSubmission,
                    TotalSubmission = ss.TotalSubmission
                },

                insertExpression: ss => new SubmissionStatistics
                {
                    Author = ss.Author,
                    ContestId = ss.ContestId,
                    ProblemId = ss.ProblemId,
                    AcceptedSubmission = ss.AcceptedSubmission,
                    TotalSubmission = ss.TotalSubmission
                });
        }

        private static async Task UpdateArchive(DbContext store)
        {
            var source = store.Set<SubmissionStatistics>()
                .Where(ss => ss.ContestId == 0)
                .GroupBy(ss => ss.ProblemId)

                .Select(g => new
                {
                    ProblemId = g.Key,
                    Accepted = g.Sum(ss => ss.AcceptedSubmission),
                    Total = g.Sum(ss => ss.TotalSubmission),
                });

            await store.Set<ProblemArchive>().MergeAsync(
                sourceTable: source,
                targetKey: a => a.ProblemId,
                sourceKey: a => a.ProblemId,
                insertExpression: null, delete: false,

                updateExpression: (a, s) => new ProblemArchive
                {
                    Accepted = s.Accepted,
                    Total = s.Total,
                });
        }

        private async Task UpdateAsync(CancellationToken stoppingToken)
        {
            using var scope = ServiceProvider.CreateScope();
            using var store = scope.ServiceProvider.GetRequiredService<DbContextAccessor>();

            await UpdateStatistics(store);
            await UpdateArchive(store);
        }
    }
}
