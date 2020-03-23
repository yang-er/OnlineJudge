using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public partial class ProblemFacade :
        ILanguageStore,
        ICrudRepositoryImpl<Language>,
        ICrudInstantUpdateImpl<Language>
    {
        public ILanguageStore LanguageStore => this;
        public DbSet<Language> Languages { get; }

        Task<Language> ILanguageStore.FindAsync(string langid)
        {
            return Languages.SingleOrDefaultAsync(l => l.Id == langid);
        }

        Task<List<Language>> ILanguageStore.ListAsync(bool? active)
        {
            IQueryable<Language> langs = Languages;
            if (active != null)
                langs = langs.Where(l => l.AllowSubmit == active.Value);
            return langs.ToListAsync();
            // return langs.CachedToListAsync(
            //     tag: $"ILanguageStore.ListAsync({active})",
            //     timeSpan: TimeSpan.FromMinutes(5));
        }

        Task<List<Executable>> ILanguageStore.ListCompilersAsync()
        {
            return ExecutableStore.ListAsync("compile");
        }
    }
}
