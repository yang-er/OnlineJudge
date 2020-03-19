using System;

namespace JudgeWeb.Data
{
    public class Event
    {
        /// <summary>
        /// 事件编号
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// 事件时间
        /// </summary>
        public DateTimeOffset EventTime { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 终结点类型
        /// </summary>
        public string EndPointType { get; set; }

        /// <summary>
        /// 终结点编号
        /// </summary>
        public string EndPointId { get; set; }

        /// <summary>
        /// 终结点操作
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 终结点内容
        /// </summary>
        public byte[] Content { get; set; }
    }
}
