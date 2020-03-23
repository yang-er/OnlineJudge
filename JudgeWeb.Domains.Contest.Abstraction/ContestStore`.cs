using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IContestStore :
        ICrudRepository<Contest>
    {
        Task<List<ContestListModel>> ListAsync(bool gym);

        Task<List<Contest>> ListAsync();

        Task<HashSet<int>> GetRegisteredContestAsync(int uid);

        Task<List<TeamMember>> GetRegisteredContestWithDetailAsync(int uid);
    }
}
