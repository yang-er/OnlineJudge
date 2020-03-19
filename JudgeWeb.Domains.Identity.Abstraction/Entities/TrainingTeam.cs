using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 训练队伍
    /// </summary>
    public class TrainingTeam
    {
        /// <summary>
        /// 队伍编号
        /// </summary>
        public int TrainingTeamId { get; set; }

        /// <summary>
        /// 队伍名称
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// 学校
        /// </summary>
        public int AffiliationId { get; set; }

        /// <summary>
        /// 队伍创建者
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 成立时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 导航属性
        /// </summary>
        public TeamAffiliation Affiliation { get; set; }
    }
}
