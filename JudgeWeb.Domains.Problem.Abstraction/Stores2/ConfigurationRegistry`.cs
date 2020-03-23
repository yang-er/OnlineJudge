using JudgeWeb.Data;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IConfigurationRegistry
    {
        Task<List<Configure>> ListPublicAsync();

        Task<Configure> FindAsync([NotNull] string config);

        Task UpdateValueAsync([NotNull] string name, [NotNull] string newValue);

        Task<List<Configure>> GetAsync(string name = null);
    }
}
