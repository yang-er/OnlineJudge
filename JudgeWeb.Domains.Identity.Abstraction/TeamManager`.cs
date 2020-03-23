using JudgeWeb.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public interface ITeamManager :
        ICrudRepository<TrainingTeam>,
        ICrudRepository<TrainingTeamUser>,
        ICrudRepository<TeamAffiliation>,
        ICrudRepository<TeamCategory>
    {
        public const int MaxTeams = 10;
        public const int MaxMembers = 5;

        Task<TeamAffiliation> FindAffiliationAsync(int affId);

        Task<IEnumerable<TeamAffiliation>> ListAffiliationsAsync();

        Task<IEnumerable<TeamCategory>> ListCategoriesAsync();

        Task<TrainingTeam> FindTeamByIdAsync(int teamid);

        Task<List<TrainingTeamUser>> ListMembersAsync(TrainingTeam team);

        Task<TrainingTeamUser> IsInTeamAsync(User user, TrainingTeam team);

        Task<IEnumerable<IGrouping<TrainingTeam, TrainingTeamUser>>> ListAsync(User user);

        Task<bool> CheckCreateAsync(User user);

        Task<bool> CheckCreateAsync(TrainingTeam team);

        Task<TeamCategory> FindCategoryAsync(int catId);

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
