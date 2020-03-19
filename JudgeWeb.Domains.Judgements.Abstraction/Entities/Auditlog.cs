using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 审计日志
    /// </summary>
    public class Auditlog
    {
        /// <summary>
        /// 日志编号
        /// </summary>
        public int LogId { get; set; }

        /// <summary>
        /// 发生时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        public int? ContestId { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public AuditlogType DataType { get; set; }

        /// <summary>
        /// 数据目标
        /// </summary>
        public string DataId { get; set; }

        /// <summary>
        /// 动作
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 附加信息
        /// </summary>
        public string ExtraInfo { get; set; }

        public Auditlog() { }
    }

    public enum AuditlogType
    {
        Judgehost,
        TeamAffiliation,
        Team,
        User,
        Contest,
        Problem,
        Testcase,
        Language,
        Scoreboard,
        Clarification,
        Submission,
        Judging,
        Executable,
        Rejudging,
        TeamCategory,
        InternalError,
        News,
        Configuration,
        Printing,
    }
}
