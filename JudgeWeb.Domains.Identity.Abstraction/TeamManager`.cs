using JudgeWeb.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public abstract class TeamManager
    {
        public const int MaxTeams = 10;

        public const int MaxMembers = 5;

        protected abstract Task<TrainingTeam> CreateAsync(TrainingTeam team);

        protected abstract Task CreateAsync(TrainingTeamUser teamUser);

        public abstract Task<TeamAffiliation> FindAffiliationAsync(int affId);

        public abstract Task<IEnumerable<TeamAffiliation>> ListAffiliationsAsync();

        public abstract Task<TrainingTeam> FindTeamByIdAsync(int teamid);

        public abstract Task<List<TrainingTeamUser>> ListMembersAsync(TrainingTeam team);

        public abstract Task<TrainingTeamUser> IsInTeamAsync(User user, TrainingTeam team);

        public abstract Task DismissAsync(TrainingTeam team);

        public abstract Task RemoveAsync(TrainingTeamUser user);

        public abstract Task<IEnumerable<IGrouping<TrainingTeam, TrainingTeamUser>>> ListAsync(User user);

        public abstract Task<bool> CheckCreateAsync(User user);

        public abstract Task<bool> CheckCreateAsync(TrainingTeam team);

        public abstract Task UpdateAsync(TrainingTeam team);

        public abstract Task UpdateAsync(TrainingTeamUser user);

        public Task AddTeamMemberAsync(TrainingTeam team, User user)
        {
            return CreateAsync(new TrainingTeamUser
            {
                TrainingTeamId = team.TrainingTeamId,
                UserId = user.Id,
            });
        }

        public async Task<int> CreateTeamAsync(string teamName, User user, int affid)
        {
            var t = await CreateAsync(new TrainingTeam
            {
                AffiliationId = affid,
                TeamName = teamName,
                UserId = user.Id,
                Time = System.DateTimeOffset.Now,
            });

            int teamId = t.TrainingTeamId;

            await CreateAsync(new TrainingTeamUser
            {
                TrainingTeamId = teamId,
                UserId = user.Id,
                Accepted = true
            });

            return teamId;
        }
    }
}
