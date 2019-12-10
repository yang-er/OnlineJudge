using Microsoft.EntityFrameworkCore;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 重判请求
    /// </summary>
    public class Rejudge
    {
        /// <summary>
        /// 重判编号
        /// </summary>
        [Key]
        public int RejudgeId { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        [Index]
        public int ContestId { get; set; }

        /// <summary>
        /// 重判原因
        /// </summary>
        [IsRequired]
        public string Reason { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>
        /// 操作员
        /// </summary>
        [HasOneWithMany(typeof(User), DeleteBehavior.SetNull)]
        public int? OperatedBy { get; set; }

        /// <summary>
        /// 是否已应用
        /// </summary>
        public bool? Applied { get; set; }
    }
}
