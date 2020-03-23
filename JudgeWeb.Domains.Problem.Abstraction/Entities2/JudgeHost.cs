using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 测评机信息
    /// </summary>
    public class JudgeHost
    {
        /// <summary>
        /// 服务器名称
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// 是否允许评测
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// 上次查询时间
        /// </summary>
        public DateTimeOffset PollTime { get; set; }
    }
}
