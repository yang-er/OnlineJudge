using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.OjUpdate
{
    public abstract class OjUpdateService : BackgroundService
    {
        private CancellationTokenSource manualCancellatinSource;
        private readonly int sleepToken;
        private bool firstUpdate = true;

        /// <summary>
        /// 评测网站列表
        /// </summary>
        public static Dictionary<string, OjUpdateService> OjList { get; }

        /// <summary>
        /// 初始化字典。
        /// </summary>
        static OjUpdateService()
        {
            OjList = new Dictionary<string, OjUpdateService>();
        }

        /// <summary>
        /// 站点名称
        /// </summary>
        public string SiteName { get; }

        /// <summary>
        /// 等级模板
        /// </summary>
        public abstract string RankTemplate(int rk);

        /// <summary>
        /// 账户地址模板
        /// </summary>
        public string AccountTemplate { get; set; }

        /// <summary>
        /// 查询值
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// 账户列表
        /// </summary>
        public List<OjAccount> NameSet { get; }

        /// <summary>
        /// 日志
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// 是否正在更新
        /// </summary>
        public bool IsUpdating { get; private set; }

        /// <summary>
        /// 上次更新时间
        /// </summary>
        public DateTime LastUpdate { get; private set; }

        /// <summary>
        /// 构造基础刷新服务实例。
        /// </summary>
        /// <param name="logger">日志器</param>
        /// <param name="nameSet">账户列表</param>
        /// <param name="sleepLength">睡眠间隔分钟</param>
        public OjUpdateService(
            ILogger<OjUpdateService> logger,
            List<OjAccount> nameSet,
            int sleepLength,
            string siteName)
        {
            Logger = logger;
            NameSet = nameSet ?? new List<OjAccount>();
            LastUpdate = DateTime.UnixEpoch;
            sleepToken = sleepLength <= 0 ? 44640 : sleepLength;
            SiteName = siteName;
            OjList[siteName] = this;
            ColumnName = "Count";
        }

        /// <summary>
        /// 请求更新
        /// </summary>
        public void RequestUpdate()
        {
            manualCancellatinSource?.Cancel();
        }

        /// <summary>
        /// 获取每年对应的账户。
        /// </summary>
        /// <param name="year">年份</param>
        /// <returns>账户列表</returns>
        public IEnumerable<OjAccount> GetAccounts(int year = -1)
        {
            if (year == -1) return NameSet;
            return NameSet.Where(a => a.Grade == year);
        }

        /// <summary>
        /// 初始化HttpClient，例如设置超时和基地址。
        /// </summary>
        /// <param name="httpClient">HttpClient实例</param>
        protected abstract void ConfigureHttpClient(HttpClient httpClient);

        /// <summary>
        /// 创建GET请求的源地址。
        /// </summary>
        /// <param name="account">账户名</param>
        /// <returns>GET地址</returns>
        protected abstract string GenerateGetSource(string account);

        /// <summary>
        /// 匹配做题数目。
        /// </summary>
        /// <param name="html">读取到的HTML</param>
        /// <returns>做题数目</returns>
        protected abstract int MatchCount(string html);

        /// <summary>
        /// 更新一条记录。
        /// </summary>
        /// <param name="httpClient">HttpClient实例</param>
        /// <param name="id">账户信息</param>
        /// <param name="stoppingToken">提前终止令牌</param>
        protected virtual async Task UpdateOne(HttpClient httpClient, OjAccount id, CancellationToken stoppingToken)
        {
            var getSrc = GenerateGetSource(id.Account);
            var resp = await httpClient.GetAsync(getSrc, stoppingToken);
            var result = await resp.Content.ReadAsStringAsync();
            id.Solved = MatchCount(result);
        }

        /// <summary>
        /// 初始化HttpClientHandler
        /// </summary>
        /// <param name="handler">HttpClientHandler实例</param>
        protected virtual void ConfigureHandler(HttpClientHandler handler)
        {

        }

        /// <summary>
        /// 尝试一次更新操作。
        /// </summary>
        /// <param name="stoppingToken">提前终止的令牌</param>
        private async Task TryUpdateAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    ConfigureHandler(handler);

                    using (var httpClient = new HttpClient())
                    {
                        LastUpdate = DateTime.UnixEpoch;
                        ConfigureHttpClient(httpClient);

                        foreach (var id in NameSet)
                        {
                            await UpdateOne(httpClient, id, stoppingToken);
                        }

                        NameSet.Sort();
                        LastUpdate = DateTime.Now;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Web request timed out.");
            }
            catch (HttpRequestException ex)
            {
                Logger.LogWarning(ex, "Web request timed out.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Something wrong happend unexpectedly.");
            }
        }
        
        /// <summary>
        /// 检查是否可以直接从缓存中读取值。
        /// </summary>
        /// <returns>是否跳过更新，剩余时间</returns>
        private async Task<(bool, int)> CheckCacheAsync()
        {
            var cacheNotHit = (false, sleepToken * 60000);
            var cacheFile = $"ojcache.{SiteName}.json";
            if (!File.Exists(cacheFile)) return cacheNotHit;
            
            var content = await File.ReadAllTextAsync(cacheFile);
            var lastCache = content.AsJson<OjUpdateCache>();
            if (lastCache?.NameSet == null) return cacheNotHit;

            // this hit because of the change of list.
            if (lastCache.NameSet.Count != NameSet.Count) return cacheNotHit;

            var updateGap = DateTime.Now - lastCache.LastUpdate;
            if (updateGap.TotalMinutes > sleepToken) return cacheNotHit;
            
            NameSet.Clear();
            NameSet.AddRange(lastCache.NameSet);
            int awaitTime = cacheNotHit.Item2 - (int)updateGap.TotalMilliseconds;
            if (awaitTime <= 0) awaitTime = 1000;
            LastUpdate = lastCache.LastUpdate;
            return (true, awaitTime);
        }

        /// <summary>
        /// 执行后台任务。
        /// </summary>
        /// <param name="stoppingToken">提前停止令牌</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Fetch service started.");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    IsUpdating = true;
                    manualCancellatinSource = null;
                    bool jumpFromUpdate = false;
                    int sleepLength = sleepToken * 60000;

                    if (firstUpdate)
                    {
                        firstUpdate = false;
                        (jumpFromUpdate, sleepLength) = await CheckCacheAsync();
                        if (jumpFromUpdate) Logger.LogInformation("Cache hit.");
                    }

                    if (!jumpFromUpdate)
                    {
                        Logger.LogInformation("Fetch scope began!");
                        await TryUpdateAsync(stoppingToken);
                        Logger.LogInformation("Fetch scope finished~");

                        var result = new OjUpdateCache
                        {
                            LastUpdate = LastUpdate,
                            NameSet = NameSet
                        };

                        await File.WriteAllTextAsync(
                            $"ojcache.{SiteName}.json",
                            result.ToJson());
                    }

                    // wait for task cancellation or next scope.
                    manualCancellatinSource = new CancellationTokenSource();
                    var chained = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, manualCancellatinSource.Token);
                    IsUpdating = false;
                    await Task.Delay(sleepLength, chained.Token);
                }
                catch (TaskCanceledException)
                {
                    Logger.LogWarning("Fetch timer was interrupted.");
                }
            }
            
            Logger.LogInformation("Fetch service stopped.");
        }
    }
}
