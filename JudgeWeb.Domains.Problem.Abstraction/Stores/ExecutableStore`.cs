using JudgeWeb.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IExecutableStore :
        ICrudRepository<Executable>
    {
        Task<Executable> FindAsync(string execid);

        Task<List<Executable>> ListAsync(string? type = null);

        Task<Dictionary<string, string>> ListMd5Async(params string[] targets);

        Task<ILookup<string, string>> ListUsageAsync(Executable executable);

        Task<List<ExecutableViewContentModel>> FetchContentAsync(Executable executable);
    }
}
