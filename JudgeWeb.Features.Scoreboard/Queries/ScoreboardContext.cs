using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public sealed class ScoreboardContext : IAsyncDisposable, IDisposable
    {
        public DbContext Context { get; }

        public DbSet<Team> Teams => Context.Set<Team>();

        public DbSet<ScoreCache> ScoreCache => Context.Set<ScoreCache>();

        public DbSet<RankCache> RankCache => Context.Set<RankCache>();

        public DbSet<TeamAffiliation> Affiliations => Context.Set<TeamAffiliation>();

        public DbSet<TeamCategory> Categories => Context.Set<TeamCategory>();

        public DbSet<Balloon> Balloon => Context.Set<Balloon>();

        public DbSet<Submission> Submissions => Context.Set<Submission>();

        public DbSet<Judging> Judgings => Context.Set<Judging>();

        public DbSet<ContestProblem> Problems => Context.Set<ContestProblem>();

        public DbSet<Testcase> Testcases => Context.Set<Testcase>();

        public Task<int> SaveChangesAsync() => Context.SaveChangesAsync();

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            return ((IAsyncDisposable)Context).DisposeAsync();
        }

        void IDisposable.Dispose()
        {
            ((IDisposable)Context).Dispose();
        }

        public ScoreboardContext(DbContext context)
        {
            Context = context;
            Context.ChangeTracker.AutoDetectChangesEnabled = false;
        }
    }
}
