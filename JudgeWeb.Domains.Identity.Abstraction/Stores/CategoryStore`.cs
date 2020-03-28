using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public interface ICategoryStore :
        ICrudRepository<TeamCategory>
    {
        Task<TeamCategory> FindAsync(int affId);

        Task<IEnumerable<TeamCategory>> ListAsync(
            Expression<Func<TeamCategory, bool>>? predicate = null);

        IQueryable<int> GetContestFilter(int cid);
    }
}
