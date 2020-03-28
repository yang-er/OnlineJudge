using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(IJudgehostStore), typeof(JudgehostStore))]
namespace JudgeWeb.Domains.Problems
{
    public class JudgehostStore :
        IJudgehostStore,
        ICrudRepositoryImpl<JudgeHost>
    {
        public DbContext Context { get; }

        public DbSet<JudgeHost> Judgehosts => Context.Set<JudgeHost>();

        public JudgehostStore(DbContext context)
        {
            Context = context;
        }

        public Task<int> ToggleAsync(string hostname, bool active)
        {
            IQueryable<JudgeHost> src = Judgehosts;
            if (hostname != null) src = src.Where(h => h.ServerName == hostname);
            return src.BatchUpdateAsync(h => new JudgeHost { Active = active });
        }

        public Task<List<JudgeHost>> ListAsync()
        {
            return Judgehosts.ToListAsync();
        }

        public Task<JudgeHost> FindAsync(string name)
        {
            return Judgehosts.SingleOrDefaultAsync(h => h.ServerName == name);
        }

        public Task<int> CountJudgingsAsync(string hostname)
        {
            return Context.Set<Judging>().Where(g => g.Server == hostname).CountAsync();
        }

        public Task<List<Judging>> FetchJudgingsAsync(string hostname, int count)
        {
            return Context.Set<Judging>()
                .Where(j => j.Server == hostname)
                .OrderByDescending(g => g.JudgingId)
                .Take(count)
                .ToListAsync();
        }

        public Task NotifyPollAsync(JudgeHost host)
        {
            host.PollTime = DateTimeOffset.Now;
            Judgehosts.Update(host);
            return Context.SaveChangesAsync();
        }

        public Task<int> GetJudgeStatusAsync()
        {
            return Judgehosts
                .Where(jh => jh.PollTime < DateTimeOffset.Now.AddSeconds(-120) && jh.Active)
                .CountAsync();
        }
    }
}
