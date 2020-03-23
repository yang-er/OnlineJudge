using JudgeWeb.Data;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IJudgingStore :
        ICreateRepository<Judging>,
        IUpdateRepository<Judging>,
        ICreateRepository<Detail>
    {
        Task<(Judging j, int pid, int cid, int uid, DateTimeOffset time)> FindAsync(int judgingId);

        Task<List<T>> ListAsync<T>(
            Expression<Func<Judging, bool>> predicate,
            Expression<Func<Judging, T>> selector,
            int topCount);

        Task<IFileInfo> GetRunFileAsync(
            int judgingId, int runId, string type,
            int? submissionId = null, int? problemId = null);

        Task<IFileInfo> SetRunFileAsync(
            int judgingId, int runId, string type, byte[] content);

        Task<IEnumerable<T>> GetDetailsAsync<T>(
            int problemId, int judgingId,
            Expression<Func<Testcase, Detail, T>> selector);

        Task<Detail> SummarizeAsync(Judging j);

        public async Task<IEnumerable<(Detail, Testcase)>> GetDetailsAsync(int pid, int jid)
        {
            var result = await GetDetailsAsync(pid, jid, (t, d) => new { t, d });
            return result.Select(a => (a.d, a.t));
        }
    }
}
