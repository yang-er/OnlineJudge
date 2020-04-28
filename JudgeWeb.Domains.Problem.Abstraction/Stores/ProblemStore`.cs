using JudgeWeb.Data;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IProblemStore :
        ICrudRepository<Problem>,
        ICreateRepository<Testcase>
    {
        Task ToggleSubmitAsync(int pid, bool tobe);

        Task ToggleJudgeAsync(int pid, bool tobe);

        Task<Problem> FindAsync(int pid);

        Task<(IEnumerable<Problem> model, int totPage)> ListAsync(
            int? uid, int page, int perCount);

        Task<IEnumerable<(int UserId, string UserName, string NickName)>> ListPermittedUserAsync(int pid);

        Task<IFileInfo> WriteFileAsync(Problem problem, string fileName, string content);

        Task<IFileInfo> WriteFileAsync(Problem problem, string fileName, byte[] content);

        Task<IFileInfo> WriteFileAsync(Problem problem, string fileName, Stream content);

        IFileInfo GetFile(int problemId, string fileName);

        public IFileInfo GetFile(Problem problem, string fileName)
            => GetFile(problem.ProblemId, fileName);

        public IFileInfo GetFile(ProblemArchive problem, string fileName)
            => GetFile(problem.ProblemId, fileName);

        Task<ProblemStatement> StatementAsync(Problem problem);

        public static readonly string[] MarkdownFiles = new[]
        {
            "description",
            "inputdesc",
            "outputdesc",
            "hint",
            "interact"
        };
    }
}
