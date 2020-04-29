using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface ISubmissionStore :
        IUpdateRepository<Submission>
    {
        Task<Submission> CreateAsync(
            string code,
            string language,
            int problemId,
            int? contestId,
            int userId,
            IPAddress ipAddr,
            string via,
            string username,
            Verdict? expected = null,
            DateTimeOffset? time = null,
            bool fullJudge = false);

        Task<Submission> FindAsync(
            int submissionId,
            bool includeJudgings = false);

        Task<(string src, string ext)?> GetFileAsync(int sid);

        Task<Submission> FindByJudgingAsync(int jid);

        Task UpdateStatisticsAsync(int cid, int author, int probid, bool ac);

        Task<IEnumerable<SubmissionStatistics>> StatisticsByUserAsync(int uid);

        Task<Dictionary<int, string>> GetAuthorNamesAsync(Expression<Func<Submission, bool>> sids);

        Task<string> GetAuthorNameAsync(int sid);

        Task<IEnumerable<T>> ListAsync<T>(
            Expression<Func<Submission, T>> projection,
            Expression<Func<Submission, bool>> predicate = null);

        Task<(IEnumerable<T> list, int count)> ListWithJudgingAsync<T>(
            (int Page, int PageCount) pagination,
            Expression<Func<Submission, Judging, T>> selector,
            Expression<Func<Submission, bool>> predicate = null);

        Task<IEnumerable<T>> ListWithJudgingAsync<T>(
            Expression<Func<Submission, Judging, T>> selector,
            Expression<Func<Submission, bool>> predicate = null,
            int? limits = null);

        private Expression<Func<Submission, Judging, ListSubmissionModel>> CreateSelector(bool includeDetails)
        {
            if (includeDetails)
                return (s, j) => new ListSubmissionModel
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
            else
                return (s, j) => new ListSubmissionModel
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
                };
        }

        public Task<(IEnumerable<ListSubmissionModel> list, int count)> ListWithJudgingAsync(
            (int Page, int PageCount) pagination,
            Expression<Func<Submission, bool>> predicate = null,
            bool includeDetails = false)
        {
            return ListWithJudgingAsync(pagination, CreateSelector(includeDetails), predicate);
        }

        public Task<IEnumerable<ListSubmissionModel>> ListWithJudgingAsync(
            Expression<Func<Submission, bool>> predicate = null,
            bool includeDetails = false,
            int? limits = null)
        {
            return ListWithJudgingAsync(CreateSelector(includeDetails), predicate, limits);
        }
    }
}
