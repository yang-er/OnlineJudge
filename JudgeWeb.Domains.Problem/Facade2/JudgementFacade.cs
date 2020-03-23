using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public class DbContextJudgementFacade<TContext> :
        JudgementFacade
        where TContext : DbContext
    {
        public DbContextJudgementFacade(
            TContext context,
            IRunFileRepository fileProvider,
            ILogger<IJudgementFacade> logger) :
            base(context, fileProvider, logger)
        {
        }
    }

    public partial class JudgementFacade : IJudgementFacade
    {
        public DbContext Context { get; }

        public ILogger<IJudgementFacade> Logger { get; }

        public IMutableFileProvider Files { get; }

        public JudgementFacade(
            DbContext context,
            IMutableFileProvider fileProvider,
            ILogger<IJudgementFacade> logger)
        {
            Context = context;
            Logger = logger;
            Files = fileProvider;
        }

        async Task<List<ServerStatus>> IJudgementFacade.GetJudgeQueueAsync(int? cid)
        {
            IQueryable<Submission> submissions = Context.Set<Submission>();
            if (cid != null) submissions = submissions.Where(s => s.ContestId == cid);

            var judgingStatus = await Queryable
                .Join(
                    outer: Context.Set<Judging>(),
                    inner: Context.Set<Submission>(),
                    outerKeySelector: j => j.SubmissionId,
                    innerKeySelector: s => s.SubmissionId,
                    resultSelector: (j, s) => new { j.Status, s.ContestId })
                .GroupBy(g => new { g.Status, g.ContestId })
                .Select(g => new { g.Key.Status, g.Key.ContestId, Count = g.Count() })
                .ToListAsync();

            return judgingStatus
                .GroupBy(a => a.ContestId)
                .Select(g => new ServerStatus
                {
                    cid = g.Key,
                    num_submissions = g.Sum(a => a.Count),
                    num_queued = g.SingleOrDefault(a => a.Status == Verdict.Pending)?.Count ?? 0,
                    num_judging = g.SingleOrDefault(a => a.Status == Verdict.Running)?.Count ?? 0,
                })
                .ToList();
        }

        async Task<(int, int)> IJudgementFacade.GetJudgeStatusAsync()
        {
            var judgehosts = await Judgehosts
                .Where(jh => jh.PollTime < DateTimeOffset.Now.AddSeconds(-120) && jh.Active)
                .CountAsync();
            var internal_error = await InternalErrors
                .Where(ie => ie.Status == InternalErrorStatus.Open)
                .CountAsync();
            return (judgehosts, internal_error);
        }
    }
}
