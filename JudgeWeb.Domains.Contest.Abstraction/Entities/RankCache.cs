namespace JudgeWeb.Data
{
    /// <summary>
    /// 排名缓存
    /// </summary>
    public class RankCache
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
        /// 内榜分数
        /// </summary>
        public int PointsRestricted { get; set; }

        /// <summary>
        /// 内榜罚时
        /// </summary>
        public int TotalTimeRestricted { get; set; }

        /// <summary>
        /// 外榜分数
        /// </summary>
        public int PointsPublic { get; set; }

        /// <summary>
        /// 外榜罚时
        /// </summary>
        public int TotalTimePublic { get; set; }
    }
}
