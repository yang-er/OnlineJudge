using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IProblemStore :
        ICrudRepository<Problem>
    {
        Task ToggleAsync(
            int pid,
            Expression<Func<Problem, bool>> expression,
            bool tobe);

        Task<Problem> FindAsync(int pid);

        Task<(IEnumerable<Problem> model, int totPage)> ListAsync(
            int? uid, int page, int perCount);
    }
}
