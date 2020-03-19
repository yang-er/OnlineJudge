﻿using System;
using System.Collections.Generic;

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
        public int SubmissionId { get; set; }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 比赛编号，为0时是普通提交
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 提交作者，训练是用户ID，比赛为队伍ID
        /// </summary>
        public int Author { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        public int ProblemId { get; set; }

        /// <summary>
        /// 源代码，BASE64
        /// </summary>
        public string SourceCode { get; set; }

        /// <summary>
        /// 代码长度
        /// </summary>
        public int CodeLength { get; set; }

        /// <summary>
        /// 编程语言
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// 提交时用户IP地址
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// 期望的验题结果
        /// </summary>
        public Verdict? ExpectedResult { get; set; }

        /// <summary>
        /// 重测请求编号
        /// </summary>
        public int? RejudgeId { get; set; }

        /// <summary>
        /// 到评测信息的导航属性
        /// </summary>
        public ICollection<Judging> Judgings { get; set; }
    }
}
