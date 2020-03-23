using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface ICrudInstantUpdate<TEntity>
        where TEntity : class, new()
    {
        Task<int> UpdateAsync(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TEntity>> update);
    }
}
