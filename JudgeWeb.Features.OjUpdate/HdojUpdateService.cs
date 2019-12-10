using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace JudgeWeb.Features.OjUpdate
{
    public class HdojUpdateService : OjUpdateService
    {
        public HdojUpdateService(
            ILogger<HdojUpdateService> logger, IServiceProvider serviceProvider)
            : base(logger, serviceProvider, 1, "HDOJ")
        {
            AccountTemplate = "http://acm.hdu.edu.cn/userstatus.php?user={0}";
        }

        public override string RankTemplate(int rk)
        {
            return rk.ToString();
        }

        protected override void ConfigureHttpClient(HttpClient httpClient)
        {
            httpClient.BaseAddress = new Uri("http://acm.hdu.edu.cn/");
            httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        protected override string GenerateGetSource(string account)
        {
            return "userstatus.php?user=" + account;
        }

        protected override int MatchCount(string html)
        {
            var cnt = Regex.Match(html,
                @"<tr><td>Problems Solved</td><td align=center>(\S+)</td></tr>"
            ).Groups[1].Value;

            var success = int.TryParse(cnt, out int ans);
            return success ? ans : -1;
        }
    }
}
