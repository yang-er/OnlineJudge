using Microsoft.EntityFrameworkCore;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 提交
    /// </summary>
    public class Submission
    {
        /// <summary>
        /// 提交编号
        /// </summary>
        [Key]
        public int SubmissionId { get; set; }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 比赛编号，为0时是普通提交
        /// </summary>
        [Index]
        public int ContestId { get; set; }

        /// <summary>
        /// 提交作者，训练是用户ID，比赛为队伍ID
        /// </summary>
        [Index]
        public int Author { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(Problem), DeleteBehavior.Cascade)]
        public int ProblemId { get; set; }

        /// <summary>
        /// 源代码，BASE64
        /// </summary>
        [IsRequired]
        [NonUnicode(MaxLength = 131072)]
        public string SourceCode { get; set; }

        /// <summary>
        /// 代码长度
        /// </summary>
        public int CodeLength { get; set; }

        /// <summary>
        /// 编程语言
        /// </summary>
        [HasOneWithMany(typeof(Language), DeleteBehavior.Restrict)]
        [IsRequired]
        [NonUnicode(MaxLength = 16)]
        public string Language { get; set; }

        /// <summary>
        /// 提交时用户IP地址
        /// </summary>
        [IsRequired]
        [NonUnicode(MaxLength = 128)]
        public string Ip { get; set; }

        /// <summary>
        /// 期望的验题结果
        /// </summary>
        public Verdict? ExpectedResult { get; set; }

        /// <summary>
        /// 重测请求编号
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(Rejudge), DeleteBehavior.SetNull)]
        public int? RejudgeId { get; set; }
    }
}
