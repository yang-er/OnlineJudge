using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Features
{
    public class Auditlogger : IAuditlogger
    {
        private DbContext Context { get; }

        private DbSet<Auditlog> Auditlogs => Context.Set<Auditlog>();

        public Auditlogger(AppDbContext context)
        {
            Context = context;
        }

        public Task LogAsync(
            AuditlogType type,
            string userName,
            DateTimeOffset now,
            string action,
            string target,
            string extra,
            int? cid)
        {
            Auditlogs.Add(new Auditlog
            {
                Action = action,
                Time = now,
                DataId = target,
                DataType = type,
                ContestId = cid,
                ExtraInfo = extra,
                UserName = userName,
            });

            return Context.SaveChangesAsync();
        }

        public async Task<(
            List<Auditlog> model,
            int totPage)>
            ViewLogsAsync(int? cid, int page, int pageCount)
        {
            var count = await Auditlogs
                .Where(a => a.ContestId == cid)
                .CountAsync();

            var query = await Auditlogs
                .Where(a => a.ContestId == cid)
                .OrderByDescending(a => a.LogId)
                .Skip((page - 1) * pageCount)
                .Take(pageCount)
                .ToListAsync();

            return (query, (count - 1) / pageCount + 1);
        }
    }
}
