using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<挂起>")]
    public abstract class DbContextAccessor : IDisposable, IAsyncDisposable
    {
        public abstract DbContext Context { get; }

        void IDisposable.Dispose()
        {
            ((IDisposable)Context).Dispose();
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            return ((IAsyncDisposable)Context).DisposeAsync();
        }

        public DbSet<T> Set<T>() where T : class
        {
            return Context.Set<T>();
        }

        public Task SaveChangesAsync(CancellationToken token = default)
        {
            return Context.SaveChangesAsync(token);
        }

        public static implicit operator DbContext(DbContextAccessor accessor)
        {
            return accessor.Context;
        }
    }

    public class DbContextAccessor<TContext> :
        DbContextAccessor
        where TContext : DbContext
    {
        public DbContextAccessor(TContext context) => Context = context;

        public override DbContext Context { get; }
    }
}
