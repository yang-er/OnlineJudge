using JudgeWeb.Data;
using JudgeWeb.Features.OjUpdate;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity
{
    public interface IStudentStore :
        ICrudRepository<Student>,
        ICrudRepository<TeachingClass>,
        ICrudRepository<ClassStudent>
    {
        Task<List<OjAccount>> GetRanklistAsync(int cid, int year);

        Task<Student> FindStudentAsync(int sid);

        Task<User> FindByStudentIdAsync(int sid);

        Task<IdentityResult> SlideExpirationAsync(User user);

        Task<List<Role>> ListRolesByUserIdAsync(int uid);

        Task<(List<User>, int)> ListUsersAsync(int page, int pageCount);

        Task<List<IdentityUserRole<int>>> ListUserRolesAsync(int minUid, int maxUid);

        Task<Dictionary<int, Role>> ListNamedRolesAsync();

        Task<int> MergeStudentListAsync(List<Student> students);

        Task<(IEnumerable<Student>, int)> ListStudentsAsync(int page, int pageCount);

        Task<(IEnumerable<Student>, int)> ListStudentsAsync(int classId, int page, int pageCount);

        Task<int[]> CheckStudentIdAsync(IEnumerable<int> ids);

        Task<int> MergeClassStudentAsync(int classId, IEnumerable<int> studIds);

        Task<IEnumerable<TeachingClass>> ListClassAsync();

        Task<bool> ClassKickAsync(int classId, int studentId);

        Task<TeachingClass> FindClassAsync(int classId);
    }
}
