using JudgeWeb.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.OjUpdate
{
    public class PojUpdateService : OjUpdateService
    {
        public PojUpdateService(
            ILogger<PojUpdateService> logger, IServiceProvider serviceProvider)
            : base(logger, serviceProvider, 3, "POJ")
        {
            AccountTemplate = "http://poj.org/userstatus?user_id={0}";
        }

        public override string RankTemplate(int rk)
        {
            return rk.ToString();
        }

        protected override void ConfigureHandler(HttpClientHandler handler)
        {
            handler.CookieContainer = new System.Net.CookieContainer();
            handler.UseCookies = true;
        }

        protected override void ConfigureHttpClient(HttpClient httpClient)
        {
            httpClient.BaseAddress = new Uri("http://poj.org/");
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/64.0.3282.140 Safari/537.36 Edge/18.17763");
        }

        protected override string GenerateGetSource(string account)
        {
            return "userstatus?user_id=" + account;
        }

        protected override int MatchCount(string html)
        {
            const string stt = "<td width=15% align=left>Solved:</td>\r\n" +
                "<td align=center width=25%><a href=status?result=0&user_id=";
            var sst = html.IndexOf(stt);
            // if (sst == -1) Logger.LogInformation(html);
            if (sst == -1) return -1;

            var gt = html.IndexOf('>', sst + stt.Length) + 1;
            var lt = html.IndexOf('<', gt);
            var cnt = html.Substring(gt, lt - gt);
            var success = int.TryParse(cnt, out int ans);
            return success ? ans : -1;
        }

        protected override async Task UpdateOne(HttpClient httpClient,
            PersonRank id, CancellationToken stoppingToken)
        {
            await base.UpdateOne(httpClient, id, stoppingToken);
            await Task.Delay(10000, stoppingToken);
        }
    }
}
