using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
            return db.CachedGetAsync($"`c{cid}`probs", TimeSpan.FromMinutes(5), async () =>
            {
                var query1 =
                    from cp in db.ContestProblem
                    where cp.ContestId == cid
                    join p in db.Problems on cp.ProblemId equals p.ProblemId
                    select new ContestProblem(cp, p.Title, p.TimeLimit, p.MemoryLimit, p.CombinedRunCompare, p.Shared);

                var query2 =
                    from cp in db.ContestProblem
                    where cp.ContestId == cid
                    join t in db.Testcases on cp.ProblemId equals t.ProblemId
                    group t by cp.ProblemId into g
                    select new { g.Key, Count = g.Count(), Score = g.Sum(t => t.Point) };

                var result = await query1.ToArrayAsync();
                Array.Sort(result, (a, b) => a.ShortName.CompareTo(b.ShortName));
                for (int i = 0; i < result.Length; i++)
                    result[i].Rank = i + 1;
                var result2 = await query2.ToDictionaryAsync(k => k.Key);

                foreach (var item in result)
                {
                    var res = result2.GetValueOrDefault(item.ProblemId) ?? new { Key = 0, Count = 0, Score = 0 };
                    item.TestcaseCount = res.Count;
                    if (item.Score == 0) item.Score = res.Score;
                }

                return result;
            });
        }

        public static Task<Dictionary<string, Language>> GetLanguagesAsync(this AppDbContext db, int cid)
        {
            return db.Languages.CachedToDictionaryAsync(
                keySelector: k => k.Id,
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

        public static async Task UpdateContestAsync(this AppDbContext db, int cid, Expression<Func<Contest, Contest>> expression)
        {
            int solved = await db.Contests
                .Where(c => c.ContestId == cid)
                .BatchUpdateAsync(expression);

            db.RemoveCacheEntry($"`c{cid}`info");
            db.RemoveCacheEntry($"`c{cid}`internal_state");
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

            return await db.CachedGetAsync($"`c{cid}`scoreboard", TimeSpan.FromSeconds(3), async () =>
            {
                var value = await db.Teams
                    .Where(t => t.ContestId == cid && t.Status == 1)
                    .Include(t => t.Affiliation)
                    .Include(t => t.RankCache)
                    .Include(t => t.ScoreCache)
                    .ToDictionaryAsync(a => a.TeamId);

                var result = new ScoreboardDataModel
                {
                    Data = value,
                    RefreshTime = DateTimeOffset.Now,
                    Statistics = new Dictionary<int, int>()
                };

                foreach (var (_, item) in value)
                {
                    foreach (var ot in item.ScoreCache)
                    {
                        var val = result.Statistics.GetValueOrDefault(ot.ProblemId);
                        if (ot.IsCorrectRestricted)
                            result.Statistics[ot.ProblemId] = ++val;
                    }
                }

                return result;
            });
        }

        public static Task<Dictionary<int, IGrouping<int, string>>> ListTeamMembersAsync(this AppDbContext db, int cid)
        {
            return db.CachedGetAsync($"`c{cid}`teams`members", TimeSpan.FromMinutes(5), async () =>
            {
                var query =
                    from tu in db.TeamMembers
                    where tu.ContestId == cid
                    join u in db.Users on tu.UserId equals u.Id
                    select new { tu.TeamId, u.UserName };

                return (await query.ToListAsync())
                    .GroupBy(a => a.TeamId, a => a.UserName)
                    .ToDictionary(g => g.Key);
            });
        }
    }
}
