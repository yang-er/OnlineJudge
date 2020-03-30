using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

[assembly: Inject(typeof(IClarificationStore), typeof(ClarificationStore))]
namespace JudgeWeb.Domains.Contests
{
    public class ClarificationStore :
        IClarificationStore,
        ICrudRepositoryImpl<Clarification>
    {
        public DbContext Context { get; }

        DbSet<Clarification> Clarifications => Context.Set<Clarification>();

        public ClarificationStore(DbContext context)
        {
            Context = context;
        }

        public async Task<int> SendAsync(Clarification clar, Clarification replyTo)
        {
            var cl = Clarifications.Add(clar);

            if (replyTo != null)
            {
                replyTo.Answered = true;
                Clarifications.Update(replyTo);
            }

            await Context.SaveChangesAsync();
            return cl.Entity.ClarificationId;
        }

        public Task<Clarification> FindAsync(int cid, int id)
        {
            return Clarifications.SingleOrDefaultAsync(c => c.ContestId == cid && c.ClarificationId == id);
        }

        public Task<List<Clarification>> ListAsync(int cid,
            Expression<Func<Clarification, bool>>? predicate)
        {
            var query = Clarifications.Where(c => c.ContestId == cid);
            if (predicate != null) query = query.Where(predicate);
            return query.ToListAsync();
        }

        public Task<int> SetAnsweredAsync(int cid, int clarId, bool answered)
        {
            return Clarifications
                .Where(c => c.ContestId == cid && c.ClarificationId == clarId)
                .BatchUpdateAsync(c => new Clarification { Answered = answered });
        }

        public Task<int> ClaimAsync(int cid, int clarId, string jury, bool claim)
        {
            var (from, to) = claim ? (default(string), jury) : (jury, default(string));
            return Clarifications
                .Where(c => c.ContestId == cid && c.ClarificationId == clarId)
                .Where(c => c.JuryMember == from)
                .BatchUpdateAsync(c => new Clarification { JuryMember = to });
        }

        public Task<int> GetJuryStatusAsync(int cid)
        {
            return Clarifications
                .Where(c => c.ContestId == cid && !c.Answered)
                .CachedCountAsync($"`c{cid}`clar`una_count", TimeSpan.FromSeconds(10));
        }
    }
}
