using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestFacade :
        IPrintingStore,
        ICrudRepositoryImpl<Printing>
    {
        public IPrintingStore PrintingStore => this;

        DbSet<Printing> Printings => Context.Set<Printing>();

        Task<List<T>> IPrintingStore.ListAsync<T>(
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
                join t in Teams on new { tu.ContestId, tu.TeamId } equals new { t.ContestId, t.TeamId }
                into tt from t in tt.DefaultIfEmpty()
                select new { p, u, t };

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

        async Task<bool> IPrintingStore.SetStateAsync(int pid, bool? done)
        {
            int status = await Printings
                .Where(p => p.Id == pid)
                .BatchUpdateAsync(p => new Printing { Done = done });
            return status > 0;
        }
    }
}
