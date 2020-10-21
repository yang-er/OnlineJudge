using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public class RefreshScoreboardCacheHandler : IRequestHandler<RefreshScoreboardCacheRequest>
    {
        DbContext Context { get; }

        public RefreshScoreboardCacheHandler(DbContextAccessor db)
        {
            Context = db;
        }

        public async Task<Unit> Handle(RefreshScoreboardCacheRequest request, CancellationToken cancellationToken)
        {
            request.Deadline = request.Deadline < request.EndTime ? request.Deadline : request.EndTime;
            var strategy = IRankingStrategy.SC[request.Contest.RankingStrategy];
            await strategy.RefreshCache(Context, request);
            return Unit.Value;
        }
    }
}
