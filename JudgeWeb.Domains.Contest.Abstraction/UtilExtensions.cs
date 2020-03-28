using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.Extensions.FileProviders;

namespace JudgeWeb.Domains.Contests
{
    public static class UtilExtensions
    {
        public static IFileInfo GetFile(this IProblemStore that, ContestProblem problem, string fileName)
            => that.GetFile(problem.ProblemId, fileName);
    }
}
