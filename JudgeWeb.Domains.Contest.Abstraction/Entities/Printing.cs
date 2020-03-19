using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 打印文件
    /// </summary>
    public class Printing
    {
        /// <summary>
        /// 打印编号
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 提交打印的时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 比赛
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 比赛队伍
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 是否已经处理过
        /// </summary>
        public bool? Done { get; set; }

        /// <summary>
        /// 源代码
        /// </summary>
        public byte[] SourceCode { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 配套工具中对应的语言
        /// </summary>
        public string LanguageId { get; set; }
    }
}
