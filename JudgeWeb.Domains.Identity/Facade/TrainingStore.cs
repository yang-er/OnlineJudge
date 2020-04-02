using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(ITrainingStore), typeof(TrainingStore))]
namespace JudgeWeb.Domains.Identity
{
    public class TrainingStore :
        ITrainingStore,
        ICrudRepositoryImpl<TrainingTeam>,
        ICrudRepositoryImpl<TrainingTeamUser>
    {
        public DbContext Context { get; }
        public TrainingStore(DbContext context) => Context = context;

        DbSet<TrainingTeam> TrainingTeams => Context.Set<TrainingTeam>();
        DbSet<TrainingTeamUser> TrainingTeamUsers => Context.Set<TrainingTeamUser>();

        public async Task<bool> CheckCreateAsync(User user)
        {
            var count = await TrainingTeams.CountAsync(t => t.UserId == user.Id);
            return count < ITrainingStore.MaxTeams;
        }

        public async Task<bool> CheckCreateAsync(TrainingTeam team)
        {
            var item = await TrainingTeamUsers
                .CountAsync(a => a.TrainingTeamId == team.TrainingTeamId);
            return item < ITrainingStore.MaxMembers;
        }

        public Task<TrainingTeam> FindTeamByIdAsync(int teamid)
        {
            return TrainingTeams
                .Include(t => t.Affiliation)
                .Where(t => t.TrainingTeamId == teamid)
                .SingleOrDefaultAsync();
        }

        public Task<TrainingTeamUser> IsInTeamAsync(User user, TrainingTeam team)
        {
            return TrainingTeamUsers
                .Where(tu => tu.UserId == user.Id && tu.TrainingTeamId == team.TrainingTeamId)
                .SingleOrDefaultAsync();
        }

        public async Task<ILookup<TrainingTeam, TrainingTeamUser>> ListAsync(int uid, bool active = false)
        {
            var query =
                from ttu in TrainingTeamUsers
                where ttu.UserId == uid && ttu.Accepted == true
                join t in TrainingTeams on ttu.TrainingTeamId equals t.TrainingTeamId
                join tu in TrainingTeamUsers on t.TrainingTeamId equals tu.TrainingTeamId
                where tu.Accepted == true || !active
                join u in Context.Set<User>() on tu.UserId equals u.Id
                select new { t, tuu = new TrainingTeamUser(tu, u.UserName, u.Email) };
            var results = await query.AsTracking().ToListAsync();
            return results.ToLookup(k => k.t, v => v.tuu);
        }

        public Task<List<TrainingTeamUser>> ListMembersAsync(TrainingTeam team, bool active = false)
        {
            var uquery =
                from tu in TrainingTeamUsers
                where tu.TrainingTeamId == team.TrainingTeamId
                where tu.Accepted == true || !active
                join u in Context.Set<User>() on tu.UserId equals u.Id
                select new TrainingTeamUser(tu, u.UserName, u.Email);
            return uquery.ToListAsync();
        }
    }
}
