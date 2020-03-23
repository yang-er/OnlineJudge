using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface ICrudRepositoryImpl<TEntity> :
        ICrudRepository<TEntity>
        where TEntity : class, new()
    {
        DbContext Context { get; }

        Task IUpdateRepository<TEntity>.UpdateAsync(TEntity entity)
        {
            Context.Set<TEntity>().Update(entity);
            return Context.SaveChangesAsync();
        }

        Task IDeleteRepository<TEntity>.DeleteAsync(TEntity entity)
        {
            Context.Set<TEntity>().Remove(entity);
            return Context.SaveChangesAsync();
        }

        async Task<TEntity> ICreateRepository<TEntity>.CreateAsync(TEntity entity)
        {
            Context.Set<TEntity>().Add(entity);
            await Context.SaveChangesAsync();
            return entity;
        }
    }
}
