using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IContestFacade
    {
        IContestStore Contests { get; }

        IProblemsetStore Problemset { get; }

        ITeamStore Teams { get; }

        Task<Dictionary<string, Language>> ListLanguageAsync(int cid);
    }
}
