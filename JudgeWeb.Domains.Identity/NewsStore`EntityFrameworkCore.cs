using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public class EntityFrameworkCoreNewsStore<TContext> : EntityFrameworkCoreNewsStore
        where TContext : DbContext
    {
        public EntityFrameworkCoreNewsStore(TContext context)
            : base(context)
        {
        }
    }

    public class EntityFrameworkCoreNewsStore : INewsStore
    {
        public DbContext Context { get; }
        protected EntityFrameworkCoreNewsStore(DbContext context) => Context = context;

        public DbSet<News> News => Context.Set<News>();

        public async Task<News> CreateAsync(News news)
        {
            News.Add(news);
            await Context.SaveChangesAsync();
            return news;
        }

        public Task DeleteAsync(News news)
        {
            News.Remove(news);
            return Context.SaveChangesAsync();
        }

        public Task<News> FindAsync(int newid)
        {
            return News.Where(n => n.NewsId == newid).SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<(int id, string title)>> ListActiveAsync(int count)
        {
            var r = await News.Where(n => n.Active)
                .OrderByDescending(n => n.NewsId)
                .Select(n => new { n.Title, n.NewsId })
                .Take(count)
                .ToListAsync();
            return r.Select(a => (a.NewsId, a.Title));
        }

        public Task<List<News>> ListAsync()
        {
            return News.Select(n => new News
            {
                NewsId = n.NewsId,
                Active = n.Active,
                Title = n.Title,
                LastUpdate = n.LastUpdate,
            })
            .ToListAsync();
        }

        public Task UpdateAsync(News news)
        {
            News.Update(news);
            return Context.SaveChangesAsync();
        }
    }
}
