namespace JudgeWeb.Data
{
    /// <summary>
    /// 分数缓存
    /// </summary>
    public class ScoreCache
    {
        /// <summary>
        /// 比赛编号
        /// </summary>
        [Key]
        public int ContestId { get; set; }

        /// <summary>
        /// 队伍编号
        /// </summary>
        [Key]
        public int TeamId { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        [Key]
        public int ProblemId { get; set; }

        /// <summary>
        /// 内榜提交数
        /// </summary>
        public int SubmissionRestricted { get; set; }

        /// <summary>
        /// 内榜挂起数
        /// </summary>
        public int PendingRestricted { get; set; }

        /// <summary>
        /// 内榜解决时间
        /// </summary>
        public double SolveTimeRestricted { get; set; }

        /// <summary>
        /// 内榜是否正确
        /// </summary>
        public bool IsCorrectRestricted { get; set; }

        /// <summary>
        /// 公榜提交数
        /// </summary>
        public int SubmissionPublic { get; set; }

        /// <summary>
        /// 公榜挂起数
        /// </summary>
        public int PendingPublic { get; set; }

        /// <summary>
        /// 公榜解决时间
        /// </summary>
        public double SolveTimePublic { get; set; }

        /// <summary>
        /// 公榜是否正确
        /// </summary>
        public bool IsCorrectPublic { get; set; }

        /// <summary>
        /// 是否是该SortOrder内第一个解决该题的人
        /// </summary>
        public bool FirstToSolve { get; set; }
    }
}
