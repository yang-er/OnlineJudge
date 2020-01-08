using Microsoft.EntityFrameworkCore;
using System;

namespace JudgeWeb.Data
{
    public class Event
    {
        /// <summary>
        /// 事件编号
        /// </summary>
        [Key]
        public int EventId { get; set; }

        /// <summary>
        /// 事件时间
        /// </summary>
        [Index]
        public DateTimeOffset EventTime { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(Contest), DeleteBehavior.Cascade)]
        public int ContestId { get; set; }

        /// <summary>
        /// 终结点类型
        /// </summary>
        [IsRequired]
        [NonUnicode(MaxLength = 32)]
        public string EndPointType { get; set; }

        /// <summary>
        /// 终结点编号
        /// </summary>
        [IsRequired]
        public string EndPointId { get; set; }

        /// <summary>
        /// 终结点操作
        /// </summary>
        [IsRequired]
        [NonUnicode(MaxLength = 6)]
        public string Action { get; set; }

        /// <summary>
        /// 终结点内容
        /// </summary>
        [IsRequired]
        [MaxLength(2048)]
        public byte[] Content { get; set; }
    }
}
