using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public class DbContextProblemFacade<TContext> : ProblemFacade
        where TContext : DbContext
    {
        public DbContextProblemFacade(
            TContext context,
            IProblemFileRepository fileProvider,
            ILogger<ProblemFacade> logger) :
            base(context, fileProvider, logger)
        {
        }
    }

    public partial class ProblemFacade : IProblemFacade
    {
        public DbContext Context { get; }

        public ILogger<IProblemFacade> Logger { get; }

        public IMutableFileProvider Files { get; }

        public ProblemFacade(
            DbContext context,
            IMutableFileProvider fileProvider,
            ILogger<ProblemFacade> logger)
        {
            Context = context;
            Logger = logger;
            Files = fileProvider;
        }

        public IFileInfo GetFile(Problem problem, string fileName)
        {
            return Files.GetFileInfo($"p{problem.ProblemId}/{fileName}");
        }

        public IFileInfo GetFile(ProblemArchive problem, string fileName)
        {
            return Files.GetFileInfo($"p{problem.ProblemId}/{fileName}");
        }

        protected Task<string> TryReadFileAsync(Problem problem, string fileName)
        {
            var fileInfo = GetFile(problem, fileName);
            if (!fileInfo.Exists) return Task.FromResult("");
            return fileInfo.ReadAsync();
        }

        public async Task<ProblemStatement> StatementAsync(Problem problem)
        {
            var pid = problem.ProblemId;

            var description = await TryReadFileAsync(problem, "description.md");
            var inputdesc = await TryReadFileAsync(problem, "inputdesc.md");
            var outputdesc = await TryReadFileAsync(problem, "outputdesc.md");
            var hint = await TryReadFileAsync(problem, "hint.md");
            var interact = await TryReadFileAsync(problem, "interact.md");

            var testcases = await TestcaseStore.ListAsync(pid, false);
            var samples = new List<MemoryTestCase>();

            foreach (var item in testcases)
            {
                var input = await TryReadFileAsync(problem, $"t{item.TestcaseId}.in");
                var output = await TryReadFileAsync(problem, $"t{item.TestcaseId}.out");
                samples.Add(new MemoryTestCase(item.Description, input, output, item.Point));
            }

            return new ProblemStatement
            {
                Description = description,
                Hint = hint,
                Input = inputdesc,
                Output = outputdesc,
                Interaction = interact,
                Problem = problem,
                Samples = samples
            };
        }

        public Task<IFileInfo> WriteFileAsync(Problem problem, string fileName, string content)
        {
            return Files.WriteStringAsync($"p{problem.ProblemId}/{fileName}", content);
        }

        public Task<IFileInfo> WriteFileAsync(Problem problem, string fileName, byte[] content)
        {
            return Files.WriteBinaryAsync($"p{problem.ProblemId}/{fileName}", content);
        }

        public Task<IFileInfo> WriteFileAsync(Problem problem, string fileName, Stream content)
        {
            return Files.WriteStreamAsync($"p{problem.ProblemId}/{fileName}", content);
        }
    }
}
