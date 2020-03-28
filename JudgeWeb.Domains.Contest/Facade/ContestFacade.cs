using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public class DbContestFacade<TContext> : ContestFacade
        where TContext : DbContext
    {
        public DbContestFacade(TContext context, IProblemFileRepository files)
            : base(context, files)
        {
        }
    }

    public partial class ContestFacade : IContestFacade
    {
        public DbContext Context { get; }

        public IMutableFileProvider Files { get; }

        protected ContestFacade(DbContext context, IMutableFileProvider files)
        {
            Context = context;
            Files = files;
        }

        public async Task<(int clarifications, int teams, int rejudgings)> GetJuryStatusAsync(int cid)
        {
            var clarifications = await Clarifications
                .Where(c => c.ContestId == cid && !c.Answered)
                .CachedCountAsync($"`c{cid}`clar`una_count", TimeSpan.FromSeconds(10));
            var teams = await Teams
                .Where(t => t.Status == 0 && t.ContestId == cid)
                .CachedCountAsync($"`c{cid}`teams`pending_count", TimeSpan.FromSeconds(10));
            var rejudgings = await Context.Set<Rejudge>()
                .Where(t => t.Applied == null && t.ContestId == cid)
                .CachedCountAsync($"`c{cid}`rejs`pending_count", TimeSpan.FromSeconds(10));
            return (clarifications, teams, rejudgings);
        }
    }
}
