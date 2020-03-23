using JudgeWeb.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface ILanguageStore :
        ICrudRepository<Language>,
        ICrudInstantUpdate<Language>
    {
        Task<Language> FindAsync(string langid);

        Task<List<Language>> ListAsync(bool? active = null);

        Task<List<Executable>> ListCompilersAsync();
    }
}
