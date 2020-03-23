using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public partial class ProblemFacade :
        IExecutableStore,
        ICrudRepositoryImpl<Executable>,
        ICrudInstantUpdateImpl<Executable>
    {
        public IExecutableStore ExecutableStore => this;

        public DbSet<Executable> Executables { get; }

        public Task<Dictionary<string, string>> ListMd5Async(params string[] targets)
        {
            return Executables
                .Where(e => targets.Contains(e.ExecId))
                .Select(e => new { e.ExecId, e.Md5sum })
                .ToDictionaryAsync(e => e.ExecId, e => e.Md5sum);
        }

        async Task<List<ExecutableViewContentModel>> IExecutableStore.FetchContentAsync(Executable executable)
        {
            var items = new List<ExecutableViewContentModel>();
            using var stream = new MemoryStream(executable.ZipFile, false);
            using var zipArchive = new ZipArchive(stream);

            foreach (var entry in zipArchive.Entries)
            {
                var fileName = entry.FullName;
                var fileExt = Path.GetExtension(fileName);
                fileExt = string.IsNullOrEmpty(fileExt) ? "dummy.sh" : "dummy" + fileExt;

                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream, Encoding.UTF8, false);
                var fileContent2 = await reader.ReadToEndAsync();

                items.Add(new ExecutableViewContentModel
                {
                    FileName = fileName,
                    FileContent = fileContent2,
                    Language = fileExt,
                });
            }

            return items;
        }

        Task<Executable> IExecutableStore.FindAsync(string execid)
        {
            return Executables.SingleOrDefaultAsync(e => e.ExecId == execid);
        }

        Task<List<Executable>> IExecutableStore.ListAsync(string? type)
        {
            IQueryable<Executable> execs = Executables;
            if (type != null)
                execs.Where(e => e.Type == type);
            return execs
                .Select(e => new Executable(e.ExecId, e.Md5sum, e.ZipSize, e.Description, e.Type))
                .ToListAsync();
        }

        async Task<ILookup<string, string>> IExecutableStore.ListUsageAsync(Executable executable)
        {
            var execid = executable.ExecId;
            var compile = Languages
                .Where(l => l.CompileScript == execid)
                .Select(l => new { l.Id, Type = "compile" });
            var run = Problems
                .Where(p => p.RunScript == execid)
                .Select(p => new { Id = p.ProblemId.ToString(), Type = "run" });
            var compare = Problems
                .Where(p => p.CompareScript == execid)
                .Select(p => new { Id = p.ProblemId.ToString(), Type = "compare" });
            var query = compile.Concat(run).Concat(compare);
            var list = await query.ToListAsync();
            return list.ToLookup(k => k.Type, v => v.Id);
        }
    }
}
