using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Problems;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

[assembly: Inject(typeof(IProblemsetStore), typeof(ProblemsetStore))]
namespace JudgeWeb.Domains.Contests
{
    public class ProblemsetStore : IProblemsetStore
    {
        public DbContext Context { get; }

        public IProblemStore Parent { get; }

        DbSet<ContestProblem> ContestProblems => Context.Set<ContestProblem>();

        public ProblemsetStore(DbContext context, IProblemStore problem)
        {
            Context = context;
            Parent = problem;
        }

        public Task<ContestProblem[]> ListAsync(int cid)
        {
            return Context.CachedGetAsync($"`c{cid}`probs", TimeSpan.FromMinutes(5), async () =>
            {
                var result = await ContestProblems
                    .Where(cp => cp.ContestId == cid)
                    .Select(cp => new ContestProblem(cp, cp.p.Title, cp.p.TimeLimit, cp.p.MemoryLimit, cp.p.CombinedRunCompare, cp.p.Shared, cp.p.AllowJudge))
                    .ToArrayAsync();

                Array.Sort(result, (a, b) => a.ShortName.CompareTo(b.ShortName));
                for (int i = 0; i < result.Length; i++)
                    result[i].Rank = i + 1;

                var query2 =
                    from cp in ContestProblems
                    where cp.ContestId == cid
                    join t in Context.Set<Testcase>() on cp.ProblemId equals t.ProblemId
                    group t by cp.ProblemId into g
                    select new { g.Key, Count = g.Count(), Score = g.Sum(t => t.Point) };

                var result2 = await query2.ToDictionaryAsync(k => k.Key);

                foreach (var item in result)
                {
                    var res = result2.GetValueOrDefault(item.ProblemId) ?? new { Key = 0, Count = 0, Score = 0 };
                    item.TestcaseCount = res.Count;
                    if (item.Score == 0) item.Score = res.Score;
                }

                return result;
            });
        }

        public async Task<IEnumerable<ProblemStatement>> StatementsAsync(int cid)
        {
            var probs = await ContestProblems
                .Where(cp => cp.ContestId == cid)
                .Include(cp => cp.p)
                .OrderBy(cp => cp.ShortName)
                .ToListAsync();

            var lst = new List<ProblemStatement>(probs.Count);
            for (int i = 0; i < probs.Count; i++)
            {
                var stmt = await Parent.StatementAsync(probs[i].p);
                stmt.ShortName = probs[i].ShortName;
                lst.Add(stmt);
            }

            return lst;
        }

        public async Task<(bool ok, string msg)> CheckAvailabilityAsync(
            int cid, int pid, ClaimsPrincipal user)
        {
            var list = await ListAsync(cid);
            if (list.Any(p => p.ProblemId == pid))
                return (false, "Problem has been added.");
            var prob = await Parent.FindAsync(pid);
            if (prob == null)
                return (false, "Problem not found.");
            if (!user.IsInRole("Administrator") && !user.IsInRole($"AuthorOfProblem{pid}"))
                return (false, "Access denined.");
            return (true, prob.Title);
        }

        public async Task CreateAsync(ContestProblem problem)
        {
            ContestProblems.Add(problem);
            await Context.SaveChangesAsync();
            Context.RemoveCacheEntry($"`c{problem.ContestId}`probs");
        }

        public async Task DeleteAsync(ContestProblem problem)
        {
            var (cid, pid) = (problem.ContestId, problem.ProblemId);
            await ContestProblems
                .Where(cp => cp.ContestId == cid && cp.ProblemId == pid)
                .BatchDeleteAsync();
            Context.RemoveCacheEntry($"`c{problem.ContestId}`probs");
        }

        public async Task UpdateAsync(int cid, int pid, Expression<Func<ContestProblem>> change)
        {
            var change2 = Expression.Lambda<Func<ContestProblem, ContestProblem>>(
                change.Body, Expression.Parameter(typeof(ContestProblem), "oldcp"));
            await ContestProblems
                .Where(oldcp => oldcp.ContestId == cid && oldcp.ProblemId == pid)
                .BatchUpdateAsync(change2);
            Context.RemoveCacheEntry($"`c{cid}`probs");
        }
    }
}
