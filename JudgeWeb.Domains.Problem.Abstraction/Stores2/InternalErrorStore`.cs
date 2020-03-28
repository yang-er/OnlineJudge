using JudgeWeb.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IInternalErrorStore :
        ICrudRepository<InternalError>
    {
        Task<InternalError> FindAsync(int id);

        Task<InternalErrorDisable> ResolveAsync(InternalError error, InternalErrorStatus status);

        Task<int> GetJudgeStatusAsync();

        Task<List<InternalError>> ListAsync(int page = 1, int count = 50);
    }
}
