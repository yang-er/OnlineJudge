using JudgeWeb.Data;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public interface IContestEventNotifier
    {
        Task Update(int cid, Judging judging);

        Task Create(int cid, Detail detail);

        Task Update(int cid, Contest contest);

        Task Update(int cid, Contest contest, ContestState state);

        Task Delete(int cid, ContestProblem problem);

        Task Update(int cid, ContestProblem problem);

        Task Create(int cid, ContestProblem problem);

        Task ResetAsync(int cid);

        IQueryable<Event> Query(int cid);
    }
}
