using JudgeWeb.Domains.Contests;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public class JudgingFinishedHandler : IRequestHandler<JudgingFinishedRequest>
    {
        DbContext Context { get; }

        IContestStore Contests { get; }

        IProblemsetStore Problems { get; }

        public JudgingFinishedHandler(
            DbContextAccessor db,
            IContestStore contests,
            IProblemsetStore probs)
        {
            Context = db;
            Contests = contests;
            Problems = probs;
        }

        public async Task<Unit> Handle(JudgingFinishedRequest request, CancellationToken cancellationToken)
        {
            // 此处两个请求大概率命中缓存，对性能影响不严重
            request.Contest = await Contests.FindAsync(request.ContestId);
            if (request.Contest.RankingStrategy == 1)
            {
                var probs = await Problems.ListAsync(request.ContestId);
                var prob = probs.SingleOrDefault(cp => cp.ProblemId == request.ProblemId);
                if (prob == null) return Unit.Value;
                request.CfScore = prob.Score;
            }

            var strategy = IRankingStrategy.SC[request.Contest.RankingStrategy];
            if (request.SubmitTime < (request.Contest.EndTime ?? DateTimeOffset.Now))
                await (request.Judging.Status switch
                {
                    Verdict.Accepted => strategy.Accept(Context, request),
                    Verdict.CompileError => strategy.CompileError(Context, request),
                    _ => strategy.Reject(Context, request)
                });

            return Unit.Value;
        }
    }
}
