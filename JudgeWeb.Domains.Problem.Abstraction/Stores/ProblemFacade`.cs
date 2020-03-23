using JudgeWeb.Data;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IProblemFacade
    {
        ILogger<IProblemFacade> Logger { get; }

        IExecutableStore ExecutableStore { get; }

        ILanguageStore LanguageStore { get; }

        IProblemStore ProblemStore { get; }

        ITestcaseStore TestcaseStore { get; }

        IArchiveStore ArchiveStore { get; }

        public IExecutableStore Executables => ExecutableStore;

        public ILanguageStore Languages => LanguageStore;

        public IProblemStore Problems => ProblemStore;

        public ITestcaseStore Testcases => TestcaseStore;

        public IArchiveStore Archives => ArchiveStore;

        public static readonly string[] MarkdownFiles = new[]
        {
            "description",
            "inputdesc",
            "outputdesc",
            "hint",
            "interact"
        };

        Task<IFileInfo> WriteFileAsync(Problem problem, string fileName, string content);

        Task<IFileInfo> WriteFileAsync(Problem problem, string fileName, byte[] content);

        Task<IFileInfo> WriteFileAsync(Problem problem, string fileName, Stream content);

        IFileInfo GetFile(Problem problem, string fileName);

        IFileInfo GetFile(ProblemArchive problem, string fileName);

        Task<ProblemStatement> StatementAsync(Problem problem);
    }
}
