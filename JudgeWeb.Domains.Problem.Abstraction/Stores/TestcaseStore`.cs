using JudgeWeb.Data;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface ITestcaseStore :
        ICreateRepository<Testcase>,
        IUpdateRepository<Testcase>
    {
        Task<List<Testcase>> ListAsync(int pid, bool? secret = null);

        Task<Testcase> FindAsync(int pid, int tid);

        Task<Testcase> FindAsync(int tid);

        Task<int> CascadeDeleteAsync(Testcase testcase);

        Task ChangeRankAsync(int pid, int tid, bool up);

        Task<int> CountAsync(int problemId);

        IFileInfo GetFile(Testcase testcase, string target);

        public Task<int> CountAsync(Problem problem) => CountAsync(problem.ProblemId);
    }
}
