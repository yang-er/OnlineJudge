using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Inject(typeof(IExecutableStore), typeof(ExecutableStore))]
namespace JudgeWeb.Domains.Problems
{
    public class ExecutableStore :
        IExecutableStore,
        ICrudRepositoryImpl<Executable>
    {
        public DbContext Context { get; }

        public DbSet<Executable> Executables => Context.Set<Executable>();

        public ExecutableStore(DbContext context)
        {
            Context = context;
        }

        public Task<Dictionary<string, string>> ListMd5Async(params string[] targets)
        {
            return Executables
                .Where(e => targets.Contains(e.ExecId))
                .Select(e => new { e.ExecId, e.Md5sum })
                .ToDictionaryAsync(e => e.ExecId, e => e.Md5sum);
        }

        public async Task<List<ExecutableViewContentModel>> FetchContentAsync(Executable executable)
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

        public Task<Executable> FindAsync(string execid)
        {
            return Executables.SingleOrDefaultAsync(e => e.ExecId == execid);
        }

        public Task<List<Executable>> ListAsync(string? type = null)
        {
            IQueryable<Executable> execs = Executables;
            if (type != null)
                execs.Where(e => e.Type == type);
            return execs
                .Select(e => new Executable(e.ExecId, e.Md5sum, e.ZipSize, e.Description, e.Type))
                .ToListAsync();
        }

        public async Task<ILookup<string, string>> ListUsageAsync(Executable executable)
        {
            var execid = executable.ExecId;
            var compile = Context.Set<Language>()
                .Where(l => l.CompileScript == execid)
                .Select(l => new { l.Id, Type = "compile" });
            var run = Context.Set<Problem>()
                .Where(p => p.RunScript == execid)
                .Select(p => new { Id = p.ProblemId.ToString(), Type = "run" });
            var compare = Context.Set<Problem>()
                .Where(p => p.CompareScript == execid)
                .Select(p => new { Id = p.ProblemId.ToString(), Type = "compare" });
            var query = compile.Concat(run).Concat(compare);
            var list = await query.ToListAsync();
            return list.ToLookup(k => k.Type, v => v.Id);
        }
    }
}
