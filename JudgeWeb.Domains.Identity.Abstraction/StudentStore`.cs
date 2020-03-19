using JudgeWeb.Data;
using JudgeWeb.Features.OjUpdate;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public interface IStudentStore
    {
        IQueryable<Student> Students { get; }

        IQueryable<TeachingClass> Classes { get; }

        IQueryable<ClassStudent> ClassStudent { get; }

        Task<List<OjAccount>> GetRanklistAsync(int cid, int year);

        Task<Student> FindStudentAsync(int sid);

        Task<User> FindByStudentIdAsync(int sid);

        Task<IdentityResult> SlideExpirationAsync(User user);
    }
}
