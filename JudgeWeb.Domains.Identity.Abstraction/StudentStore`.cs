using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public interface IStudentStore
    {
        IQueryable<Student> Students { get; }

        IQueryable<TeachingClass> Classes { get; }

        IQueryable<ClassStudent> ClassStudent { get; }

        Task<Student> FindStudentAsync(int sid);

        Task<User> FindByStudentIdAsync(int sid);

        Task<IdentityResult> SlideExpirationAsync(User user);
    }
}
