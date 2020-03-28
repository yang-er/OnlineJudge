using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IJudgehostStore :
        ICrudRepository<JudgeHost>
    {
        Task<int> ToggleAsync(string hostname, bool active);

        Task<List<JudgeHost>> ListAsync();

        Task<JudgeHost> FindAsync(string name);

        Task NotifyPollAsync(JudgeHost host);

        Task<int> CountJudgingsAsync(string hostname);

        Task<int> GetJudgeStatusAsync();

        Task<List<Judging>> FetchJudgingsAsync(string hostname, int count);
    }
}
