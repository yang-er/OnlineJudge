using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 提问
    /// </summary>
    public class Clarification
    {
        /// <summary>
        /// 提问编号
        /// </summary>
        public int ClarificationId { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 回应的提问编号
        /// </summary>
        public int? ResponseToId { get; set; }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTimeOffset SubmitTime { get; set; }

        /// <summary>
        /// 发者队伍编号
        /// </summary>
        public int? Sender { get; set; }

        /// <summary>
        /// 收者队伍编号
        /// </summary>
        public int? Recipient { get; set; }

        /// <summary>
        /// 裁判组成员
        /// </summary>
        public string JuryMember { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        public int? ProblemId { get; set; }

        /// <summary>
        /// 提问分类
        /// </summary>
        public TargetCategory Category { get; set; }

        /// <summary>
        /// 提问正文
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 是否已经被回答
        /// </summary>
        public bool Answered { get; set; }

        /// <summary>
        /// [Ignore] 队伍名称
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// 检查是否有权限
        /// </summary>
        /// <param name="teamid">队伍编号</param>
        public bool CheckPermission(int teamid)
        {
            return !Recipient.HasValue || Recipient == teamid || Sender == teamid;
        }

        /// <summary>
        /// 提问分类
        /// </summary>
        public enum TargetCategory
        {
            General,
            Technical,
            Problem
        }
    }
}
