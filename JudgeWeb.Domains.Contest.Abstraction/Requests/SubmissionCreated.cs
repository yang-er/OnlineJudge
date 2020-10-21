using JudgeWeb.Data;
using JudgeWeb.Features.Scoreboard;
using MediatR;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public class SubmissionCreatedRequest : IRequest
    {
        public Contest Contest { get; set; }

        public Submission Submission { get; set; }

        public int ContestId => Contest.ContestId;

        public int TeamId => Submission.Author;

        public int ProblemId => Submission.ProblemId;
    }
}

namespace JudgeWeb.Domains.Contests
{
    public static partial class ScoreboardMediatorExtensions
    {
        public static Task SubmissionCreated(this IMediator mediator, Contest contest, Submission submission)
        {
            return mediator.Send(new SubmissionCreatedRequest
            {
                Contest = contest,
                Submission = submission,
            });
        }
    }
}
