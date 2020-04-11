using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(IBalloonStore), typeof(BalloonStore))]
namespace JudgeWeb.Domains.Contests
{
    public partial class BalloonStore :
        IBalloonStore,
        ICrudRepositoryImpl<Balloon>
    {
        public DbContext Context { get; }

        public DbSet<Balloon> Balloons => Context.Set<Balloon>();

        public BalloonStore(DbContextAccessor context)
        {
            Context = context;
        }

        public async Task<List<Balloon>> ListAsync(int cid, ContestProblem[] problems)
        {
            var balloonQuery =
                from b in Balloons
                where b.s.ContestId == cid
                join t in Context.Set<Team>() on new { b.s.ContestId, TeamId = b.s.Author } equals new { t.ContestId, t.TeamId }
                select new Balloon(b, b.s.ProblemId, b.s.Author, t.TeamName, t.Location, b.s.Time, t.Category.Name, t.Category.SortOrder);

            var balloons = await balloonQuery.ToListAsync();
            balloons.Sort((b1, b2) => b1.Time.CompareTo(b2.Time));
            foreach (var g in balloons
                .OrderBy(b => b.Time)
                .GroupBy(b => new { b.ProblemId, b.SortOrder }))
            {
                var fb = true;
                foreach (var item in g)
                {
                    item.FirstToSolve = fb;
                    fb = false;
                    var p = problems.SingleOrDefault(p => p.ProblemId == item.ProblemId);
                    item.BalloonColor = p.Color;
                    item.ProblemShortName = p.ShortName;
                }
            }

            return balloons;
        }

        public Task SetDoneAsync(int cid, int id)
        {
            return Balloons
                .Where(b => b.Id == id && b.s.ContestId == cid)
                .BatchUpdateAsync(b => new Balloon { Done = true });
        }
    }
}
