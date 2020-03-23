using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementFacade :
        IInternalErrorStore,
        ICrudRepositoryImpl<InternalError>
    {
        public IInternalErrorStore InternalErrorStore => this;

        public DbSet<InternalError> InternalErrors => Context.Set<InternalError>();

        Task<InternalError> IInternalErrorStore.FindAsync(int id)
        {
            return InternalErrors.SingleOrDefaultAsync(e => e.ErrorId == id);
        }

        async Task<InternalErrorDisable> IInternalErrorStore.ResolveAsync(InternalError error, InternalErrorStatus status)
        {
            if (error.Status != InternalErrorStatus.Open)
                throw new InvalidOperationException();
            await InternalErrors
                .Where(ie => ie.ErrorId == error.ErrorId)
                .BatchUpdateAsync(ie => new InternalError { Status = status });

            if (status != InternalErrorStatus.Resolved) return null;
            return error.Disabled.AsJson<InternalErrorDisable>();
        }

        Task<List<InternalError>> IInternalErrorStore.ListAsync()
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
                .Take(50)
                .ToListAsync();
        }
    }
}
