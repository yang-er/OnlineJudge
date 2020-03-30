using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IClarificationStore
    {
        Task<Clarification> FindAsync(int cid, int id);

        Task<List<Clarification>> ListAsync(int cid,
            Expression<Func<Clarification, bool>>? predicate = null);

        Task<int> GetJuryStatusAsync(int cid);
        
        Task<int> SendAsync(Clarification clar, Clarification replyTo = null);

        Task<int> SetAnsweredAsync(int cid, int clarId, bool answered);

        Task<int> ClaimAsync(int cid, int clarId, string jury, bool claim);
    }
}
