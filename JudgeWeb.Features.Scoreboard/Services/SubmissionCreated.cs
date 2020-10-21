using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public class SubmissionCreatedHandler : IRequestHandler<SubmissionCreatedRequest>
    {
        DbContext Context { get; }

        public SubmissionCreatedHandler(DbContextAccessor db)
        {
            Context = db;
        }

        public async Task<Unit> Handle(SubmissionCreatedRequest request, CancellationToken cancellationToken)
        {
            var strategy = IRankingStrategy.SC[request.Contest.RankingStrategy];
            if (request.Submission.Time < (request.Contest.EndTime ?? DateTimeOffset.Now))
                await strategy.Pending(Context, request);
            return Unit.Value;
        }
    }
}
