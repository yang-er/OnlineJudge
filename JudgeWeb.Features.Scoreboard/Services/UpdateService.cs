using JudgeWeb.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    internal class ScoreboardUpdateService : BackgroundService
    {
        private readonly IServiceProvider _servicesProvider;
        private readonly ILogger<ScoreboardUpdateService> _logger;
        private static readonly SemaphoreSlim _locker
            = new SemaphoreSlim(0);
        private static readonly Queue<ScoreboardEventArgs> _queue
            = new Queue<ScoreboardEventArgs>();

        public ScoreboardUpdateService(IServiceProvider serviceProvider)
        {
            _servicesProvider = serviceProvider;
            _logger = _servicesProvider
                .GetRequiredService<ILogger<ScoreboardUpdateService>>();
        }

        public static void OnUpdateRequested(ScoreboardEventArgs stt)
        {
            lock (_queue) _queue.Enqueue(stt);
            _locker.Release();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var inner = new Queue<ScoreboardEventArgs>();

            while (true)
            {
                await _locker.WaitAsync(stoppingToken);
                if (stoppingToken.IsCancellationRequested) break;

                inner.Clear();
                inner.Enqueue(_queue.Dequeue());
                while (_locker.CurrentCount > 0)
                {
                    await _locker.WaitAsync();
                    inner.Enqueue(_queue.Dequeue());
                }

                try
                {
                    using (var scope = _servicesProvider.CreateScope())
                    using (var db = scope.ServiceProvider.GetRequiredService<AppDbContext>())
                    {
                        while (inner.Count > 0)
                        {
                            var item = inner.Dequeue();
                            var sc = ScoreboardService.SC[item.RankStrategy];
                            await sc.Redistribute(db, item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unknown error.");
                }
            }
        }
    }
}
