using System.Linq;

namespace EntityFrameworkCore.Cacheable
{
    public static class Ext
    {
        public static IQueryable<T> Cacheable<T>(this IQueryable<T> q, params object[] p)
        {
            return q;
        }
    }
}
