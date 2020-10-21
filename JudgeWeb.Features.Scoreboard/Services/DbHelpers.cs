using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace JudgeWeb.Features.Scoreboard
{
    internal static class Extensions
    {
        public static IQueryable<ScoreCache> Score(
            this DbContext db,
            int cid, int probid, int teamid)
        {
            return db.Set<ScoreCache>().Where(sc
                => sc.ContestId == cid
                && sc.ProblemId == probid
                && sc.TeamId == teamid);
        }
    }
}
