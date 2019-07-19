using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace JudgeWeb.Features.OjUpdate
{
    public class CfUpdateOptions : OjUpdateOptions { }

    public class CfUpdateService : OjUpdateService
    {
        public CfUpdateService(
            ILogger<CfUpdateService> logger,
            IOptions<CfUpdateOptions> options)
            : base(logger, options.Value.NameSet, options.Value.SleepMinute, "Codeforces")
        {
            AccountTemplate = "https://codeforces.com/profile/{0}";
            ColumnName = "Rating";
        }

        public override string RankTemplate(int rk)
        {
            if (rk == -51) return "N/A";
            if (rk == -50) return "Unrated";
            if (rk < 1200) return $"<b><font color=\"#808080\">{rk}</font></b>";
            if (rk < 1400) return $"<b><font color=\"#008000\">{rk}</font></b>";
            if (rk < 1600) return $"<b><font color=\"#03a89e\">{rk}</font></b>";
            if (rk < 1900) return $"<b><font color=\"#0000ff\">{rk}</font></b>";
            if (rk < 2100) return $"<b><font color=\"#a0a\">{rk}</font></b>";
            if (rk < 2300) return $"<b><font color=\"#ff8c00\">{rk}</font></b>";
            if (rk < 2600) return $"<b><font color=\"#ff0000\">{rk}</font></b>";
            if (rk < 3000) return $"<b><font color=\"#dd0000\">{rk}</font></b>";
            return $"<b><font color=\"#aa0000\">{rk}</font></b>";
        }

        protected override void ConfigureHttpClient(HttpClient httpClient)
        {
            httpClient.BaseAddress = new Uri("https://codeforces.com/");
            httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        protected override string GenerateGetSource(string account)
        {
            return "profile/" + account;
        }

        protected override int MatchCount(string html)
        {
            var cnt = Regex.Match(html,
                @"Contest rating:(\s+)<span style=""font-weight:bold;"" class=""user-(\S+)"">(\S+)</span>"
            ).Groups[3].Value;

            var success = int.TryParse(cnt, out int ans);
            return success ? ans :
                   html.Contains("<span class=\"user-black\">Unrated </span>")
                   ? -50 : -51;
        }
    }
}
