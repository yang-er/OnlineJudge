using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public class TestcaseStore :
        ITestcaseStore,
        ICrudRepositoryImpl<Testcase>
    {
        public DbContext Context { get; }

        public DbSet<Testcase> Testcases => Context.Set<Testcase>();

        public IMutableFileProvider Files { get; }

        public TestcaseStore(DbContext context, IProblemFileRepository fs)
        {
            Context = context;
            Files = fs;
        }

        public Task<List<Testcase>> ListAsync(int pid, bool? secret)
        {
            var query = Testcases.Where(t => t.ProblemId == pid);
            if (secret.HasValue)
                query = query.Where(t => t.IsSecret == secret.Value);
            query = query.OrderBy(t => t.Rank);
            return query.ToListAsync();
        }

        public Task<Testcase> FindAsync(int pid, int tid)
        {
            return Testcases
                .Where(t => t.ProblemId == pid && t.TestcaseId == tid)
                .SingleOrDefaultAsync();
        }

        public async Task<int> CascadeDeleteAsync(Testcase testcase)
        {
            using var tran = await Context.Database.BeginTransactionAsync();
            int dts;

            try
            {
                // details are set ON DELETE NO ACTION, so we have to delete it before
                dts = await Context.Set<Detail>()
                    .Where(d => d.TestcaseId == testcase.TestcaseId)
                    .BatchDeleteAsync();
                await Testcases
                    .Where(t => t.TestcaseId == testcase.TestcaseId)
                    .BatchDeleteAsync();
                // set the rest testcases correct rank
                await Testcases
                    .Where(t => t.Rank > testcase.Rank)
                    .BatchUpdateAsync(t => new Testcase { Rank = t.Rank - 1 });
                await tran.CommitAsync();
            }
            catch
            {
                dts = -1;
            }

            return dts;
        }

        public async Task ChangeRankAsync(int pid, int tid, bool up)
        {
            var tc = await Testcases
                .Where(t => t.ProblemId == pid && t.TestcaseId == tid)
                .FirstOrDefaultAsync();
            if (tc == null) return;

            int rk2 = tc.Rank + (up ? -1 : 1);
            var tc2 = await Testcases
                .Where(t => t.ProblemId == pid && t.Rank == rk2)
                .FirstOrDefaultAsync();

            if (tc2 != null)
            {
                tc2.Rank = tc.Rank;
                tc.Rank = rk2;
                Testcases.UpdateRange(tc, tc2);
                await Context.SaveChangesAsync();
            }
        }

        public Task<int> CountAsync(int pid)
        {
            return Testcases.CountAsync(p => p.ProblemId == pid);
        }

        public Task<Testcase> FindAsync(int tid)
        {
            return Testcases.SingleOrDefaultAsync(t => t.TestcaseId == tid);
        }

        public IFileInfo GetFile(Testcase tc, string target)
        {
            return Files.GetFileInfo($"p{tc.ProblemId}/t{tc.TestcaseId}.{target}");
        }
    }
}
