using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using JudgeWeb.Features.OjUpdate;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(IStudentStore), typeof(StudentStore))]
namespace JudgeWeb.Domains.Identity
{
    public class StudentStore :
        IStudentStore,
        ICrudRepositoryImpl<Student>,
        ICrudRepositoryImpl<ClassStudent>,
        ICrudRepositoryImpl<TeachingClass>
    {
        public DbContext Context { get; }
        public StudentStore(DbContextAccessor context) => Context = context;

        DbSet<Student> Students => Context.Set<Student>();
        DbSet<TeachingClass> Classes => Context.Set<TeachingClass>();
        DbSet<ClassStudent> ClassStudent => Context.Set<ClassStudent>();
        DbSet<User> Users => Context.Set<User>();
        DbSet<Role> Roles => Context.Set<Role>();
        DbSet<IdentityUserRole<int>> UserRoles => Context.Set<IdentityUserRole<int>>();

        public Task<Student> FindStudentAsync(int sid)
        {
            return Students
                .Where(s => s.Id == sid)
                .SingleOrDefaultAsync();
        }

        public Task<User> FindByStudentIdAsync(int sid)
        {
            return Users
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

        public Task<List<Role>> ListRolesByUserIdAsync(int uid)
        {
            var query = from userRole in UserRoles
                        join role in Roles on userRole.RoleId equals role.Id
                        where userRole.UserId.Equals(uid)
                        select role;
            return query.ToListAsync();
        }

        public async Task<(List<User>, int)> ListUsersAsync(int page, int pageCount)
        {
            var lst = await Users
                .OrderBy(u => u.Id)
                .Skip((page - 1) * pageCount)
                .Take(pageCount)
                .ToListAsync();
            var count = await Users.CountAsync();
            return (lst, (count - 1) / pageCount + 1);
        }

        public Task<List<IdentityUserRole<int>>> ListUserRolesAsync(int min, int max)
        {
            return UserRoles
                .Where(ur => ur.UserId >= min && ur.UserId <= max)
                .ToListAsync();
        }

        public Task<Dictionary<int, Role>> ListNamedRolesAsync()
        {
            return Roles
                .Where(r => r.ShortName != null)
                .ToDictionaryAsync(r => r.Id);
        }

        public Task<int> MergeStudentListAsync(List<Student> students)
        {
            return Students.MergeAsync(
                sourceTable: students.Select(s => new { s.Id, s.Name }),
                targetKey: s => s.Id,
                sourceKey: s => s.Id,
                updateExpression: (t, s) => new Student { Name = s.Name },
                insertExpression: s => new Student { Id = s.Id, Name = s.Name },
                delete: false);
        }

        public async Task<(IEnumerable<Student>, int)> ListStudentsAsync(int page, int pageCount)
        {
            int total = await Students.CountAsync();
            int totPage = (total - 1) / pageCount + 1;

            var stuQuery =
                from s in Students
                join u in Users on s.Id equals u.StudentId
                into uu from u in uu.DefaultIfEmpty()
                orderby s.Id ascending
                select new Student
                {
                    Id = s.Id,
                    IsVerified = u.StudentVerified,
                    Name = s.Name,
                    Email = u.StudentEmail,
                    UserId = u.Id,
                    UserName = u.UserName,
                };

            var model = await stuQuery
                .Skip(pageCount * (page - 1))
                .Take(pageCount)
                .ToListAsync();
            return (model, totPage);
        }

        public async Task<IEnumerable<TeachingClass>> ListClassAsync()
        {
            return await Classes
                .Select(c => new TeachingClass { Id = c.Id, Name = c.Name, Count = c.Collection.Count })
                .ToListAsync();
        }

        public async Task<bool> ClassKickAsync(int classId, int studentId)
        {
            int count = await ClassStudent
                .Where(cs => cs.ClassId == classId && cs.StudentId == studentId)
                .BatchDeleteAsync();
            return count > 0;
        }

        public Task<TeachingClass> FindClassAsync(int classId)
        {
            return Classes.Where(s => s.Id == classId).SingleOrDefaultAsync();
        }

        public async Task<(IEnumerable<Student>, int)> ListStudentsAsync(int classId, int page, int pageCount)
        {
            int total = await ClassStudent.Where(c => c.ClassId == classId).CountAsync();
            int totPage = (total - 1) / pageCount + 1;

            var stuQuery =
                from gs in ClassStudent
                where gs.ClassId == classId
                join s in Students on gs.StudentId equals s.Id
                join u in Users on s.Id equals u.StudentId
                into uu from u in uu.DefaultIfEmpty()
                orderby s.Id ascending
                select new Student
                {
                    Id = s.Id,
                    IsVerified = u.StudentVerified,
                    Name = s.Name,
                    Email = u.StudentEmail,
                    UserId = u.Id,
                    UserName = u.UserName,
                };

            var model = await stuQuery
                .Skip(pageCount * (page - 1))
                .Take(pageCount)
                .ToListAsync();
            return (model, totPage);
        }

        public Task<int[]> CheckStudentIdAsync(IEnumerable<int> ids)
        {
            return Students
                .Where(s => ids.Contains(s.Id))
                .Select(s => s.Id)
                .ToArrayAsync();
        }

        public Task<int> MergeClassStudentAsync(int ClassId, IEnumerable<int> studIds)
        {
            return ClassStudent.MergeAsync(
                sourceTable: studIds.Select(a => new { ClassId, StudentId = a }),
                targetKey: f => new { f.ClassId, f.StudentId },
                sourceKey: f => new { f.ClassId, f.StudentId },
                updateExpression: null,
                insertExpression: s => new ClassStudent { ClassId = s.ClassId, StudentId = s.StudentId },
                delete: false);
        }
    }
}
