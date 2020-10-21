using JudgeWeb.Data;
using JudgeWeb.Features.Scoreboard;
using MediatR;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public class RefreshScoreboardCacheRequest : IRequest
    {
        public Contest Contest { get; set; }

        public DateTimeOffset Deadline { get; set; }

        public int ContestId => Contest.ContestId;

        public DateTimeOffset EndTime => Contest.EndTime ?? (DateTimeOffset.Now + TimeSpan.FromSeconds(5));

        public DateTimeOffset ContestTime => Contest.StartTime ?? DateTimeOffset.Now;

        public DateTimeOffset? FreezeTime => Contest.FreezeTime;
    }
}

namespace JudgeWeb.Domains.Contests
{
    public static partial class ScoreboardMediatorExtensions
    {
        public static Task RefreshScoreboardCache(this IMediator mediator, Contest contest)
        {
            return mediator.Send(new RefreshScoreboardCacheRequest
            {
                Contest = contest,
                Deadline = DateTimeOffset.Now,
            });
        }
    }
}
