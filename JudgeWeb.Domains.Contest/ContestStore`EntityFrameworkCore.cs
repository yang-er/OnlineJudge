using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public class EntityFrameworkCoreContestStore<TContext>
        : EntityFrameworkCoreContestStore
        where TContext : DbContext
    {
        public EntityFrameworkCoreContestStore(TContext context)
            : base(context)
        {
        }
    }

    public class EntityFrameworkCoreContestStore : IContestStore
    {
        public DbContext Context { get; }

        protected EntityFrameworkCoreContestStore(DbContext context) => Context = context;

        public DbSet<TeamMember> Members => Context.Set<TeamMember>();
        public DbSet<Contest> Contests => Context.Set<Contest>();
        public DbSet<Team> Teams => Context.Set<Team>();
        public DbSet<ContestProblem> ContestProblems => Context.Set<ContestProblem>();
        public DbSet<Clarification> Clarifications => Context.Set<Clarification>();
        public DbSet<TeamAffiliation> Affiliations => Context.Set<TeamAffiliation>();
        public DbSet<TeamCategory> Categories => Context.Set<TeamCategory>();

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
                    from t in Teams
                    join c in Categories on t.CategoryId equals c.CategoryId
                    where c.IsPublic && t.Status < 3
                    group 1 by t.ContestId into g
                    select new { g.Key, Count = g.Count() };
                var results = await teamQuery.ToDictionaryAsync(a => a.Key, a => a.Count);
                contests.ForEach(c => c.TeamCount = results.GetValueOrDefault(c.ContestId));

                contests.Sort();
                return contests;
            });
        }

        public async Task<HashSet<int>> GetRegisteredContestAsync(int uid)
        {
            var members = await Members
                .Where(t => t.UserId == uid)
                .Select(t => t.ContestId)
                .ToArrayAsync();
            return members.ToHashSet();
        }
    }
}
