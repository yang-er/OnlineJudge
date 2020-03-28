using JudgeWeb.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Scoreboard
{
    /// <summary>
    /// 比赛排名规则
    /// </summary>
    public interface IRankingStrategy
    {
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
        Task Pending(ScoreboardContext db, ScoreboardEventArgs args);

        /// <summary>
        /// 提交等待评测
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="args">提交事件数据</param>
        Task CompileError(ScoreboardContext db, ScoreboardEventArgs args);

        /// <summary>
        /// 提交等待评测
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="args">提交事件数据</param>
        Task Reject(ScoreboardContext db, ScoreboardEventArgs args);

        /// <summary>
        /// 提交等待评测
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="args">提交事件数据</param>
        Task Accept(ScoreboardContext db, ScoreboardEventArgs args);

        /// <summary>
        /// 刷新榜单缓存
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="contest">比赛</param>
        Task RefreshCache(ScoreboardContext db, ScoreboardEventArgs args);
    }
}
