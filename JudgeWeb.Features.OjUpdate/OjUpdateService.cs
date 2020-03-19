using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Features.OjUpdate
{
    public abstract class OjUpdateService : BackgroundService
    {
        private CancellationTokenSource manualCancellatinSource;
        private bool firstUpdate = true;

        /// <summary>
        /// 评测网站列表
        /// </summary>
        public static Dictionary<string, OjUpdateService> OjList { get; }

        /// <summary>
        /// 睡眠间隔时长
        /// </summary>
        public static int SleepLength { get; set; }

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
        /// 分类编号，对应数据库
        /// </summary>
        public int CategoryId { get; }

        /// <summary>
        /// 服务提供程序
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

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
        public DateTimeOffset? LastUpdate { get; private set; }

        /// <summary>
        /// 构造基础刷新服务实例。
        /// </summary>
        /// <param name="logger">日志器</param>
        /// <param name="catId">分类编号</param>
        /// <param name="serviceProvider">服务提供程序</param>
        /// <param name="siteName">站点名称</param>
        protected OjUpdateService(
            ILogger<OjUpdateService> logger,
            IServiceProvider serviceProvider,
            int catId,
            string siteName)
        {
            Logger = logger;
            SiteName = siteName;
            OjList[siteName] = this;
            CategoryId = catId;
            ServiceProvider = serviceProvider;
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
        protected virtual async Task UpdateOne(HttpClient httpClient, PersonRank id, CancellationToken stoppingToken)
        {
            var getSrc = GenerateGetSource(id.Account);
            var resp = await httpClient.GetAsync(getSrc, stoppingToken);
            var result = await resp.Content.ReadAsStringAsync();
            id.Result = MatchCount(result);
        }

        /// <summary>
        /// 初始化HttpClientHandler
        /// </summary>
        /// <param name="handler">HttpClientHandler实例</param>
        protected virtual void ConfigureHandler(HttpClientHandler handler)
        {

        }

        /// <summary>
        /// 初始化服务同时加载上次更新时间。
        /// </summary>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);

            using var scope = ServiceProvider.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<IDbContextHolder>();
            var confName = $"oj_{CategoryId}_update_time";
            var conf = await db.Configures
                .Where(c => c.Name == confName)
                .FirstOrDefaultAsync();

            if (conf == null)
            {
                var cnf = db.Configures.Add(new Configure
                {
                    Name = confName,
                    Description = $"The last update time of {SiteName}.",
                    Public = -1,
                    Value = "null",
                    Type = "datetime",
                    Category = "Internal",
                });

                await db.SaveChangesAsync();
                conf = cnf.Entity;
            }

            LastUpdate = conf.Value.AsJson<DateTimeOffset?>();
        }

        /// <summary>
        /// 将上次结束的时间储存起来。
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);

            using var scope = ServiceProvider.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<IDbContextHolder>();
            var confName = $"oj_{CategoryId}_update_time";
            var confValue = LastUpdate.ToJson();
            var conf = await db.Configures
                .Where(c => c.Name == confName)
                .BatchUpdateAsync(c => new Configure { Value = confValue });
        }

        /// <summary>
        /// 尝试一次更新操作。
        /// </summary>
        /// <param name="stoppingToken">提前终止的令牌</param>
        private async Task TryUpdateAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var handler = new HttpClientHandler();
                ConfigureHandler(handler);

                using var httpClient = new HttpClient();
                LastUpdate = null;
                ConfigureHttpClient(httpClient);

                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider
                        .GetRequiredService<IDbContextHolder>();
                    int category = CategoryId;

                    var names = await dbContext.PersonRanks
                        .Where(r => r.Category == category)
                        .ToListAsync();

                    foreach (var id in names)
                    {
                        await UpdateOne(httpClient, id, stoppingToken);
                        dbContext.PersonRanks.Update(id);
                        await dbContext.SaveChangesAsync();
                    }
                }

                LastUpdate = DateTimeOffset.Now;
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
        /// 执行后台任务。
        /// </summary>
        /// <param name="stoppingToken">提前停止令牌</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogDebug("Fetch service started.");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    IsUpdating = true;
                    manualCancellatinSource = null;
                    bool jumpFromUpdate = false;
                    int sleepLength = SleepLength * 60000;

                    if (firstUpdate)
                    {
                        firstUpdate = false;
                        jumpFromUpdate = true;
                    }

                    if (!jumpFromUpdate)
                    {
                        Logger.LogInformation("Fetch scope began!");
                        await TryUpdateAsync(stoppingToken);
                        Logger.LogInformation("Fetch scope finished~");
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
            
            Logger.LogDebug("Fetch service stopped.");
        }
    }
}
