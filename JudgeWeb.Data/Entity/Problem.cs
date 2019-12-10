﻿using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 题目
    /// </summary>
    public class Problem
    {
        /// <summary>
        /// 题目编号
        /// </summary>
        [Key]
        public int ProblemId { get; set; }

        /// <summary>
        /// 题目名称
        /// </summary>
        [IsRequired]
        [MaxLength(128)]
        public string Title { get; set; }

        /// <summary>
        /// 题目来源
        /// </summary>
        [IsRequired]
        [MaxLength(256)]
        public string Source { get; set; }

        /// <summary>
        /// 允许评测
        /// </summary>
        public bool AllowJudge { get; set; }

        /// <summary>
        /// 允许提交
        /// </summary>
        public bool AllowSubmit { get; set; }

        /// <summary>
        /// 题目信息的标志
        /// </summary>
        public int Flag { get; set; }

        /// <summary>
        /// 时间限制，以ms为单位
        /// </summary>
        public int TimeLimit { get; set; }

        /// <summary>
        /// 内存限制，以kb为单位
        /// </summary>
        public int MemoryLimit { get; set; } = 524288;

        /// <summary>
        /// 输出限制，以kb为单位
        /// </summary>
        public int OutputLimit { get; set; } = 4096;

        /// <summary>
        /// 运行脚本
        /// </summary>
        [IsRequired]
        [NonUnicode(MaxLength = 64)]
        [HasOneWithMany(typeof(Executable), DeleteBehavior.Restrict)]
        public string RunScript { get; set; }

        /// <summary>
        /// 比较脚本
        /// </summary>
        [IsRequired]
        [NonUnicode(MaxLength = 64)]
        [HasOneWithMany(typeof(Executable), DeleteBehavior.Restrict)]
        public string CompareScript { get; set; }

        /// <summary>
        /// 比较参数
        /// </summary>
        [NonUnicode(MaxLength = 128)]
        public string ComapreArguments { get; set; }

        /// <summary>
        /// 是否运行和比较使用同一个脚本
        /// </summary>
        public bool CombinedRunCompare { get; set; }
    }
}
