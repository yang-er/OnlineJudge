using JudgeWeb.Data;
using System;
using System.Collections.Generic;
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

        Task<int> GetJuryStatusAsync(int cid);

        Task<Rejudge> FindAsync(int cid, int rjid);

        Task<List<Rejudge>> ListAsync(int cid);

        Task<IEnumerable<(
            Judging,
            Judging,
            int pid,
            string lang,
            DateTimeOffset time,
            int teamid)>> ViewAsync(Rejudge rejudge);

        Task CancelAsync(Rejudge rejudge, int uid);

        Task ApplyAsync(Rejudge rejudge, int uid);
    }
}
