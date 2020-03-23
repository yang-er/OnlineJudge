using JudgeWeb.Data;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IRejudgingStore : ICrudRepository<Rejudge>
    {
        Task RejudgeAsync(Submission submission, bool fullTest = false);

        Task<int> BatchRejudgeAsync(
            Expression<Func<Submission, Judging, bool>> predicate,
            Rejudge rejudge = null, bool fullTest = false);
    }
}
