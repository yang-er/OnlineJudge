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

        private static async Task UpdateStatistics(IDbContextHolder store)
        {
            var source = store.Statistics.FromSqlRaw(
                "SELECT COUNT(*) AS [TotalSubmission], [s].[ProblemId], [s].[Author], [s].[ContestId]," +
                      " SUM(CASE WHEN [j].[Status] = 11 THEN 1 ELSE 0 END) AS [AcceptedSubmission]\r\n" +
                "FROM [Submissions] AS [s]\r\n" +
                "INNER JOIN [Judgings] AS [j] ON ([s].[SubmissionId] = [j].[SubmissionId]) AND ([j].[Active] = 1)\r\n" +
                "GROUP BY [s].[ProblemId], [s].[ContestId], [s].[Author]");

            await store.Statistics.MergeAsync(
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

        private static async Task UpdateArchive(IDbContextHolder store)
        {
            var source = store.Statistics
                .Where(ss => ss.ContestId == 0)
                .GroupBy(ss => ss.ProblemId)

                .Select(g => new
                {
                    ProblemId = g.Key,
                    Accepted = g.Sum(ss => ss.AcceptedSubmission),
                    Total = g.Sum(ss => ss.TotalSubmission),
                });

            await store.Archives.MergeAsync(
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
            using var store = scope.ServiceProvider.GetRequiredService<IDbContextHolder>();

            await UpdateStatistics(store);
            await UpdateArchive(store);
        }
    }
}
