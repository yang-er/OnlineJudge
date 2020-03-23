using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public partial class ProblemFacade :
        IArchiveStore,
        IUpdateRepositoryImpl<ProblemArchive>
    {
        public IArchiveStore ArchiveStore => this;

        public DbSet<ProblemArchive> Archives => Context.Set<ProblemArchive>();

        async Task<ProblemArchive> ICreateRepository<ProblemArchive>.CreateAsync(ProblemArchive archive)
        {
            if (archive.PublicId != 0)
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

        Task<ProblemArchive> IArchiveStore.FindAsync(int pid)
        {
            var query =
                from a in Archives
                where a.PublicId == pid
                join p in Problems on a.ProblemId equals p.ProblemId
                select new ProblemArchive(a, p.Title, p.Source, p.AllowSubmit);
            return query.SingleOrDefaultAsync();
        }

        Task<ProblemArchive> IArchiveStore.FindInternalAsync(int pid)
        {
            return Archives.SingleOrDefaultAsync(p => p.ProblemId == pid);
        }

        Task<List<ProblemArchive>> IArchiveStore.ListAsync(int page, int uid)
        {
            var query =
                from a in Archives
                where a.PublicId <= IArchiveStore.StartId + page * IArchiveStore.ArchivePerPage
                    && a.PublicId > IArchiveStore.StartId + (page - 1) * IArchiveStore.ArchivePerPage
                join p in Problems on a.ProblemId equals p.ProblemId
                join ss in Context.Set<SubmissionStatistics>()
                    on new { p.ProblemId, ContestId = 0, Author = uid }
                    equals new { ss.ProblemId, ss.ContestId, ss.Author }
                orderby a.PublicId ascending
                select new ProblemArchive(a, p.Title, p.Source, ss.AcceptedSubmission, ss.TotalSubmission);
            return query.ToListAsync();
        }

        Task<int> IArchiveStore.MaxPageAsync()
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
