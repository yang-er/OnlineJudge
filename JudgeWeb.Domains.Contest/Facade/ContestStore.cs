using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

[assembly: Inject(typeof(IContestStore), typeof(ContestStore))]
namespace JudgeWeb.Domains.Contests
{
    public class ContestStore :
        IContestStore,
        ICrudRepositoryImpl<Contest>
    {
        public DbContext Context { get; }

        DbSet<Contest> Contests => Context.Set<Contest>();
        
        public ContestStore(DbContextAccessor context)
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
                        OpenRegister = c.RegisterDefaultCategory > 0,
                        Gym = c.Gym,
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
            Context.RemoveCacheEntry($"cont::list");
            Context.RemoveCacheEntry($"gym::list");
        }
    }
}
