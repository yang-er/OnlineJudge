using JudgeWeb.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public interface INewsStore
    {
        Task<IEnumerable<(int id, string title)>> ListActiveAsync(int count);

        Task<List<News>> ListAsync();

        Task<News> FindAsync(int newid);

        Task<News> CreateAsync(News news);

        Task DeleteAsync(News news);

        Task UpdateAsync(News news);
    }
}
