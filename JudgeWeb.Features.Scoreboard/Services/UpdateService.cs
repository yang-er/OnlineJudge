using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public class ScoreboardUpdateService : BackgroundService
    {
        private readonly IServiceProvider _servicesProvider;
        private readonly ILogger<ScoreboardUpdateService> _logger;
        private readonly SemaphoreSlim _locker;
        private readonly Queue<ScoreboardState> _queue;

        public ScoreboardUpdateService(IServiceProvider serviceProvider)
        {
            _servicesProvider = serviceProvider;
            _locker = new SemaphoreSlim(0);
            _queue = new Queue<ScoreboardState>();
            _logger = _servicesProvider
                .GetRequiredService<ILogger<ScoreboardUpdateService>>();
        }

        private void OnUpdateRequested(ScoreboardState stt)
        {
            lock (this) _queue.Enqueue(stt);
            _locker.Release();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            AppDbContext.ScoreboardUpdate += OnUpdateRequested;
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            AppDbContext.ScoreboardUpdate -= OnUpdateRequested;
            return base.StopAsync(cancellationToken);
        }

        private Task OnSubmissionCreated(AppDbContext db, ScoreboardState c)
        {
            var state = c.GetState();
            if (state >= ContestState.Ended) return Task.CompletedTask;

            var board = Instance.Scoreboards[c.RankStrategy];
            return board.UpdateScoreboardPendingAsync(db,
                c.ContestId, c.TeamId, c.ProblemId,
                freeze: state == ContestState.Frozen);
        }

        private Task OnJudgingCreated(AppDbContext db, ScoreboardState c, bool opfb = true)
        {
            var state = c.GetState();
            if (state >= ContestState.Ended) return Task.CompletedTask;

            var board = Instance.Scoreboards[c.RankStrategy];

            if (c.Verdict.Value == Verdict.Accepted)
            {
                return board.UpdateScoreboardCorrectAsync(db,
                    c.SubmissionId, c.ContestId, c.TeamId, c.ProblemId,
                    time: (c.Time - c.StartTime)?.TotalSeconds ?? 0,
                    freeze: state == ContestState.Frozen, c.UseBalloon, opfb);
            }
            else if (c.Verdict.Value == Verdict.CompileError)
            {
                return board.UpdateScoreboardCompileErrorAsync(db,
                    c.ContestId, c.TeamId, c.ProblemId,
                    freeze: state == ContestState.Frozen);
            }
            else
            {
                return board.UpdateScoreboardRejectedAsync(db,
                    c.ContestId, c.TeamId, c.ProblemId,
                    freeze: state == ContestState.Frozen);
            }
        }

        private async Task OnRefreshScoreboardCache(AppDbContext db, int cid, DateTimeOffset time)
        {
            var c = await db.Contests
                .Where(cc => cc.ContestId == cid)
                .SingleOrDefaultAsync();

            var subsQuery =
                from s in db.Submissions
                where s.ContestId == cid && s.Time < time
                join j in db.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                join t in db.Teams on new { s.ContestId, TeamId = s.Author } equals new { t.ContestId, t.TeamId }
                join cc in db.TeamCategories on t.CategoryId equals cc.CategoryId
                orderby s.Time ascending
                select new { s.ProblemId, cc.SortOrder, s.SubmissionId, s.Author, s.Time, v = (Verdict?)j.Status };
            
            var info = await subsQuery.ToListAsync();
            await Instance.Scoreboards[c.RankingStrategy]
                .UpdateScoreboardBundleAsync(db, c,
                    info.Select(a => (a.SubmissionId, a.Author, a.SortOrder, a.ProblemId, a.Time, a.v)));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var inner = new Queue<ScoreboardState>();

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
                            if (item.SubmissionId == -1)
                                await OnRefreshScoreboardCache(db, item.ContestId, item.Time);
                            else if (item.Verdict.HasValue)
                                await OnJudgingCreated(db, item);
                            else
                                await OnSubmissionCreated(db, item);
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
