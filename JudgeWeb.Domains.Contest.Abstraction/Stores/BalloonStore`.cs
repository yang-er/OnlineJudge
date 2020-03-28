using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IBalloonStore :
        ICreateRepository<Balloon>
    {
        Task<List<Balloon>> ListAsync(int cid, ContestProblem[] problems);

        Task SetDoneAsync(int cid, int id);
    }
}
