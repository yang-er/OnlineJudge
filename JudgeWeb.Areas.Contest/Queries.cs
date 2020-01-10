using EFCore.BulkExtensions;
using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb
{
    public static class ContestQueries
    {
        public static Task<Contest> GetContestAsync(this AppDbContext db, int cid)
        {
            return db.Contests
                .Where(c => c.ContestId == cid)
                .CachedSingleOrDefaultAsync($"`c{cid}`info", TimeSpan.FromMinutes(5));
        }

        public static Task<ContestProblem[]> GetProblemsAsync(this AppDbContext db, int cid)
        {
            return ContestCache._cache.GetOrCreateAsync($"`c{cid}`probs", async entry =>
            {
                var query1 =
                    from cp in db.ContestProblem
                    where cp.ContestId == cid
                    orderby cp.Rank ascending
                    join p in db.Problems on cp.ProblemId equals p.ProblemId
                    select new ContestProblem(cp, p.Title, p.TimeLimit, p.MemoryLimit, p.CombinedRunCompare);

                var query2 =
                    from cp in db.ContestProblem
                    where cp.ContestId == cid
                    join t in db.Testcases on cp.ProblemId equals t.ProblemId
                    group t by cp.ProblemId into g
                    select new { g.Key, Count = g.Count() };

                var result = await query1.ToArrayAsync();
                var result2 = await query2.ToDictionaryAsync(k => k.Key, v => v.Count);
                foreach (var item in result)
                    item.TestcaseCount = result2.GetValueOrDefault(item.ProblemId);

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return result;
            });
        }

        public static Task<Dictionary<int, Language>> GetLanguagesAsync(this AppDbContext db, int cid)
        {
            return db.Languages.CachedToDictionaryAsync(
                keySelector: k => k.LangId,
                tag: $"`c{cid}`langs",
                timeSpan: TimeSpan.FromMinutes(10));
        }

        public static Task<Dictionary<int, string>> GetTeamNameAsync(this AppDbContext db, int cid)
        {
            return db.Teams
                .Where(t => t.ContestId == cid && t.Status == 1)
                .Select(t => new { t.TeamId, t.TeamName })
                .CachedToDictionaryAsync(
                    keySelector: t => t.TeamId,
                    valueSelector: t => t.TeamName,
                    tag: $"`c{cid}`teams`names_dict",
                    timeSpan: TimeSpan.FromMinutes(5));
        }

        public static async Task UpdateContestAsync(this AppDbContext db, int cid, Contest template, params string[] contents)
        {
            int solved = await db.Contests
                .Where(c => c.ContestId == cid)
                .BatchUpdateAsync(template, contents.ToList());

            ContestCache._cache.Remove($"`c{cid}`info");
            ContestCache._cache.Remove($"`c{cid}`internal_state");
        }

        public static Task<List<TeamCategory>> ListTeamCategoryAsync(this AppDbContext db, int cid, bool? requirePublic = null)
        {
            if (requirePublic.HasValue)
            {
                var query = db.Teams
                    .Where(t => t.ContestId == cid && t.Status == 1)
                    .Select(t => new { t.CategoryId })
                    .Distinct()
                    .Join(
                        inner: db.TeamCategories,
                        outerKeySelector: i => i.CategoryId,
                        innerKeySelector: c => c.CategoryId,
                        resultSelector: (i, c) => c);

                if (requirePublic.Value)
                    query = query
                        .Where(tc => tc.IsPublic);

                return query.CachedToListAsync(
                    tag: $"`c{cid}`teams`cat`{(requirePublic.Value ? 2 : 1)}",
                    timeSpan: TimeSpan.FromMinutes(5));
            }
            else
            {
                return db.TeamCategories.CachedToListAsync(
                    tag: $"`c{cid}`teams`cat`0",
                    timeSpan: TimeSpan.FromMinutes(5));
            }
        }

        public static Task<List<TeamAffiliation>> ListTeamAffiliationAsync(this AppDbContext db, int cid, bool filtered = true)
        {
            return filtered
                ? db.Teams
                    .Where(t => t.ContestId == cid && t.Status == 1)
                    .Select(t => new { t.AffiliationId })
                    .Distinct()
                    .Join(
                        inner: db.TeamAffiliations,
                        outerKeySelector: a => a.AffiliationId,
                        innerKeySelector: a => a.AffiliationId,
                        resultSelector: (i, a) => a)
                    .CachedToListAsync($"`c{cid}`teams`aff0", TimeSpan.FromMinutes(5))
                : db.TeamAffiliations
                    .CachedToListAsync($"`c{cid}`teams`aff1", TimeSpan.FromMinutes(5));
        }

        public static async Task<ScoreboardDataModel> LoadScoreboardAsync(this AppDbContext db, int cid)
        {
            var aff = await db.ListTeamAffiliationAsync(cid);

            return await ContestCache._cache.GetOrCreateAsync($"`c{cid}`scoreboard", async entry =>
            {
                var query =
                    from t in db.Teams
                    where t.ContestId == cid && t.Status == 1
                    join a in db.TeamAffiliations on t.AffiliationId equals a.AffiliationId
                    join rc in db.RankCache on new { t.TeamId, t.ContestId } equals new { rc.TeamId, rc.ContestId } into rcs
                    from rc in rcs.DefaultIfEmpty()
                    join sc in db.ScoreCache on new { t.TeamId, t.ContestId } equals new { sc.TeamId, sc.ContestId } into scs
                    select new Features.Scoreboard.BoardQuery
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

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3);
                return result;
            });
        }

    }
}
