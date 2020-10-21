using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    /// <summary>
    /// 比赛排名规则
    /// </summary>
    public interface IRankingStrategy
    {
        internal static IRankingStrategy[] SC = new IRankingStrategy[]
        {
            new XCPCRank(),
            new CFRank(),
            new OIRank(),
        };

        /// <summary>
        /// 根据排序规则进行排序。
        /// </summary>
        /// <param name="source">原数据</param>
        /// <param name="isPublic">是否按公榜处理</param>
        /// <returns>排序后的结果</returns>
        IEnumerable<Team> SortByRule(IEnumerable<Team> source, bool isPublic);

        /// <summary>
        /// 提交等待评测
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="args">提交事件数据</param>
        Task Pending(DbContext db, SubmissionCreatedRequest args);

        /// <summary>
        /// 提交等待评测
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="args">提交事件数据</param>
        Task CompileError(DbContext db, JudgingFinishedRequest args);

        /// <summary>
        /// 提交等待评测
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="args">提交事件数据</param>
        Task Reject(DbContext db, JudgingFinishedRequest args);

        /// <summary>
        /// 提交等待评测
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="args">提交事件数据</param>
        Task Accept(DbContext db, JudgingFinishedRequest args);

        /// <summary>
        /// 刷新榜单缓存
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="args">请求参数</param>
        Task RefreshCache(DbContext db, RefreshScoreboardCacheRequest args);
    }
}
