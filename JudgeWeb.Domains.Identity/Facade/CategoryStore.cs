using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

[assembly: Inject(typeof(ICategoryStore), typeof(CategoryStore))]
namespace JudgeWeb.Domains.Identity
{
    public class CategoryStore :
        ICategoryStore,
        ICrudRepositoryImpl<TeamCategory>
    {
        public DbContext Context { get; }

        DbSet<TeamCategory> Categories => Context.Set<TeamCategory>();

        public CategoryStore(DbContext context)
        {
            Context = context;
        }

        public Task<TeamCategory> FindAsync(int affId)
        {
            return Categories
                .Where(a => a.CategoryId == affId)
                .SingleOrDefaultAsync();
        }

        public IQueryable<int> GetContestFilter(int cid)
        {
            return Context.Set<Team>()
                .Where(t => t.ContestId == cid)
                .Select(t => t.CategoryId)
                .Distinct();
        }

        public async Task<IEnumerable<TeamCategory>> ListAsync(
            Expression<Func<TeamCategory, bool>>? predicate = null)
        {
            IQueryable<TeamCategory> query = Categories;
            if (predicate != null) query = query.Where(predicate);
            return await query.ToListAsync();
        }
    }
}
