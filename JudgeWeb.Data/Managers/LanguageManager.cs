using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Data
{
    public class LanguageManager
    {
        protected AppDbContext DbContext { get; }

        public LanguageManager(AppDbContext adbc)
        {
            DbContext = adbc;
        }

        private void Expire()
        {
            GlobalCache.Instance.Remove<Dictionary<int, Language>>();
        }

        public async Task ToggleAsync(string extid, Action<Language> act)
        {
            var cur = await DbContext.Languages
                .Where(l => l.ExternalId == extid)
                .FirstOrDefaultAsync();

            if (cur != null)
            {
                act.Invoke(cur);
                DbContext.Languages.Update(cur);
            }

            await DbContext.SaveChangesAsync();
            Expire();
        }

        public async Task<IEnumerable<(int, int)>> StatisticsAsync()
        {
            if (GlobalCache.Instance.TryGetValue("lang::stat", out var qwq))
                return (IEnumerable<(int, int)>) qwq;
            var ans = await DbContext.Submissions
                .GroupBy(s => s.Language)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();
            var value = ans.Select(a => (a.Key, a.Count));
            GlobalCache.Instance.Set("lang::stat", value, TimeSpan.FromMinutes(30));
            return value;
        }

        public async Task<Dictionary<int, Language>> GetAllAsync()
        {
            if (GlobalCache.Instance.TryGet<Dictionary<int, Language>>(out var value))
                return value;
            value = await DbContext.Languages.ToDictionaryAsync(l => l.LangId);
            GlobalCache.Instance.Set(value, TimeSpan.FromMinutes(30));
            return value;
        }

        public Dictionary<int, Language> GetAll()
        {
            if (GlobalCache.Instance.TryGet<Dictionary<int, Language>>(out var value))
                return value;
            value = DbContext.Languages.ToDictionary(l => l.LangId);
            GlobalCache.Instance.Set(value, TimeSpan.FromMinutes(30));
            return value;
        }

        public Language Get(int langid)
        {
            return GetAll().GetValueOrDefault(langid);
        }
    }
}
