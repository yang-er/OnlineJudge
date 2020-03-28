using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

[assembly: Inject(typeof(IAffiliationStore), typeof(AffiliationStore))]
namespace JudgeWeb.Domains.Identity
{
    public class AffiliationStore :
        IAffiliationStore,
        ICrudRepositoryImpl<TeamAffiliation>
    {
        public DbContext Context { get; }

        DbSet<TeamAffiliation> Affiliations => Context.Set<TeamAffiliation>();

        public AffiliationStore(DbContext context)
        {
            Context = context;
        }

        public Task<TeamAffiliation> FindAsync(int affId)
        {
            return Affiliations
                .Where(a => a.AffiliationId == affId)
                .SingleOrDefaultAsync();
        }

        public IQueryable<int> GetContestFilter(int cid)
        {
            return Context.Set<Team>()
                .Where(t => t.ContestId == cid)
                .Select(t => t.AffiliationId)
                .Distinct();
        }

        public async Task<IEnumerable<TeamAffiliation>> ListAsync(
            Expression<Func<TeamAffiliation, bool>>? predicate = null)
        {
            IQueryable<TeamAffiliation> query = Affiliations;
            if (predicate != null) query = query.Where(predicate);
            return await query.ToListAsync();
        }

        public Task<TeamAffiliation> FindAsync(string externalId)
        {
            return Affiliations
                .Where(a => a.ExternalId == externalId)
                .SingleOrDefaultAsync();
        }
    }
}
