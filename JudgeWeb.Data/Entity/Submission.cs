﻿using Microsoft.EntityFrameworkCore;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 提交
    /// </summary>
    public class Submission
    {
        /// <summary>
        /// 提交编号
        /// </summary>
        [Key]
        public int SubmissionId { get; set; }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 比赛编号，为0时是普通提交
        /// </summary>
        [Index]
        public int ContestId { get; set; }

        /// <summary>
        /// 提交作者，训练是用户ID，比赛为队伍ID
        /// </summary>
        [Index]
        public int Author { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(Problem), DeleteBehavior.Restrict)]
        public int ProblemId { get; set; }

        /// <summary>
        /// 源代码，BASE64
        /// </summary>
        [Property(IsRequired = true, IsUnicode = false, MaxLength = 131072)]
        public string SourceCode { get; set; }

        /// <summary>
        /// 代码长度
        /// </summary>
        public int CodeLength { get; set; }

        /// <summary>
        /// 编程语言
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(Language), DeleteBehavior.Restrict)]
        public int Language { get; set; }

        /// <summary>
        /// 提交时用户IP地址
        /// </summary>
        [Property(IsRequired = true, IsUnicode = false, MaxLength = 128)]
        public string Ip { get; set; }
    }
}