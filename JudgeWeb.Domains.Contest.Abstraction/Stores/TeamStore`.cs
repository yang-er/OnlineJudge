using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface ITeamStore :
        ICrudRepository<TeamMember>
    {
        Task<List<T>> ListAsync<T>(int cid,
            Expression<Func<Team, T>> selector,
            Expression<Func<Team, bool>>? predicate = null,
            (string, TimeSpan)? cacheTag = null);

        Task<T> FindAsync<T>(int cid, int tid,
            Expression<Func<Team, T>> selector);

        Task<int> GetJuryStatusAsync(int cid);

        Task<HashSet<int>> ListRegisteredAsync(int uid);

        Task<List<TeamMember>> ListRegisteredWithDetailAsync(int uid);

        Task<List<TeamAffiliation>> ListAffiliationAsync(int cid, bool filtered = true);

        Task<HashSet<int>> ListMemberUidsAsync(int cid);

        Task<List<TeamCategory>> ListCategoryAsync(int cid, bool? requirePublic = null);

        Task<ScoreboardDataModel> LoadScoreboardAsync(int cid);

        Task<Dictionary<int, string>> ListNamesAsync(int cid);

        Task<ILookup<int, string>> ListMembersAsync(int cid);

        Task<Dictionary<int, (int ac, int tot)>> StatisticsSubmissionAsync(int cid, int teamid);

        Task<Team> FindByIdAsync(int cid, int teamid);

        Task<Team> FindByUserAsync(int cid, int uid);

        Task<List<(Team team, string password)>> BatchCreateAsync(
            UserManager userManager,
            int cid,
            TeamAffiliation affiliation,
            TeamCategory category,
            string[] teamNames);

        Task<int> BatchLockOutAsync(int cid);

        Task<int> CreateAsync(Team team, int[]? uids);

        Task UpdateAsync(int cid, int teamid, Expression<Func<Team, Team>> activator);

        Task<IEnumerable<int>> DeleteAsync(Team team);
    }
}
