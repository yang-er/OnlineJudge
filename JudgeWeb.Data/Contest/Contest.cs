using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 比赛
    /// </summary>
    public class Contest
    {
        /// <summary>
        /// 比赛编号
        /// </summary>
        [Key]
        public int ContestId { get; set; }

        /// <summary>
        /// 比赛名称
        /// </summary>
        [Property(IsRequired = true)]
        public string Name { get; set; }

        /// <summary>
        /// 比赛短名称
        /// </summary>
        [Property(IsRequired = true)]
        public string ShortName { get; set; }

        /// <summary>
        /// 比赛开始时间
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// 榜冻结时间
        /// </summary>
        public DateTimeOffset? FreezeTime { get; set; }

        /// <summary>
        /// 比赛结束时间
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>
        /// 榜解封时间
        /// </summary>
        public DateTimeOffset? UnfreezeTime { get; set; }

        /// <summary>
        /// 排名方法
        /// </summary>
        public int RankingStrategy { get; set; }

        /// <summary>
        /// 是否公众可见
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// 默认注册分类
        /// </summary>
        public int RegisterDefaultCategory { get; set; }

        /// <summary>
        /// 金牌数量
        /// </summary>
        public int GoldMedal { get; set; }

        /// <summary>
        /// 银牌数量
        /// </summary>
        public int SilverMedal { get; set; }

        /// <summary>
        /// 铜牌数量
        /// </summary>
        public int BronzeMedal { get; set; }
    }
}
