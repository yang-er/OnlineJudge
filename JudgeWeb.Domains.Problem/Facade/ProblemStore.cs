using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public class ProblemStore :
        IProblemStore,
        ICrudRepositoryImpl<Problem>,
        ICreateRepositoryImpl<Testcase>
    {
        public DbContext Context { get; }

        public IMutableFileProvider Files { get; }

        public DbSet<Problem> Problems => Context.Set<Problem>();

        public ProblemStore(DbContext context, IProblemFileRepository files)
        {
            Context = context;
            Files = files;
        }

        public Task<Problem> FindAsync(int pid)
        {
            return Problems.SingleOrDefaultAsync(p => p.ProblemId == pid);
        }

        public async Task<(IEnumerable<Problem>, int)> ListAsync(
            int? uid, int page, int perCount)
        {
            IQueryable<Problem> problemSource = Problems;

            if (uid.HasValue)
            {
                var avaliable =
                    from ur in Context.Set<IdentityUserRole<int>>()
                    where ur.UserId == uid
                    join r in Context.Set<Role>() on ur.RoleId equals r.Id
                    where r.ProblemId != null
                    select r.ProblemId;
                problemSource = problemSource
                    .Where(p => avaliable.Contains(p.ProblemId));
            }

            int totalCount = await problemSource.CountAsync();
            int totPage = (totalCount - 1) / perCount + 1;

            var probs = await problemSource
                .Include(p => p.ArchiveCollection)
                .OrderBy(p => p.ProblemId)
                .Skip(perCount * (page - 1)).Take(perCount)
                .ToListAsync();

            return (probs, totPage);
        }

        public IFileInfo GetFile(int problemId, string fileName)
        {
            return Files.GetFileInfo($"p{problemId}/{fileName}");
        }

        protected Task<string> TryReadFileAsync(Problem problem, string fileName)
        {
            var fileInfo = GetFile(problem.ProblemId, fileName);
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

            var testcases = await Context.Set<Testcase>()
                .Where(t => t.ProblemId == pid)
                .Where(t => t.IsSecret == false)
                .OrderBy(t => t.Rank)
                .ToListAsync();
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

        public Task ToggleSubmitAsync(int pid, bool tobe)
        {
            return Problems
                .Where(p => p.ProblemId == pid)
                .BatchUpdateAsync(p => new Problem { AllowSubmit = tobe });
        }

        public Task ToggleJudgeAsync(int pid, bool tobe)
        {
            return Problems
                .Where(p => p.ProblemId == pid)
                .BatchUpdateAsync(p => new Problem { AllowJudge = tobe });
        }
    }
}
