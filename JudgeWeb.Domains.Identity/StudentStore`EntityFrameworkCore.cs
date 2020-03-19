using JudgeWeb.Data;
using JudgeWeb.Features.OjUpdate;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public class EntityFrameworkCoreStudentStore<TContext> : EntityFrameworkCoreStudentStore
        where TContext : DbContext
    {
        public EntityFrameworkCoreStudentStore(TContext context)
            : base(context)
        {
        }
    }

    public class EntityFrameworkCoreStudentStore : IStudentStore
    {
        public DbContext Context { get; }
        protected EntityFrameworkCoreStudentStore(DbContext context) => Context = context;

        IQueryable<Student> IStudentStore.Students => Context.Set<Student>();
        IQueryable<TeachingClass> IStudentStore.Classes => Context.Set<TeachingClass>();
        IQueryable<ClassStudent> IStudentStore.ClassStudent => Context.Set<ClassStudent>();

        DbSet<Student> Students => Context.Set<Student>();
        DbSet<TeachingClass> Classes => Context.Set<TeachingClass>();
        DbSet<ClassStudent> ClassStudent => Context.Set<ClassStudent>();

        public Task<Student> FindStudentAsync(int sid)
        {
            return Students
                .Where(s => s.Id == sid)
                .SingleOrDefaultAsync();
        }

        public Task<User> FindByStudentIdAsync(int sid)
        {
            return Context.Set<User>()
                .Where(u => u.StudentId == sid)
                .SingleOrDefaultAsync();
        }

        public Task<IdentityResult> SlideExpirationAsync(User user)
        {
            CachedQueryable.Cache.Set(
                key: "SlideExpiration: " + user.NormalizedUserName,
                value: DateTimeOffset.UtcNow,
                absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(20));
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<List<OjAccount>> GetRanklistAsync(int cid, int year)
        {
            var query = Context.Set<PersonRank>()
                .Where(p => p.Category == cid);
            if (year != -1)
                query = query.Where(p => p.Grade == year);
            return query.Select(p => new OjAccount(p)).ToListAsync();
        }
    }
}
