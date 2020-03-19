using System;
using System.Collections.Generic;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 比赛队伍
    /// </summary>
    public class Team
    {
        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 队伍编号
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        /// 队伍名称
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// 归属组织编号
        /// </summary>
        public int AffiliationId { get; set; }

        /// <summary>
        /// 比赛分类编号
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// 队伍状态，0为挂起，1为通过，2为拒绝，3为已删除
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTimeOffset? RegisterTime { get; set; }

        /// <summary>
        /// 队伍位置
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// 归属组织
        /// </summary>
        public TeamAffiliation Affiliation { get; }

        /// <summary>
        /// 排名缓存
        /// </summary>
        public ICollection<RankCache> RankCache { get; set; }

        /// <summary>
        /// 分数缓存
        /// </summary>
        public ICollection<ScoreCache> ScoreCache { get; set; }
    }
}
