using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IContestStore
    {
        Task<List<ContestListModel>> ListAsync(bool gym);

        Task<HashSet<int>> GetRegisteredContestAsync(int uid);
    }
}
