using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

[assembly: Inject(typeof(IPrintingStore), typeof(PrintingStore))]
namespace JudgeWeb.Domains.Contests
{
    public class PrintingStore :
        IPrintingStore,
        ICrudRepositoryImpl<Printing>
    {
        public DbContext Context { get; }

        DbSet<Printing> Printings => Context.Set<Printing>();

        DbSet<TeamMember> Members => Context.Set<TeamMember>();

        public PrintingStore(DbContextAccessor context)
        {
            Context = context;
        }

        public Task<List<T>> ListAsync<T>(
            int takeCount, int page,
            Expression<Func<Printing, User, Team, T>> expression,
            Expression<Func<Printing, bool>>? predicate)
        {
            IQueryable<Printing> prints = Printings;
            if (predicate != null) prints = prints.Where(predicate);

            var query =
                from p in prints
                join u in Context.Set<User>() on p.UserId equals u.Id
                into uu from u in uu.DefaultIfEmpty()
                join tu in Members on new { p.ContestId, p.UserId } equals new { tu.ContestId, tu.UserId }
                into tuu from tu in tuu.DefaultIfEmpty()
                select new { p, u, t = tu.Team };

            if (page > 0)
            {
                query = query.OrderByDescending(a => a.p.Time);
            }
            else
            {
                query = query.OrderBy(a => a.p.Time);
                page = -page;
            }

            var selector = expression.Combine(
                objectTemplate: new { p = (Printing)null, u = (User)null, t = (Team)null },
                place1: a => a.p, place2: a => a.u, place3: a => a.t);
            return query.Select(selector)
                .Skip((page - 1) * takeCount).Take(takeCount)
                .ToListAsync();
        }

        public async Task<bool> SetStateAsync(int pid, bool? done)
        {
            int status = await Printings
                .Where(p => p.Id == pid)
                .BatchUpdateAsync(p => new Printing { Done = done });
            return status > 0;
        }
    }
}
