using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Data
{
    static class ContestCache
    {
        public static readonly MemoryCache _cache
            = new MemoryCache(new MemoryCacheOptions { Clock = new SystemClock() });

        public static readonly AsyncLock _locker = new AsyncLock();

        public static Task<int> CachedCountAsync<T>(
            this IQueryable<T> query, string tag, TimeSpan timeSpan)
        {
            return _cache.GetOrCreateAsync(tag, async entry =>
            {
                var result = await query.CountAsync();
                entry.AbsoluteExpirationRelativeToNow = timeSpan;
                return result;
            });
        }

        public static Task<T> CachedSingleOrDefaultAsync<T>(
            this IQueryable<T> query, string tag, TimeSpan timeSpan)
        {
            return _cache.GetOrCreateAsync(tag, async entry =>
            {
                var result = await query.SingleOrDefaultAsync();
                entry.AbsoluteExpirationRelativeToNow = timeSpan;
                return result;
            });
        }

        public static Task<List<T>> CachedToListAsync<T>(
            this IQueryable<T> query, string tag, TimeSpan timeSpan)
        {
            return _cache.GetOrCreateAsync(tag, async entry =>
            {
                var result = await query.ToListAsync();
                entry.AbsoluteExpirationRelativeToNow = timeSpan;
                return result;
            });
        }

        public static Task<Dictionary<T2, T>> CachedToDictionaryAsync<T, T2>(
            this IQueryable<T> query,
            Func<T, T2> keySelector,
            string tag, TimeSpan timeSpan)
        {
            return _cache.GetOrCreateAsync(tag, async entry =>
            {
                var result = await query.ToDictionaryAsync(keySelector);
                entry.AbsoluteExpirationRelativeToNow = timeSpan;
                return result;
            });
        }

        public static Task<Dictionary<T2, T3>> CachedToDictionaryAsync<T, T2, T3>(
            this IQueryable<T> query,
            Func<T, T2> keySelector,
            Func<T, T3> valueSelector,
            string tag, TimeSpan timeSpan)
        {
            return _cache.GetOrCreateAsync(tag, async entry =>
            {
                var result = await query.ToDictionaryAsync(keySelector, valueSelector);
                entry.AbsoluteExpirationRelativeToNow = timeSpan;
                return result;
            });
        }


        public static ContestProblem Find(this ContestProblem[] cps, int pid) => cps.FirstOrDefault(cp => cp.ProblemId == pid);

        public static ContestProblem Find(this ContestProblem[] cps, string shortname) => cps.FirstOrDefault(cp => cp.ShortName == shortname);

        public static void AddCreate(this DbSet<Event> db, int cid, Api.ContestEventEntity cee)
            => db.Add(cee.ToEvent("create", cid));

        public static void AddCreate(this DbSet<Event> db, int cid, IEnumerable<Api.ContestEventEntity> cees)
            => db.AddRange(cees.Select(cee => cee.ToEvent("create", cid)));

        public static void AddUpdate(this DbSet<Event> db, int cid, Api.ContestEventEntity cee)
            => db.Add(cee.ToEvent("update", cid));

        public static void AddDelete(this DbSet<Event> db, int cid, Api.ContestEventEntity cee)
            => db.Add(cee.ToEvent("delete", cid));
    }
}
