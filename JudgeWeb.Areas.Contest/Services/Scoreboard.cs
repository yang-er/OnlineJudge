using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Scoreboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Services
{
    public partial class ContestManager
    {
        private static IScoreboard[] Scoreboards { get; } = new[]
        {
            new ICPCScoreboard(),
            new ICPCScoreboard(),
            (IScoreboard)new ICPCScoreboard(),
        };

        public void RefreshScoreboardCache(int cid) { }

        private async Task<ScoreboardDataModel> LoadScoreboardAsync(int cid)
        {
            var aff = await ListTeamAffiliationAsync(cid);

            return await Cache.GetOrCreateAsync($"`c{cid}`scoreboard", async entry =>
            {
                var query =
                    from t in DbContext.Teams
                    where t.ContestId == cid && t.Status == 1
                    join a in DbContext.TeamAffiliations on t.AffiliationId equals a.AffiliationId
                    join rc in DbContext.RankCache on new { t.TeamId, t.ContestId } equals new { rc.TeamId, rc.ContestId } into rcs
                    from rc in rcs.DefaultIfEmpty()
                    join sc in DbContext.ScoreCache on new { t.TeamId, t.ContestId } equals new { sc.TeamId, sc.ContestId } into scs
                    select new BoardQuery
                    {
                        Rank = rc ?? new RankCache(),
                        Score = scs.ToList(),
                        Team = t,
                        Affiliation = a
                    };

                var value = await query
                    .ToDictionaryAsync(k => k.Team.TeamId);

                var result = new ScoreboardDataModel
                {
                    Data = value,
                    RefreshTime = DateTimeOffset.Now
                };

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
                return result;
            });
        }

        public async Task<SingleBoardViewModel> FindScoreboardAsync(int cid, int teamid)
        {
            var scb = await LoadScoreboardAsync(cid);
            var bq = scb.Data.GetValueOrDefault(teamid);
            var cts = await GetContestAsync(cid);
            var probs = await GetProblemsAsync(cid);
            var cats = await ListTeamCategoryAsync(cid);

            return new SingleBoardViewModel
            {
                QueryInfo = bq,
                ExecutionStrategy = Scoreboards[cts.RankingStrategy],
                Contest = cts,
                Problems = probs,
                Category = cats.FirstOrDefault(c => c.CategoryId == bq.Team.CategoryId),
            };
        }

        public async Task<FullBoardViewModel> FindScoreboardAsync(int cid, bool isPublic, bool isJury)
        {
            var cts = await GetContestAsync(cid);
            var scb = await LoadScoreboardAsync(cid);
            var probs = await GetProblemsAsync(cid);
            var affs = await ListTeamAffiliationAsync(cid);
            var orgs = await ListTeamCategoryAsync(cid, !isJury);

            return new FullBoardViewModel
            {
                RankCache = scb.Data.Values,
                UpdateTime = scb.RefreshTime,
                Problems = probs,
                IsPublic = isPublic && !isJury,
                Categories = orgs,
                ExecutionStrategy = Scoreboards[cts.RankingStrategy],
                Contest = cts,
                Affiliations = affs,
            };
        }
    }
}
