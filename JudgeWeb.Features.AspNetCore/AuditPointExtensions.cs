using JudgeWeb;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{
    public static class AuditPointExtensions
    {
        public static Task AuditAsync(this HttpContext context,
            string action, string? target, string? extra = null)
        {
            if (!context.Items.TryGetValue(nameof(AuditlogType), out object typer))
                return Task.FromException(new InvalidOperationException("No audit point specified."));
            int? cid = null;
            if (context.Items.TryGetValue(nameof(cid), out object cidd))
                cid = (int?)cidd;
            return AuditAsync(context,
                (AuditlogType)typer, cid, context.User.GetUserName() ?? "TOURIST",
                action, target, extra);
        }

        public static Task AuditAsync(this HttpContext context,
            AuditlogType typer, int? cid, string username,
            string action, string? target, string? extra = null)
        {
            return context.RequestServices.GetRequiredService<IAuditlogger>()
                .LogAsync(typer, username, DateTimeOffset.Now, action, target, extra, cid);
        }
    }
}
