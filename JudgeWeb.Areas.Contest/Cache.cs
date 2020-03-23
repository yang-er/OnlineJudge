using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Data
{
    static class ContestCache
    {
        public static readonly AsyncLock _locker = new AsyncLock();


        public static ContestProblem Find(this ContestProblem[] cps, int pid) => cps.FirstOrDefault(cp => cp.ProblemId == pid);

        public static ContestProblem Find(this ContestProblem[] cps, string shortname) => cps.FirstOrDefault(cp => cp.ShortName == shortname);

        
        public static void AddCreate(this DbSet<Event> db, int cid, Api.ContestEventEntity cee)
            => db.Add(cee.ToEvent("create", cid));

        public static void AddCreate(this DbSet<Event> db, int cid, IEnumerable<Api.ContestEventEntity> cees)
            => db.AddRange(cees.Select(cee => cee.ToEvent("create", cid)));

        public static void AddUpdate(this DbSet<Event> db, int cid, Api.ContestEventEntity cee)
            => db.Add(cee.ToEvent("update", cid));

        public static void AddDelete(this DbSet<Event> db, int cid, Api.ContestEventEntity cee)
            => db.Add(cee.ToEvent("delete", cid));
    }
}
