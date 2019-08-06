using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using System;

namespace JudgeWeb.Areas.Judge.Providers
{
    internal static class GlobalCache
    {
        static GlobalCache()
        {
            Instance = new MemoryCache(new MemoryCacheOptions()
            {
                Clock = new SystemClock()
            });
        }

        public static IMemoryCache Instance { get; }

        public static T Set<T>(this IMemoryCache cache, T value, TimeSpan expire)
        {
            return cache.Set(typeof(T), value, expire);
        }

        public static bool TryGet<T>(this IMemoryCache cache, out T value)
        {
            return cache.TryGetValue(typeof(T), out value);
        }

        public static void Remove<T>(this IMemoryCache cache)
        {
            cache.Remove(typeof(T));
        }
    }
}
