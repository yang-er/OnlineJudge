using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public interface IAffiliationStore :
        ICrudRepository<TeamAffiliation>
    {
        Task<TeamAffiliation> FindAsync(int affId);

        Task<TeamAffiliation> FindAsync(string externalId);

        Task<IEnumerable<TeamAffiliation>> ListAsync(
            Expression<Func<TeamAffiliation, bool>>? predicate = null);

        IQueryable<int> GetContestFilter(int cid);
    }
}
