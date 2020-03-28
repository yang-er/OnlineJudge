using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IPrintingStore : ICrudRepository<Printing>
    {
        Task<bool> SetStateAsync(int pid, bool? done);

        Task<List<T>> ListAsync<T>(int takeCount, int page,
            Expression<Func<Printing, User, Team, T>> expression,
            Expression<Func<Printing, bool>>? predicate = null);

        public Task<bool> SetStateAsync(int cid, int pid, bool? done)
            => SetStateAsync(pid, done);
    }
}
