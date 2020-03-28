using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public class LanguageStore :
        ILanguageStore,
        ICrudRepositoryImpl<Language>
    {
        public DbContext Context { get; }

        public DbSet<Language> Languages => Context.Set<Language>();

        public LanguageStore(DbContext context)
        {
            Context = context;
        }

        public Task ToggleJudgeAsync(string langid, bool tobe)
        {
            return Languages
                .Where(l => l.Id == langid)
                .BatchUpdateAsync(l => new Language { AllowJudge = tobe });
        }

        public Task<Language> FindAsync(string langid)
        {
            return Languages.SingleOrDefaultAsync(l => l.Id == langid);
        }

        public Task<List<Language>> ListAsync(bool? active)
        {
            IQueryable<Language> langs = Languages;
            if (active != null)
                langs = langs.Where(l => l.AllowSubmit == active.Value);
            return langs.ToListAsync();
            // return langs.CachedToListAsync(
            //     tag: $"ILanguageStore.ListAsync({active})",
            //     timeSpan: TimeSpan.FromMinutes(5));
        }

        public Task ToggleSubmitAsync(string langid, bool tobe)
        {
            return Languages
                .Where(l => l.Id == langid)
                .BatchUpdateAsync(l => new Language { AllowSubmit = tobe });
        }
    }
}
