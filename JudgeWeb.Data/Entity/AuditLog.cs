using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 审计日志
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// 日志编号
        /// </summary>
        [Key]
        public int LogId { get; set; }

        /// <summary>
        /// 发生时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [IsRequired]
        public string UserName { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        [Index]
        public int ContestId { get; set; }

        /// <summary>
        /// 操作的实体类型
        /// </summary>
        [Index]
        public TargetType Type { get; set; }

        /// <summary>
        /// 操作的实体
        /// </summary>
        [Index]
        public int EntityId { get; set; }

        /// <summary>
        /// 操作的注释
        /// </summary>
        [IsRequired]
        public string Comment { get; set; }

        /// <summary>
        /// 是否已经经过解析
        /// </summary>
        [Index]
        public bool Resolved { get; set; }

        /// <summary>
        /// 审计的目标类型
        /// </summary>
        public enum TargetType
        {
            Submission,
            Judging,
            Testcase,
            Problem,
            Contest,
            Clarification,
        }
    }
}
