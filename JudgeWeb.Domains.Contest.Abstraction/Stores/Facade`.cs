using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IContestFacade
    {
        IContestStore Contests { get; }

        IProblemsetStore Problemset { get; }

        ITeamStore Teams { get; }

        ISubmissionStore Submissions { get; }

        Task<Dictionary<string, Language>> ListLanguageAsync(int cid);

        Task<Dictionary<int, int>> StatisticsProblemAsync();

        Task<Dictionary<int, int>> StatisticsTeamAsync();

        public Task<Submission> SubmitAsync(
            string code,
            string language,
            int problemId,
            Contest contest,
            int teamId,
            IPAddress ipAddr,
            string via,
            string username)
        {
            return Submissions.CreateAsync(
                code, language, problemId, contest.ContestId,
                teamId, ipAddr, via, username, null, null,
                fullJudge: contest.RankingStrategy == 2);
        }
    }
}
