using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Problems;
using JudgeWeb.Features.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Inject(typeof(IContestFacade), typeof(ContestFacade))]
namespace JudgeWeb.Domains.Contests
{
    public class ContestFacade : IContestFacade
    {
        public DbContext Context { get; }

        public IContestStore Contests { get; }

        public IProblemsetStore Problemset { get; }

        public ITeamStore Teams { get; }

        public ISubmissionStore Submissions { get; }

        public ContestFacade(
            DbContextAccessor context,
            IContestStore store1,
            IProblemsetStore store2,
            ITeamStore store3,
            ISubmissionStore store4)
        {
            Context = context;
            Contests = store1;
            Problemset = store2;
            Teams = store3;
            Submissions = store4;
        }

        public Task<Dictionary<string, Language>> ListLanguageAsync(int cid)
        {
            return Context.Set<Language>().CachedToDictionaryAsync(
                keySelector: k => k.Id,
                tag: $"`c{cid}`langs",
                timeSpan: TimeSpan.FromMinutes(10));
        }

        public Task<Dictionary<int, int>> StatisticsTeamAsync()
        {
            return Context.Set<Team>()
                .Where(t => t.Status == 1)
                .GroupBy(t => t.ContestId)
                .Select(g => new { ContestId = g.Key, TeamCount = g.Count() })
                .ToDictionaryAsync(k => k.ContestId, v => v.TeamCount);
        }

        public Task<Dictionary<int, int>> StatisticsProblemAsync()
        {
            return Context.Set<ContestProblem>()
                .GroupBy(t => t.ContestId)
                .Select(g => new { ContestId = g.Key, TeamCount = g.Count() })
                .ToDictionaryAsync(k => k.ContestId, v => v.TeamCount);
        }
    }
}
