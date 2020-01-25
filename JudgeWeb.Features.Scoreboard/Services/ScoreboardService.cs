﻿using JudgeWeb.Data;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Features.Scoreboard
{
    public class ScoreboardService : IScoreboardService
    {
        internal static IRankingStrategy[] SC = new IRankingStrategy[]
        {
            new XCPCRank(),
            null,
            new OIRank(),
        };

        public void JudgingFinished(Contest contest, DateTimeOffset time, int probid, int teamid, Judging judging)
        {
            ScoreboardUpdateService.OnUpdateRequested(new ScoreboardEventArgs
            {
                Balloon = contest.BalloonAvaliable,
                ContestId = contest.ContestId,
                ContestTime = contest.StartTime ?? DateTimeOffset.Now,
                EndTime = contest.EndTime ?? (DateTimeOffset.Now + TimeSpan.FromSeconds(5)),
                EventType = judging.Status == Verdict.Accepted ? 1
                    : judging.Status == Verdict.CompileError ? 3 : 2,
                FreezeTime = contest.FreezeTime,
                Frozen = contest.GetState() >= ContestState.Frozen,
                ProblemId = probid,
                RankStrategy = contest.RankingStrategy,
                SubmissionId = judging.SubmissionId,
                SubmitTime = time,
                TeamId = teamid,
            });
        }

        public void RefreshCache(Contest contest, DateTimeOffset now)
        {
            ScoreboardUpdateService.OnUpdateRequested(new ScoreboardEventArgs
            {
                Balloon = contest.BalloonAvaliable,
                ContestId = contest.ContestId,
                ContestTime = contest.StartTime ?? DateTimeOffset.Now,
                EndTime = contest.EndTime ?? (DateTimeOffset.Now + TimeSpan.FromSeconds(5)),
                EventType = 5,
                FreezeTime = contest.FreezeTime,
                Frozen = contest.GetState() >= ContestState.Frozen,
                RankStrategy = contest.RankingStrategy,
                SubmitTime = now,
            });
        }

        public IEnumerable<BoardQuery> SortBy(Contest contest, IEnumerable<BoardQuery> source, bool isPublic)
        {
            return SC[contest.RankingStrategy].SortByRule(source, isPublic);
        }

        public void SubmissionCreated(Contest contest, Submission submission)
        {
            ScoreboardUpdateService.OnUpdateRequested(new ScoreboardEventArgs
            {
                Balloon = contest.BalloonAvaliable,
                ContestId = contest.ContestId,
                ContestTime = contest.StartTime ?? DateTimeOffset.Now,
                EndTime = contest.EndTime ?? (DateTimeOffset.Now + TimeSpan.FromSeconds(5)),
                EventType = 4,
                FreezeTime = contest.FreezeTime,
                Frozen = contest.GetState() >= ContestState.Frozen,
                ProblemId = submission.ProblemId,
                RankStrategy = contest.RankingStrategy,
                SubmissionId = submission.SubmissionId,
                SubmitTime = submission.Time,
                TeamId = submission.Author,
            });
        }
    }
}
