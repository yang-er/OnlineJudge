using JudgeWeb.Data;
using JudgeWeb.Features.Scoreboard;
using MediatR;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public class JudgingFinishedRequest : IRequest
    {
        public Contest Contest { get; set; }
        
        public DateTimeOffset SubmitTime { get; set; }

        public int ProblemId { get; set; }

        public int TeamId { get; set; }

        public Judging Judging { get; set; }

        public int ContestId { get; set; }

        public int CfScore { get; set; }

        public bool Frozen => Contest.GetState() >= ContestState.Frozen;

        public DateTimeOffset ContestTime => Contest.StartTime ?? DateTimeOffset.Now;

        public int TotalScore => Judging.TotalScore ?? 0;

        public Verdict Verdict => Judging.Status;
    }
}

namespace JudgeWeb.Domains.Contests
{
    public static partial class ScoreboardMediatorExtensions
    {
        public static Task JudgingFinished(this IMediator mediator,
            int contestid, DateTimeOffset time, int probid, int teamid, Judging judging)
        {
            return mediator.Send(new JudgingFinishedRequest
            {
                ContestId = contestid,
                SubmitTime = time,
                ProblemId = probid,
                TeamId = teamid,
                Judging = judging,
            });
        }
    }
}
