using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(IArchiveStore), typeof(ArchiveStore))]
namespace JudgeWeb.Domains.Problems
{
    public class ArchiveStore :
        IArchiveStore,
        IUpdateRepositoryImpl<ProblemArchive>
    {
        public DbContext Context { get; }

        public DbSet<ProblemArchive> Archives => Context.Set<ProblemArchive>();

        public ArchiveStore(DbContext context)
        {
            Context = context;
        }

        public async Task<ProblemArchive> CreateAsync(ProblemArchive archive)
        {
            if (archive.PublicId == 0)
            {
                archive.PublicId = await Archives.MaxAsync(p => p.PublicId);
                if (archive.PublicId == 0) archive.PublicId = 1001;
                else archive.PublicId++;
            }
            else
            {
                var existence = await Archives
                    .Where(p => p.PublicId == archive.PublicId)
                    .SingleOrDefaultAsync();
                if (existence != null)
                    throw new InvalidOperationException("Error public id was set.");
            }

            Archives.Add(archive);
            await Context.SaveChangesAsync();
            return archive;
        }

        public Task<ProblemArchive> FindAsync(int pid)
        {
            return Archives
                .Where(a => a.PublicId == pid)
                .Select(a => new ProblemArchive(a, a.p.Title, a.p.Source, a.p.AllowSubmit))
                .SingleOrDefaultAsync();
        }

        public Task<ProblemArchive> FindInternalAsync(int pid)
        {
            return Archives.SingleOrDefaultAsync(p => p.ProblemId == pid);
        }

        public Task<List<ProblemArchive>> ListAsync(int page, int uid)
        {
            var query =
                from a in Archives
                where a.PublicId <= IArchiveStore.StartId + page * IArchiveStore.ArchivePerPage
                    && a.PublicId > IArchiveStore.StartId + (page - 1) * IArchiveStore.ArchivePerPage
                join ss in Context.Set<SubmissionStatistics>()
                    on new { a.ProblemId, ContestId = 0, Author = uid }
                    equals new { ss.ProblemId, ss.ContestId, ss.Author }
                into sss from ss in sss.DefaultIfEmpty()
                orderby a.PublicId ascending
                select new ProblemArchive(a, a.p.Title, a.p.Source, ss.AcceptedSubmission, ss.TotalSubmission);
            return query.ToListAsync();
        }

        public Task<int> MaxPageAsync()
        {
            return Context.CachedGetAsync("prob::totcount", TimeSpan.FromMinutes(10), async () =>
            {
                var pid = await Archives
                    .OrderByDescending(p => p.PublicId)
                    .Select(p => new { p.PublicId })
                    .FirstOrDefaultAsync();
                return ((pid?.PublicId ?? IArchiveStore.StartId) - 1 - IArchiveStore.StartId) / IArchiveStore.ArchivePerPage + 1;
            });
        }
    }
}
