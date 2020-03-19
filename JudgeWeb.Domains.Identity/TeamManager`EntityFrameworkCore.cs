using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public class EntityFrameworkCoreTeamManager<TContext> : EntityFrameworkCoreTeamManager
        where TContext : DbContext
    {
        public EntityFrameworkCoreTeamManager(TContext context)
            : base(context)
        {
        }
    }

    public class EntityFrameworkCoreTeamManager : TeamManager
    {
        public DbContext Context { get; }
        protected EntityFrameworkCoreTeamManager(DbContext context) => Context = context;

        DbSet<TrainingTeam> TrainingTeams => Context.Set<TrainingTeam>();
        DbSet<TrainingTeamUser> TrainingTeamUsers => Context.Set<TrainingTeamUser>();
        DbSet<TeamAffiliation> TeamAffiliations => Context.Set<TeamAffiliation>();

        protected override async Task CreateAsync(TrainingTeamUser teamUser)
        {
            int count = await TrainingTeamUsers.MergeAsync(
                sourceTable: new[] { new { teamUser.UserId, teamUser.TrainingTeamId } },
                targetKey: ttu => new { ttu.UserId, ttu.TrainingTeamId },
                sourceKey: ttu => new { ttu.UserId, ttu.TrainingTeamId },
                insertExpression: t => new TrainingTeamUser { UserId = t.UserId, TrainingTeamId = t.TrainingTeamId, Accepted = teamUser.Accepted },
                updateExpression: null, delete: false);
        }

        public override async Task<bool> CheckCreateAsync(User user)
        {
            var count = await TrainingTeams.CountAsync(t => t.UserId == user.Id);
            return count < MaxTeams;
        }

        public override async Task<bool> CheckCreateAsync(TrainingTeam team)
        {
            var item = await TrainingTeamUsers
                .CountAsync(a => a.TrainingTeamId == team.TrainingTeamId);
            return item < MaxMembers;
        }

        protected override async Task<TrainingTeam> CreateAsync(TrainingTeam team)
        {
            TrainingTeams.Add(team);
            await Context.SaveChangesAsync();
            return team;
        }

        public override Task<TrainingTeam> FindTeamByIdAsync(int teamid)
        {
            return TrainingTeams
                .Include(t => t.Affiliation)
                .Where(t => t.TrainingTeamId == teamid)
                .SingleOrDefaultAsync();
        }

        public override Task<TeamAffiliation> FindAffiliationAsync(int affId)
        {
            return TeamAffiliations
                .Where(a => a.AffiliationId == affId)
                .SingleOrDefaultAsync();
        }

        public override async Task<IEnumerable<TeamAffiliation>> ListAffiliationsAsync()
        {
            return await TeamAffiliations.ToListAsync();
        }

        public override Task<TrainingTeamUser> IsInTeamAsync(User user, TrainingTeam team)
        {
            return TrainingTeamUsers
                .Where(tu => tu.UserId == user.Id && tu.TrainingTeamId == team.TrainingTeamId)
                .SingleOrDefaultAsync();
        }

        public override Task DismissAsync(TrainingTeam team)
        {
            TrainingTeams.Remove(team);
            return Context.SaveChangesAsync();
        }

        public override Task RemoveAsync(TrainingTeamUser user)
        {
            TrainingTeamUsers.Remove(user);
            return Context.SaveChangesAsync();
        }

        public override async Task<IEnumerable<IGrouping<TrainingTeam, TrainingTeamUser>>> ListAsync(User user)
        {
            var query =
                from ttu in TrainingTeamUsers
                where ttu.UserId == user.Id && ttu.Accepted == true
                join t in TrainingTeams on ttu.TrainingTeamId equals t.TrainingTeamId
                join tu in TrainingTeamUsers on t.TrainingTeamId equals tu.TrainingTeamId
                join u in Context.Set<User>() on tu.UserId equals u.Id
                select new { t, tuu = new TrainingTeamUser(tu, u.UserName, u.Email) };
            var results = await query.AsTracking().ToListAsync();
            return results.GroupBy(k => k.t, v => v.tuu);
        }

        public override Task<List<TrainingTeamUser>> ListMembersAsync(TrainingTeam team)
        {
            var uquery =
                from tu in TrainingTeamUsers
                where tu.TrainingTeamId == team.TrainingTeamId
                join u in Context.Set<User>() on tu.UserId equals u.Id
                select new TrainingTeamUser(tu, u.UserName, u.Email);
            return uquery.ToListAsync();
        }

        public override Task UpdateAsync(TrainingTeam team)
        {
            TrainingTeams.Update(team);
            return Context.SaveChangesAsync();
        }

        public override Task UpdateAsync(TrainingTeamUser user)
        {
            TrainingTeamUsers.Update(user);
            return Context.SaveChangesAsync();
        }
    }
}
