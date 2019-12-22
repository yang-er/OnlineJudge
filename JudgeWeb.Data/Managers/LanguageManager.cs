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
