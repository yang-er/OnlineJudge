using Microsoft.EntityFrameworkCore;
using System;

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
        [Key]
        [HasOneWithMany(typeof(Contest), DeleteBehavior.Cascade)]
        public int ContestId { get; set; }

        /// <summary>
        /// 队伍编号
        /// </summary>
        [Key]
        public int TeamId { get; set; }

        /// <summary>
        /// 用户编号
        /// </summary>
        [Index]
        public int UserId { get; set; }

        /// <summary>
        /// 队伍名称
        /// </summary>
        [Property(IsRequired = true, MaxLength = 128)]
        public string TeamName { get; set; }

        /// <summary>
        /// 归属组织编号
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(TeamAffiliation), DeleteBehavior.Restrict)]
        public int AffiliationId { get; set; }

        /// <summary>
        /// 比赛分类编号
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(TeamCategory), DeleteBehavior.Restrict)]
        public int CategoryId { get; set; }

        /// <summary>
        /// 队伍状态，0为挂起，1为通过，2为拒绝
        /// </summary>
        [Index]
        public int Status { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime? RegisterTime { get; set; }
    }
}
