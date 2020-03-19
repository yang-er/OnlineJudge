using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.OjUpdate
{
    public interface IDbContextHolder : IDisposable
    {
        DbContext Context { get; }

        public DbSet<SubmissionStatistics> Statistics => Context.Set<SubmissionStatistics>();

        public DbSet<PersonRank> PersonRanks => Context.Set<PersonRank>();

        public DbSet<ProblemArchive> Archives => Context.Set<ProblemArchive>();

        public DbSet<Configure> Configures => Context.Set<Configure>();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Context.SaveChangesAsync(cancellationToken);

        void IDisposable.Dispose() => Context.Dispose();
    }

    public class DbContextHolder<TContext> : IDbContextHolder
        where TContext : DbContext
    {
        public DbContextHolder(TContext context) => Context = context;

        public DbContext Context { get; }
    }
}
