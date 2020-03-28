using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public class ContestStore :
        IContestStore,
        ICrudRepositoryImpl<Contest>
    {
        public DbContext Context { get; }

        DbSet<Contest> Contests => Context.Set<Contest>();
        
        DbSet<ContestProblem> Problems => Context.Set<ContestProblem>();

        public ContestStore(DbContext context)
        {
            Context = context;
        }

        public Task<List<ContestListModel>> ListAsync(bool gym)
        {
            return Context.CachedGetAsync(
            tag: $"{(gym ? "cont" : "gym")}::list",
            timeSpan: TimeSpan.FromMinutes(5),
            factory: async () =>
            {
                var contests = await Contests
                    .Where(c => c.Gym == gym)
                    .Select(c => new ContestListModel
                    {
                        Name = c.Name,
                        RankingStrategy = c.RankingStrategy,
                        ContestId = c.ContestId,
                        EndTime = c.EndTime,
                        IsPublic = c.IsPublic,
                        StartTime = c.StartTime,
                        OpenRegister = c.RegisterDefaultCategory > 0
                    })
                    .ToListAsync();

                var teamQuery =
                    from t in Context.Set<Team>()
                    where t.Category.IsPublic && t.Status < 3
                    group 1 by t.ContestId into g
                    select new { g.Key, Count = g.Count() };
                var results = await teamQuery.ToDictionaryAsync(a => a.Key, a => a.Count);
                contests.ForEach(c => c.TeamCount = results.GetValueOrDefault(c.ContestId));

                contests.Sort();
                return contests;
            });
        }

        public Task<List<Contest>> ListAsync()
        {
            return Contests.ToListAsync();
        }

        public Task<Contest> FindAsync(int cid)
        {
            return Contests
                .Where(c => c.ContestId == cid)
                .CachedSingleOrDefaultAsync($"`c{cid}`info", TimeSpan.FromMinutes(5));
        }

        public Task<ContestProblem[]> ListProblemsAsync(int cid)
        {
            return Context.CachedGetAsync($"`c{cid}`probs", TimeSpan.FromMinutes(5), async () =>
            {
                var result = await Problems
                    .Where(cp => cp.ContestId == cid)
                    .Select(cp => new ContestProblem(cp, cp.p.Title, cp.p.TimeLimit, cp.p.MemoryLimit, cp.p.CombinedRunCompare, cp.p.Shared))
                    .ToArrayAsync();

                Array.Sort(result, (a, b) => a.ShortName.CompareTo(b.ShortName));
                for (int i = 0; i < result.Length; i++)
                    result[i].Rank = i + 1;

                var query2 =
                    from cp in Problems
                    where cp.ContestId == cid
                    join t in Context.Set<Testcase>() on cp.ProblemId equals t.ProblemId
                    group t by cp.ProblemId into g
                    select new { g.Key, Count = g.Count(), Score = g.Sum(t => t.Point) };

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

        public Task<int> MaxEventIdAsync(int cid)
        {
            return Context.Set<Event>()
                .Where(e => e.ContestId == cid)
                .OrderByDescending(e => e.EventId)
                .Select(e => e.EventId)
                .Take(1)
                .SingleOrDefaultAsync();
        }

        public async Task UpdateAsync(int cid, Expression<Func<Contest, Contest>> expression)
        {
            int solved = await Contests
                .Where(c => c.ContestId == cid)
                .BatchUpdateAsync(expression);

            Context.RemoveCacheEntry($"`c{cid}`info");
            Context.RemoveCacheEntry($"`c{cid}`internal_state");
        }

        public Task<Dictionary<string, Language>> ListLanguageAsync(int cid)
        {
            return Context.Set<Language>().CachedToDictionaryAsync(
                keySelector: k => k.Id,
                tag: $"`c{cid}`langs",
                timeSpan: TimeSpan.FromMinutes(10));
        }
    }
}
