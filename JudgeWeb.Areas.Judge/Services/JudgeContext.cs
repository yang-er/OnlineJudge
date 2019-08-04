using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;

[assembly: Inject(typeof(JudgeContext))]
namespace JudgeWeb.Areas.Judge.Services
{
    public partial class JudgeContext
    {
        private AppDbContext DbContext { get; }

        public int ItemsPerPage { get; set; } = 15;

        private static readonly IMemoryCache __cache = new MemoryCache(new MemoryCacheOptions()
        {
            Clock = new SystemClock()
        });

        public JudgeContext(AppDbContext adbc)
        {
            DbContext = adbc;
        }

        public void NotifyLanguagesExpired()
        {
            __cache.Remove("judge.langs");
        }

        public Dictionary<int, Language> GetLanguages()
        {
            if (__cache.TryGetValue<Dictionary<int, Language>>("judge.langs", out var value))
                return value;
            value = DbContext.Languages.ToDictionary(l => l.LangId);
            __cache.Set("judge.langs", value, TimeSpan.FromMinutes(30));
            return value;
        }

    }
}
