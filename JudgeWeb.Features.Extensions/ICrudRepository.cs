using System.Threading.Tasks;

namespace JudgeWeb.Domains
{
    public interface ICrudRepository<TEntity> :
        ICreateRepository<TEntity>,
        IUpdateRepository<TEntity>,
        IDeleteRepository<TEntity>
        where TEntity : class, new()
    {
    }

    public interface ICreateRepository<TEntity>
        where TEntity : class, new()
    {
        Task<TEntity> CreateAsync(TEntity entity);
    }

    public interface IUpdateRepository<TEntity>
        where TEntity : class, new()
    {
        Task UpdateAsync(TEntity entity);
    }

    public interface IDeleteRepository<TEntity>
        where TEntity : class, new()
    {
        Task DeleteAsync(TEntity entity);
    }
}
