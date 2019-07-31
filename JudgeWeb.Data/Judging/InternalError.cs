using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 评测机错误
    /// </summary>
    public class InternalError
    {
        /// <summary>
        /// 错误编号
        /// </summary>
        [Key]
        public int ErrorId { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        public int? ContestId { get; set; }

        /// <summary>
        /// 评测编号
        /// </summary>
        public int? JudgingId { get; set; }

        /// <summary>
        /// 错误描述，BASE64
        /// </summary>
        [IsRequired]
        public string Description { get; set; }

        /// <summary>
        /// 评测机日志，BASE64
        /// </summary>
        [IsRequired]
        public string JudgehostLog { get; set; }

        /// <summary>
        /// 查看时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 禁用的对象
        /// </summary>
        [IsRequired]
        public string Disabled { get; set; }

        /// <summary>
        /// 错误状态
        /// </summary>
        [Index]
        public ErrorStatus Status { get; set; }

        /// <summary>
        /// 错误状态
        /// </summary>
        public enum ErrorStatus
        {
            /// <summary>
            /// 未解决
            /// </summary>
            Open,

            /// <summary>
            /// 已解决
            /// </summary>
            Resolved,

            /// <summary>
            /// 已忽略
            /// </summary>
            Ignored
        }
    }
}
