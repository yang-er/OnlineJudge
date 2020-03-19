using JudgeWeb.Data;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Judgements
{
    public interface ISubmissionRepository
    {
        Task<Submission> CreateAsync(
            string code,
            string langid,
            int probid,
            int? contest,
            int uid,
            IPAddress ipAddr,
            string via,
            string username,
            Verdict? expected = null,
            DateTimeOffset? time = null);

        Task RejudgeAsync(
            Submission sub,
            Rejudge rejudge = null,
            bool fullTest = false,
            Action<Judging> oldSolve = null);

        Task<IEnumerable<(Detail, Testcase)>> GetDetailsAsync(int jid);

        Task<Submission> FindAsync(int sid, bool includeJudgings = false);

        Task UpdateAsync(Submission submission);

        Task<IFileInfo> GetRunFileAsync(
            int jid, int rid, string type,
            int? sid = null, int? pid = null);

        Task<IEnumerable<Submission>> ListAsync(
            Expression<Func<Submission, bool>> predicate = null);

        Task<IEnumerable<T>> ListAsync<T>(
            Expression<Func<Submission, bool>> predicate,
            Expression<Func<Submission, T>> projection);

        Task<Dictionary<int, string>> GetAuthorNamesAsync(params int[] sids);

        Task<string> GetAuthorNameAsync(int sid);

        ValueTask<(IEnumerable<ListSubmissionModel> list, int totPage)> ListWithJudgingAsync(
            Expression<Func<Submission, bool>> predicate = null,
            bool includeDetails = false,
            (int Page, int PageCount)? pagination = null);

        Task<Submission> FindByJudgingAsync(int jid);

        Task<IEnumerable<SubmissionStatistics>> StatisticsByUserAsync(int uid);
    }
}
