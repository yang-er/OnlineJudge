﻿using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 比赛题目
    /// </summary>
    public class ContestProblem
    {
        /// <summary>
        /// 比赛编号
        /// </summary>
        [Key]
        [HasOneWithMany(typeof(Contest), DeleteBehavior.Cascade)]
        public int ContestId { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        [Key]
        [HasOneWithMany(typeof(Problem), DeleteBehavior.Restrict)]
        public int ProblemId { get; set; }

        /// <summary>
        /// 短名称
        /// </summary>
        [IsRequired]
        [NonUnicode(MaxLength = 10)]
        public string ShortName { get; set; }

        /// <summary>
        /// 题目顺序
        /// </summary>
        [Index]
        public int Rank { get; set; }

        /// <summary>
        /// 是否允许提交
        /// </summary>
        public bool AllowSubmit { get; set; }

        /// <summary>
        /// 是否允许评测
        /// </summary>
        public bool AllowJudge { get; set; }

        /// <summary>
        /// 气球颜色
        /// </summary>
        [IsRequired]
        public string Color { get; set; }

        /// <summary>
        /// [Ignore] 题目标题
        /// </summary>
        [Ignore]
        public string Title { get; set; }

        /// <summary>
        /// [Ignore] 题目时间限制
        /// </summary>
        [Ignore]
        public int TimeLimit { get; set; }

        /// <summary>
        /// [Ignore] 题目内存限制
        /// </summary>
        [Ignore]
        public int MemoryLimit { get; set; }
    }
}
