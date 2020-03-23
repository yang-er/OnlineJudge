using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IInternalErrorStore :
        ICrudRepository<InternalError>
    {
        Task<InternalError> FindAsync(int id);

        Task<InternalErrorDisable> ResolveAsync(InternalError error, InternalErrorStatus status);

        Task<List<InternalError>> ListAsync();
    }
}
