﻿using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 编程语言
    /// </summary>
    public class Language
    {
        /// <summary>
        /// 语言编号
        /// </summary>
        [Key]
        public int LangId { get; set; }

        /// <summary>
        /// 语言外部编号
        /// </summary>
        [Index]
        [IsRequired]
        [NonUnicode(MaxLength = 16)]
        public string ExternalId { get; set; }

        /// <summary>
        /// 语言正式名称
        /// </summary>
        [IsRequired]
        [NonUnicode]
        public string Name { get; set; }

        /// <summary>
        /// 保存的文件后缀名
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// 是否允许提交
        /// </summary>
        public bool AllowSubmit { get; set; }

        /// <summary>
        /// 是否允许评测
        /// </summary>
        public bool AllowJudge { get; set; }

        /// <summary>
        /// 时间倍数
        /// </summary>
        public double TimeFactor { get; set; }

        /// <summary>
        /// 编译脚本
        /// </summary>
        [IsRequired]
        [NonUnicode(MaxLength = 64)]
        [HasOneWithMany(typeof(Executable), DeleteBehavior.Restrict)]
        public string CompileScript { get; set; }
    }
}
