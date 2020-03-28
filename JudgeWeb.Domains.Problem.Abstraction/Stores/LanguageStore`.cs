using JudgeWeb.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface ILanguageStore :
        ICrudRepository<Language>
    {
        Task<Language> FindAsync(string langid);

        Task ToggleJudgeAsync(string langid, bool tobe);

        Task ToggleSubmitAsync(string langid, bool tobe);

        Task<List<Language>> ListAsync(bool? active = null);
    }
}
