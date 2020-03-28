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
        IClarificationStore,
        ICrudRepositoryImpl<Clarification>
    {
        public IClarificationStore ClarificationStore => this;

        DbSet<Clarification> Clarifications => Context.Set<Clarification>();

        async Task<int> IClarificationStore.SendAsync(Clarification clar, Clarification replyTo)
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

        Task<Clarification> IClarificationStore.FindAsync(int cid, int id)
        {
            return Clarifications.SingleOrDefaultAsync(c => c.ContestId == cid && c.ClarificationId == id);
        }

        Task<List<Clarification>> IClarificationStore.ListAsync(int cid,
            Expression<Func<Clarification, bool>>? predicate)
        {
            var query = Clarifications.Where(c => c.ContestId == cid);
            if (predicate != null) query = query.Where(predicate);
            return query.ToListAsync();
        }

        Task<int> IClarificationStore.SetAnsweredAsync(int cid, int clarId, bool answered)
        {
            return Clarifications
                .Where(c => c.ContestId == cid && c.ClarificationId == clarId)
                .BatchUpdateAsync(c => new Clarification { Answered = answered });
        }

        Task<int> IClarificationStore.ClaimAsync(int cid, int clarId, string jury, bool claim)
        {
            var (from, to) = claim ? (default(string), jury) : (jury, default(string));
            return Clarifications
                .Where(c => c.ContestId == cid && c.ClarificationId == clarId)
                .Where(c => c.JuryMember == from)
                .BatchUpdateAsync(c => new Clarification { JuryMember = to });
        }
    }
}
