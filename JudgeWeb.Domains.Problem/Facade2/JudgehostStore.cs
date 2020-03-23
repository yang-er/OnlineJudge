using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementFacade :
        IJudgehostStore,
        ICrudRepositoryImpl<JudgeHost>,
        ICrudInstantUpdateImpl<JudgeHost>
    {
        public IJudgehostStore JudgehostStore => this;

        public DbSet<JudgeHost> Judgehosts => Context.Set<JudgeHost>();

        Task<int> IJudgehostStore.ToggleAsync(string hostname, bool active)
        {
            IQueryable<JudgeHost> src = Judgehosts;
            if (hostname != null) src = src.Where(h => h.ServerName == hostname);
            return src.BatchUpdateAsync(h => new JudgeHost { Active = active });
        }

        Task<List<JudgeHost>> IJudgehostStore.ListAsync()
        {
            return Judgehosts.ToListAsync();
        }

        Task<JudgeHost> IJudgehostStore.FindAsync(string name)
        {
            return Judgehosts.SingleOrDefaultAsync(h => h.ServerName == name);
        }

        Task<int> IJudgehostStore.CountJudgingsAsync(string hostname)
        {
            return Context.Set<Judging>().Where(g => g.Server == hostname).CountAsync();
        }

        Task<List<Judging>> IJudgehostStore.FetchJudgingsAsync(string hostname, int count)
        {
            return Context.Set<Judging>()
                .Where(j => j.Server == hostname)
                .OrderByDescending(g => g.JudgingId)
                .Take(count)
                .ToListAsync();
        }

        Task IJudgehostStore.NotifyPollAsync(JudgeHost host)
        {
            host.PollTime = DateTimeOffset.Now;
            Judgehosts.Update(host);
            return Context.SaveChangesAsync();
            //throw new System.NotImplementedException();
        }
    }
}
