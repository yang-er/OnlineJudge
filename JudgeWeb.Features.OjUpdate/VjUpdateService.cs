using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace JudgeWeb.Features.OjUpdate
{
    public class VjUpdateOptions : OjUpdateOptions { }

    public class VjUpdateService : OjUpdateService
    {
        public VjUpdateService(
            ILogger<VjUpdateService> logger,
            IOptions<VjUpdateOptions> options)
            : base(logger, options.Value.NameSet, options.Value.SleepMinute, "Vjudge")
        {
            AccountTemplate = "https://vjudge.net/user/{0}";
        }

        public override string RankTemplate(int rk)
        {
            return rk.ToString();
        }

        protected override void ConfigureHttpClient(HttpClient httpClient)
        {
            httpClient.BaseAddress = new Uri("https://vjudge.net/");
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/64.0.3282.140 Safari/537.36 Edge/18.17763");
            httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        protected override string GenerateGetSource(string account)
        {
            return "user/" + account;
        }

        protected override int MatchCount(string html)
        {
            var cnt = Regex.Match(html,
                @"title=""Overall solved"" target=""_blank"">(\S+)</a>"
            ).Groups[1].Value;
            
            var success = int.TryParse(cnt, out int ans);
            return success ? ans : -1;
        }
    }
}
