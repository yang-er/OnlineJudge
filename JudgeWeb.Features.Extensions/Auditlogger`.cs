using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb
{
    public interface IAuditlogger
    {
        Task LogAsync(
            AuditlogType type,
            string userName,
            DateTimeOffset now,
            string action,
            string target,
            string extra,
            int? cid);

        Task<(List<Auditlog> model, int totPage)> ViewLogsAsync(
            int? cid,
            int page,
            int pageCount);
    }
}
