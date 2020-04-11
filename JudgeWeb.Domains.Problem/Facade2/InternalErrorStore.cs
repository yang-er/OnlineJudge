using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(IInternalErrorStore), typeof(InternalErrorStore))]
namespace JudgeWeb.Domains.Problems
{
    public class InternalErrorStore :
        IInternalErrorStore,
        ICrudRepositoryImpl<InternalError>
    {
        public DbContext Context { get; }

        DbSet<InternalError> InternalErrors => Context.Set<InternalError>();

        public InternalErrorStore(DbContextAccessor context)
        {
            Context = context;
        }

        public Task<InternalError> FindAsync(int id)
        {
            return InternalErrors.SingleOrDefaultAsync(e => e.ErrorId == id);
        }

        public async Task<InternalErrorDisable> ResolveAsync(
            InternalError error,
            InternalErrorStatus status)
        {
            if (error.Status != InternalErrorStatus.Open)
                throw new InvalidOperationException();
            await InternalErrors
                .Where(ie => ie.ErrorId == error.ErrorId)
                .BatchUpdateAsync(ie => new InternalError { Status = status });

            if (status != InternalErrorStatus.Resolved) return null;
            return error.Disabled.AsJson<InternalErrorDisable>();
        }

        public Task<List<InternalError>> ListAsync(int page, int count)
        {
            return InternalErrors
                .Select(
                    e => new InternalError
                    {
                        ErrorId = e.ErrorId,
                        Status = e.Status,
                        Time = e.Time,
                        Description = e.Description,
                    })
                .OrderByDescending(e => e.ErrorId)
                .Skip(count * (page - 1))
                .Take(count)
                .ToListAsync();
        }

        public Task<int> GetJudgeStatusAsync()
        {
            return InternalErrors
                .Where(ie => ie.Status == InternalErrorStatus.Open)
                .CountAsync();
        }
    }
}
