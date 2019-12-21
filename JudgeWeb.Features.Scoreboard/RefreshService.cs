using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    [Obsolete("This feature should not be used any more.", true)]
    public sealed class RefreshService : BackgroundService
    {
        private static CancellationTokenSource _global_cts;

        private IServiceProvider ServiceProvider { get; }

        private ILogger<RefreshService> Logger { get; }

        public RefreshService(IServiceProvider services, ILogger<RefreshService> logger)
        {
            ServiceProvider = services;
            Logger = logger;
        }

        public static void Notify() => _global_cts?.Cancel();

        private async Task<bool> RefreshAsync()
        {
            using (var services = ServiceProvider.CreateScope())
            using (var ce = services.ServiceProvider.GetRequiredService<CalculateExecutor>())
            {
                return await ce.GetContestLogs() || await ce.UpdateSJLogs();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!await RefreshAsync())
                    {
                        _global_cts = new CancellationTokenSource();
                        _global_cts.CancelAfter(TimeSpan.FromSeconds(30));
                        var _sleep_cts = CancellationTokenSource
                            .CreateLinkedTokenSource(_global_cts.Token, stoppingToken);
                        await Task.Delay(-1, _sleep_cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.LogInformation("Sleep is interrupted.");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unknown error happend.");
                    return;
                }
            }
        }
    }
}
