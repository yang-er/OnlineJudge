using JudgeWeb.Data;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
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
            DateTimeOffset? time = null,
            bool fullJudge = false);

        Task RejudgeAsync(Submission sub, bool fullTest = false);

        Task<IEnumerable<(Detail, Testcase)>> GetDetailsAsync(int pid, int jid);

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

        ValueTask<(IEnumerable<T> list, int totPage)> ListWithJudgingAsync<T>(
            (int Page, int PageCount) pagination,
            Expression<Func<Submission, Judging, T>> selector,
            Expression<Func<Submission, bool>> predicate = null);

        Task<IEnumerable<T>> ListWithJudgingAsync<T>(
            Expression<Func<Submission, Judging, T>> selector,
            Expression<Func<Submission, bool>> predicate = null);

        private Expression<Func<Submission, Judging, ListSubmissionModel>> CreateSelector(bool includeDetails)
        {
            Expression<Func<Submission, Judging, ListSubmissionModel>> res =
            (s, j) => new ListSubmissionModel
            {
                SubmissionId = s.SubmissionId,
                JudgingId = j.JudgingId,
                Language = s.Language,
                AuthorId = s.Author,
                CodeLength = s.CodeLength,
                ContestId = s.ContestId,
                Verdict = j.Status,
                Time = s.Time,
                Ip = s.Ip,
                ProblemId = s.ProblemId,
                ExecutionMemory = j.ExecuteMemory,
                Expected = s.ExpectedResult,
                ExecutionTime = j.ExecuteTime,
                Details = j.Details,
            };

            if (includeDetails) return res;
            var body = res.Body as MemberInitExpression;
            body = body.Update(
                newExpression: body.NewExpression,
                bindings: body.Bindings.Where(b => b.Member.Name != "Details"));
            return res.Update(body, res.Parameters);
        }

        public ValueTask<(IEnumerable<ListSubmissionModel> list, int totPage)> ListWithJudgingAsync(
            (int Page, int PageCount) pagination,
            Expression<Func<Submission, bool>> predicate = null,
            bool includeDetails = false)
        {
            return ListWithJudgingAsync(pagination, CreateSelector(includeDetails), predicate);
        }

        public Task<IEnumerable<ListSubmissionModel>> ListWithJudgingAsync(
            Expression<Func<Submission, bool>> predicate = null,
            bool includeDetails = false)
        {
            return ListWithJudgingAsync(CreateSelector(includeDetails), predicate);
        }

        Task<Submission> FindByJudgingAsync(int jid);

        Task<IEnumerable<SubmissionStatistics>> StatisticsByUserAsync(int uid);
    }
}
