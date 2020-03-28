using JudgeWeb.Data;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Domains.Contests
{
    public interface IScoreboardService
    {
        void RefreshCache(Contest contest, DateTimeOffset now);

        void SubmissionCreated(int cid, Submission submission);

        void SubmissionCreated(Contest contest, Submission submission);

        void JudgingFinished(Contest contest, DateTimeOffset time, int probid, int teamid, Judging judging);

        void JudgingFinished(int cid, DateTimeOffset time, int probid, int teamid, Judging judging);

        IEnumerable<Team> SortBy(Contest contest, IEnumerable<Team> source, bool isPublic);
    }
}
