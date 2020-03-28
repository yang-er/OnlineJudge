using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public static class Extensions
    {
        internal static IQueryable<ScoreCache> Score(
            this ScoreboardContext db,
            int cid, int probid, int teamid)
        {
            return db.ScoreCache.Where(sc
                => sc.ContestId == cid
                && sc.ProblemId == probid
                && sc.TeamId == teamid);
        }

        internal static IQueryable<ScoreCache> Score(
            this ScoreboardContext db,
            ScoreboardEventArgs args)
        {
            return Score(db, args.ContestId, args.ProblemId, args.TeamId);
        }

        internal static IQueryable<RankCache> Rank(
            this ScoreboardContext db,
            ScoreboardEventArgs args)
        {
            int cid = args.ContestId, teamid = args.TeamId;
            return db.RankCache
                .Where(rc => rc.ContestId == cid && rc.TeamId == teamid);
        }

        internal static Task Redistribute(
            this IRankingStrategy sc,
            ScoreboardContext db,
            ScoreboardEventArgs args)
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
            services.AddScoped<ScoreboardContext>();
            return services;
        }
    }
}
