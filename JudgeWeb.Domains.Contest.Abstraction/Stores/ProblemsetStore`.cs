using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IProblemsetStore
    {
        Task CreateAsync(ContestProblem problem);

        Task DeleteAsync(ContestProblem problem);

        Task UpdateAsync(int cid, int pid, Expression<Func<ContestProblem>> change);

        Task<ContestProblem[]> ListAsync(int cid);

        Task<IEnumerable<ProblemStatement>> StatementsAsync(int cid);

        Task<(bool ok, string msg)> CheckAvailabilityAsync(int cid, int pid, ClaimsPrincipal user);
    }
}
