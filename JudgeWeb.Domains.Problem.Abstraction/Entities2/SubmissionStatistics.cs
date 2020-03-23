namespace JudgeWeb.Data
{
    /// <summary>
    /// 提交统计信息
    /// </summary>
    public class SubmissionStatistics
    {
        /// <summary>
        /// 总提交数
        /// </summary>
        public int TotalSubmission { get; set; }

        /// <summary>
        /// AC提交数
        /// </summary>
        public int AcceptedSubmission { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        public int ProblemId { get; set; }

        /// <summary>
        /// 作者编号
        /// </summary>
        public int Author { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }
    }
}
