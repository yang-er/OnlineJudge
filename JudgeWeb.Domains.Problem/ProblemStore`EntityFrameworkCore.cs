using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public class EntityFrameworkCoreProblemStore<TContext>
        : EntityFrameworkCoreProblemStore
        where TContext : DbContext
    {
        public EntityFrameworkCoreProblemStore(TContext context)
            : base(context)
        {
        }
    }

    public class EntityFrameworkCoreProblemStore : IProblemStore
    {
        public DbContext Context { get; }
        public EntityFrameworkCoreProblemStore(DbContext context) => Context = context;

        public DbSet<Problem> Problems => Context.Set<Problem>();
        public DbSet<Language> Languages => Context.Set<Language>();
        public DbSet<Executable> Executables => Context.Set<Executable>();
        public DbSet<Testcase> Testcases => Context.Set<Testcase>();
        public DbSet<ProblemArchive> Archives => Context.Set<ProblemArchive>();


        public async ValueTask<(IEnumerable<Problem> model, int totPage)> ListProblemsAsync(int? uid, int page, int perCount)
        {
            var problemSource = (IQueryable<Problem>)Problems;

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

        public async Task<IEnumerable<Testcase>> ListTestcasesAsync(int pid, bool? secret = null)
        {
            var query = Testcases.Where(t => t.ProblemId == pid);
            if (secret.HasValue)
                query = query.Where(t => t.IsSecret == secret.Value);
            query = query.OrderBy(t => t.Rank);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Language>> ListLanguagesAsync(bool? active = null)
        {
            if (active == null) return await Languages.ToListAsync();
            return await Languages.Where(l => l.AllowSubmit == active.Value).ToListAsync();
        }

        public Task ToggleProblemAsync(Problem problem, Expression<Func<Problem, bool>> expression)
        {
            Expression<Func<Problem, Problem>> body = expression.Body switch
            {
                MemberExpression me
                when me.Member.Name == nameof (Problem.AllowJudge)
                    => p => new Problem { AllowJudge = !p.AllowJudge },
                MemberExpression me
                when me.Member.Name == nameof(Problem.AllowSubmit)
                    => p => new Problem { AllowSubmit = !p.AllowSubmit },
                _ => throw new InvalidOperationException()
            };

            return Problems.Where(p => p.ProblemId == problem.ProblemId)
                .BatchUpdateAsync(body);
        }

        public Task<int> CountTestcaseAsync(Problem problem)
        {
            return Testcases.Where(p => p.ProblemId == problem.ProblemId).CountAsync();
        }



        public Task<Problem> FindProblemAsync(int pid)
            => Problems.Where(p => p.ProblemId == pid).SingleOrDefaultAsync();
        public Task<Language> FindLanguageAsync(string langid)
            => Languages.Where(l => l.Id == langid).SingleOrDefaultAsync();
        public Task<Executable> FindExecutableAsync(string execid)
            => Executables.Where(l => l.ExecId == execid).SingleOrDefaultAsync();
        public Task<ProblemArchive> FindArchiveByInternalAsync(int pid)
            => Archives.Where(p => p.ProblemId == pid).SingleOrDefaultAsync();
        public Task<Testcase> FindTestcaseAsync(int pid, int tid)
            => Testcases.Where(t => t.ProblemId == pid && t.TestcaseId == tid).SingleOrDefaultAsync();

        private async Task<T> Create<T>(T entity) where T : class
        {
            var e = Context.Set<T>().Add(entity);
            await Context.SaveChangesAsync();
            return e.Entity;
        }

        public Task<Executable> CreateAsync(Executable executable) => Create(executable);
        public Task<Language> CreateAsync(Language language) => Create(language);
        public Task<Problem> CreateAsync(Problem problem) => Create(problem);
        public Task<Testcase> CreateAsync(Testcase testcase) => Create(testcase);

        public async Task<ProblemArchive> CreateAsync(ProblemArchive archive)
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

            return await Create(archive);
        }

        private Task Delete<T>(T entity) where T : class
        {
            Context.Set<T>().Remove(entity);
            return Context.SaveChangesAsync();
        }

        public Task DeleteAsync(Executable executable) => Delete(executable);
        public Task DeleteAsync(Language language) => Delete(language);
        public Task DeleteAsync(Problem problem) => Delete(problem);

        public async Task<int> DeleteAsync(Testcase testcase)
        {
            using var tran = await Context.Database.BeginTransactionAsync();
            int dts;

            try
            {
                // details are set ON DELETE NO ACTION, so we have to delete it before
                dts = await Context.Set<Detail>()
                    .Where(d => d.TestcaseId == testcase.TestcaseId)
                    .BatchDeleteAsync();
                await Context.Set<Testcase>()
                    .Where(t => t.TestcaseId == testcase.TestcaseId)
                    .BatchDeleteAsync();
                // set the rest testcases correct rank
                await Context.Set<Testcase>()
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

        private Task Update<T>(T entity) where T : class
        {
            Context.Set<T>().Update(entity);
            return Context.SaveChangesAsync();
        }

        public Task UpdateAsync(Executable executable) => Update(executable);
        public Task UpdateAsync(Language language) => Update(language);
        public Task UpdateAsync(Problem problem) => Update(problem);
        public Task UpdateAsync(Testcase testcase) => Update(testcase);
        public Task UpdateAsync(ProblemArchive archive) => Update(archive);

        public async Task ChangeTestcaseRankAsync(int pid, int tid, bool up)
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

        public async Task<IEnumerable<ProblemArchive>> ListByArchiveAsync(int page, int uid)
        {
            var query =
                from a in Archives
                where a.PublicId <= IProblemStore.StartId + page * IProblemStore.ArchivePerPage
                    && a.PublicId > IProblemStore.StartId + (page - 1) * IProblemStore.ArchivePerPage
                join p in Problems on a.ProblemId equals p.ProblemId
                join ss in Context.Set<SubmissionStatistics>()
                    on new { p.ProblemId, ContestId = 0, Author = uid }
                    equals new { ss.ProblemId, ss.ContestId, ss.Author }
                orderby a.PublicId ascending
                select new ProblemArchive(a, p.Title, p.Source, ss.AcceptedSubmission, ss.TotalSubmission);
            return await query.ToListAsync();
        }

        public Task<int> CountArchivePageAsync()
        {
            return Context.CachedGetAsync("prob::totcount", TimeSpan.FromMinutes(10), async () =>
            {
                var pid = await Archives
                    .OrderByDescending(p => p.PublicId)
                    .Select(p => new { p.PublicId })
                    .FirstOrDefaultAsync();
                return ((pid?.PublicId ?? IProblemStore.StartId) - 1 - IProblemStore.StartId) / IProblemStore.ArchivePerPage + 1;
            });
        }

        public Task<ProblemArchive> FindArchiveAsync(int pid)
        {
            var query =
                from a in Archives
                where a.PublicId == pid
                join p in Problems on a.ProblemId equals p.ProblemId
                select new ProblemArchive(a, p.Title, p.Source, p.AllowSubmit);
            return query.SingleOrDefaultAsync();
        }
    }
}
