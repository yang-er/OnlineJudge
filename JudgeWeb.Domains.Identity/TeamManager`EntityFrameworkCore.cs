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

    public class EntityFrameworkCoreTeamManager :
        ITeamManager,
        ICrudRepositoryImpl<TrainingTeam>,
        ICrudRepositoryImpl<TrainingTeamUser>,
        ICrudRepositoryImpl<TeamAffiliation>,
        ICrudRepositoryImpl<TeamCategory>
    {
        public DbContext Context { get; }
        protected EntityFrameworkCoreTeamManager(DbContext context) => Context = context;

        DbSet<TrainingTeam> TrainingTeams => Context.Set<TrainingTeam>();
        DbSet<TrainingTeamUser> TrainingTeamUsers => Context.Set<TrainingTeamUser>();
        DbSet<TeamAffiliation> TeamAffiliations => Context.Set<TeamAffiliation>();
        DbSet<TeamCategory> TeamCategories => Context.Set<TeamCategory>();

        public async Task<bool> CheckCreateAsync(User user)
        {
            var count = await TrainingTeams.CountAsync(t => t.UserId == user.Id);
            return count < ITeamManager.MaxTeams;
        }

        public async Task<bool> CheckCreateAsync(TrainingTeam team)
        {
            var item = await TrainingTeamUsers
                .CountAsync(a => a.TrainingTeamId == team.TrainingTeamId);
            return item < ITeamManager.MaxMembers;
        }

        public Task<TrainingTeam> FindTeamByIdAsync(int teamid)
        {
            return TrainingTeams
                .Include(t => t.Affiliation)
                .Where(t => t.TrainingTeamId == teamid)
                .SingleOrDefaultAsync();
        }

        public Task<TeamAffiliation> FindAffiliationAsync(int affId)
        {
            return TeamAffiliations
                .Where(a => a.AffiliationId == affId)
                .SingleOrDefaultAsync();
        }

        public Task<TeamCategory> FindCategoryAsync(int catId)
        {
            return TeamCategories
                .Where(a => a.CategoryId == catId)
                .SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<TeamAffiliation>> ListAffiliationsAsync()
        {
            return await TeamAffiliations.ToListAsync();
        }

        public async Task<IEnumerable<TeamCategory>> ListCategoriesAsync()
        {
            return await TeamCategories.ToListAsync();
        }

        public Task<TrainingTeamUser> IsInTeamAsync(User user, TrainingTeam team)
        {
            return TrainingTeamUsers
                .Where(tu => tu.UserId == user.Id && tu.TrainingTeamId == team.TrainingTeamId)
                .SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<IGrouping<TrainingTeam, TrainingTeamUser>>> ListAsync(User user)
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

        public Task<List<TrainingTeamUser>> ListMembersAsync(TrainingTeam team)
        {
            var uquery =
                from tu in TrainingTeamUsers
                where tu.TrainingTeamId == team.TrainingTeamId
                join u in Context.Set<User>() on tu.UserId equals u.Id
                select new TrainingTeamUser(tu, u.UserName, u.Email);
            return uquery.ToListAsync();
        }

    }
}
