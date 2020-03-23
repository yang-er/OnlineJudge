using JudgeWeb.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IArchiveStore :
        ICreateRepository<ProblemArchive>,
        IUpdateRepository<ProblemArchive>
    {
        protected const int ArchivePerPage = 50;
        protected const int StartId = 1000;

        Task<ProblemArchive> FindInternalAsync(int pid);
        
        Task<ProblemArchive> FindAsync(int publicId);

        Task<List<ProblemArchive>> ListAsync(int page, int uid);

        Task<int> MaxPageAsync();
    }
}
