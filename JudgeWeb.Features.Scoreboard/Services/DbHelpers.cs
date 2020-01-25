using EFCore.BulkExtensions;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public static class Extensions
    {
        internal static Task InsertAsync<T>(this DbContext db, T item) where T : class
        {
            return db.BulkInsertAsync(new List<T>
            {
                item
            });
        }

        internal static IQueryable<ScoreCache> Score(this DbContext db, int cid, int probid, int teamid)
        {
            return db.Set<ScoreCache>()
                .Where(sc => sc.ContestId == cid && sc.ProblemId == probid && sc.TeamId == teamid);
        }

        internal static IQueryable<ScoreCache> Score(this DbContext db, ScoreboardEventArgs args)
        {
            return Score(db, args.ContestId, args.ProblemId, args.TeamId);
        }

        internal static IQueryable<RankCache> Rank(this DbContext db, ScoreboardEventArgs args)
        {
            int cid = args.ContestId, teamid = args.TeamId;
            return db.Set<RankCache>()
                .Where(rc => rc.ContestId == cid && rc.TeamId == teamid);
        }

        internal static Task Redistribute(this IRankingStrategy sc, AppDbContext db, ScoreboardEventArgs args)
        {
            switch (args.EventType)
            {
                case 1:
                    return sc.Accept(db, args);
                case 2:
                    return sc.Reject(db, args);
                case 3:
                    return sc.CompileError(db, args);
                case 4:
                    return sc.Pending(db, args);
                case 5:
                    return sc.RefreshCache(db, args);
                default:
                    return Task.CompletedTask;
            }
        }

        public static IServiceCollection AddScoreboard(this IServiceCollection services)
        {
            services.AddHostedService<ScoreboardUpdateService>();
            services.AddSingleton<IScoreboardService, ScoreboardService>();
            return services;
        }
    }
}
