using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IProblemStore
    {
        protected const int ArchivePerPage = 50;
        protected const int StartId = 1000;

        public static readonly string[] MarkdownFiles = new[]
        {
            "description",
            "inputdesc",
            "outputdesc",
            "hint",
            "interact"
        };

        ValueTask<(IEnumerable<Problem> model, int totPage)> ListProblemsAsync(int? uid, int page, int perCount);

        Task<Problem> FindProblemAsync(int pid);

        Task<ProblemArchive> FindArchiveByInternalAsync(int pid);

        Task<ProblemArchive> FindArchiveAsync(int pid);

        Task<Language> FindLanguageAsync(string langid);

        Task<Executable> FindExecutableAsync(string execid);

        Task<Testcase> FindTestcaseAsync(int pid, int tid);

        Task<IEnumerable<ProblemArchive>> ListByArchiveAsync(int page, int uid);

        Task<int> CountArchivePageAsync();

        Task<IEnumerable<Language>> ListLanguagesAsync(bool? active = null);

        Task<IEnumerable<Testcase>> ListTestcasesAsync(int pid, bool? secret = null);

        Task<int> CountTestcaseAsync(Problem problem);

        Task ToggleProblemAsync(Problem problem, Expression<Func<Problem, bool>> expression);

        Task ChangeTestcaseRankAsync(int pid, int tid, bool up);

        Task<Problem> CreateAsync(Problem problem);
        Task<Language> CreateAsync(Language language);
        Task<Executable> CreateAsync(Executable executable);
        Task<Testcase> CreateAsync(Testcase testcase);
        Task<ProblemArchive> CreateAsync(ProblemArchive archive);

        Task UpdateAsync(Problem problem);
        Task UpdateAsync(Language language);
        Task UpdateAsync(Executable executable);
        Task UpdateAsync(ProblemArchive archive);
        Task UpdateAsync(Testcase testcase);

        Task DeleteAsync(Problem problem);
        Task DeleteAsync(Language language);
        Task DeleteAsync(Executable executable);
        Task<int> DeleteAsync(Testcase testcase);
    }
}
