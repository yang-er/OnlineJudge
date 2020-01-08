using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public interface IScoreboard
    {
        IEnumerable<BoardQuery> SortByRule(IEnumerable<BoardQuery> src, bool isPublic);

        Task UpdateScoreboardPendingAsync(DbContext db, int cid, int teamid, int probid, bool freeze);

        Task UpdateScoreboardCorrectAsync(DbContext db, int cid, int teamid, int probid, double time, bool freeze, bool opfb = true);

        Task UpdateScoreboardCompileErrorAsync(DbContext db, int cid, int teamid, int probid, bool freeze);

        Task UpdateScoreboardRejectedAsync(DbContext db, int cid, int teamid, int probid, bool freeze);

        Task UpdateScoreboardBundleAsync(DbContext db, Contest c, IEnumerable<(int team, int so, int prob, DateTimeOffset time, Verdict? v)> results);
    }
}
