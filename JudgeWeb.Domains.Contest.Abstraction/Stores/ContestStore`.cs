using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IContestStore :
        ICreateRepository<Contest>
    {
        Task<Contest> FindAsync(int cid);

        Task<List<ContestListModel>> ListAsync(bool gym);

        Task<List<Contest>> ListAsync();

        Task<ContestProblem[]> ListProblemsAsync(int cid);

        Task<Dictionary<string, Language>> ListLanguageAsync(int cid);

        Task<int> MaxEventIdAsync(int cid);

        Task UpdateAsync(int cid, Expression<Func<Contest, Contest>> expression);
    }
}
