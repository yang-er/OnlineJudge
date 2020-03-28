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

        IQueryable<Event> Query(int cid);
    }
}
