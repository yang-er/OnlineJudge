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

    }
}
