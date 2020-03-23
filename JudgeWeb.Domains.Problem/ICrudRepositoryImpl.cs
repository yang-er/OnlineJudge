using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IDbContextRepository
    {
        DbContext Context { get; }
    }

    public interface ICreateRepositoryImpl<TEntity> :
        ICreateRepository<TEntity>,
        IDbContextRepository
        where TEntity : class, new()
    {
        async Task<TEntity> ICreateRepository<TEntity>.CreateAsync(TEntity entity)
        {
            Context.Set<TEntity>().Add(entity);
            await Context.SaveChangesAsync();
            return entity;
        }
    }

    public interface IUpdateRepositoryImpl<TEntity> :
        IUpdateRepository<TEntity>,
        IDbContextRepository
        where TEntity : class, new()
    {
        Task IUpdateRepository<TEntity>.UpdateAsync(TEntity entity)
        {
            Context.Set<TEntity>().Update(entity);
            return Context.SaveChangesAsync();
        }
    }

    public interface IDeleteRepositoryImpl<TEntity> :
        IDeleteRepository<TEntity>,
        IDbContextRepository
        where TEntity : class, new()
    {
        Task IDeleteRepository<TEntity>.DeleteAsync(TEntity entity)
        {
            Context.Set<TEntity>().Update(entity);
            return Context.SaveChangesAsync();
        }
    }

    public interface ICrudRepositoryImpl<TEntity> :
        ICrudRepository<TEntity>,
        IDeleteRepositoryImpl<TEntity>,
        IUpdateRepositoryImpl<TEntity>,
        ICreateRepositoryImpl<TEntity>,
        IDbContextRepository
        where TEntity : class, new()
    {
    }

    public interface ICrudInstantUpdateImpl<TEntity> :
        ICrudInstantUpdate<TEntity>,
        IDbContextRepository
        where TEntity : class, new()
    {
        Task<int> ICrudInstantUpdate<TEntity>.UpdateAsync(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TEntity>> update)
        {
            return Context.Set<TEntity>()
                .Where(predicate)
                .BatchUpdateAsync(update);
        }
    }
}
