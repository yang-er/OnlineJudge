using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IContestEventNotifier
    {
        Task Update(int cid, Judging judging);

        Task Create(int cid, Detail detail);
    }
}
