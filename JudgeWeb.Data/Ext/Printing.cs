using Microsoft.EntityFrameworkCore;
using System;

namespace JudgeWeb.Data.Ext
{
    /// <summary>
    /// 打印文件
    /// </summary>
    public class Printing
    {
        /// <summary>
        /// 打印编号
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 提交打印的时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 比赛
        /// </summary>
        [Index]
        public int ContestId { get; set; }

        /// <summary>
        /// 比赛队伍
        /// </summary>
        [Index]
        public int TeamId { get; set; }

        /// <summary>
        /// 是否已经处理过
        /// </summary>
        public bool? Done { get; set; }

        /// <summary>
        /// 源代码
        /// </summary>
        [IsRequired, MaxLength(65536)]
        public byte[] SourceCode { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        [NonUnicode, MaxLength(256)]
        public string FileName { get; set; }

        /// <summary>
        /// 配套工具中对应的语言
        /// </summary>
        [NonUnicode, MaxLength(10)]
        public string LanguageId { get; set; }
    }
}
