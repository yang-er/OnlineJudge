using JudgeWeb.Data;
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

        Task<List<TeamCategory>> ListCategoryAsync(int cid, bool? requirePublic = null);

        Task<ScoreboardDataModel> LoadScoreboardAsync(int cid);

        Task<Dictionary<int, string>> ListNamesAsync(int cid);

        Task<ILookup<int, string>> ListMembersAsync(int cid);

        Task<Team> FindByIdAsync(int cid, int teamid);

        Task<Team> FindByUserAsync(int cid, int uid);

        Task<int> CreateAsync(Team team, int[]? uids);

        Task UpdateAsync(Team team);

        Task<IEnumerable<int>> DeleteAsync(Team team);
    }
}
