using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    public static class Instance
    {
        internal static Task InsertAsync<T>(this DbContext db, T item) where T : class
        {
            return db.BulkInsertAsync(new List<T>
            {
                item
            });
        }

        public static IScoreboard[] Scoreboards { get; } = new[]
        {
            new ICPCScoreboard(),
            new ICPCScoreboard(),
            (IScoreboard)new ICPCScoreboard(),
        };
    }
}
