using JudgeWeb.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public interface ITrainingStore :
        ICrudRepository<TrainingTeam>,
        ICrudRepository<TrainingTeamUser>
    {
        public const int MaxTeams = 10;
        public const int MaxMembers = 5;
        
        Task<TrainingTeam> FindTeamByIdAsync(int teamid);

        Task<List<TrainingTeamUser>> ListMembersAsync(TrainingTeam team);

        Task<TrainingTeamUser> IsInTeamAsync(User user, TrainingTeam team);

        Task<IEnumerable<IGrouping<TrainingTeam, TrainingTeamUser>>> ListAsync(User user);

        Task<bool> CheckCreateAsync(User user);

        Task<bool> CheckCreateAsync(TrainingTeam team);

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
