using JudgeWeb.Features.Scoreboard;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Data
{
    public interface IScoreboardService
    {
        void RefreshCache(Contest contest, DateTimeOffset now);

        void SubmissionCreated(Contest contest, Submission submission);

        void JudgingFinished(Contest contest, DateTimeOffset time, int probid, int teamid, Judging judging);

        IEnumerable<BoardQuery> SortBy(Contest contest, IEnumerable<BoardQuery> source, bool isPublic);
    }
}
