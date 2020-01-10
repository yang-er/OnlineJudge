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
        [Index]
        public int? IssuedBy { get; set; }

        /// <summary>
        /// [Ignore] 操作者
        /// </summary>
        [Ignore]
        public string Issuer { get; set; }

        /// <summary>
        /// 操作员
        /// </summary>
        [Index]
        public int? OperatedBy { get; set; }

        /// <summary>
        /// [Ignore] 操作者
        /// </summary>
        [Ignore]
        public string Operator { get; set; }

        /// <summary>
        /// 是否已应用
        /// </summary>
        public bool? Applied { get; set; }

        /// <summary>
        /// [Ignore] 是否已准备好
        /// </summary>
        [Ignore]
        public (int, int) Ready { get; set; }

        public Rejudge() { }

        public Rejudge(Rejudge r1, string u1, string u2)
        {
            Applied = r1.Applied;
            RejudgeId = r1.RejudgeId;
            ContestId = r1.ContestId;
            IssuedBy = r1.IssuedBy;
            Issuer = u1;
            OperatedBy = r1.OperatedBy;
            Operator = u2;
            StartTime = r1.StartTime;
            EndTime = r1.EndTime;
            Reason = r1.Reason;
        }
    }
}
