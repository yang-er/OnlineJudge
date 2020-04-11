using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(INewsStore), typeof(NewsStore))]
namespace JudgeWeb.Domains.Identity
{
    public class NewsStore :
        INewsStore,
        ICrudRepositoryImpl<News>
    {
        public DbContext Context { get; }

        public NewsStore(DbContextAccessor context) => Context = context;

        public DbSet<News> News => Context.Set<News>();

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
    }
}
