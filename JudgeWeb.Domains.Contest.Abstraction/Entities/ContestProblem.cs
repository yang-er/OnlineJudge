﻿namespace JudgeWeb.Data
{
    /// <summary>
    /// 比赛题目
    /// </summary>
    public class ContestProblem
    {
        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        public int ProblemId { get; set; }

        /// <summary>
        /// 短名称
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// [Ignore] 题目顺序
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 是否允许提交
        /// </summary>
        public bool AllowSubmit { get; set; }

        /// <summary>
        /// [Ignore] 是否允许评测
        /// </summary>
        public bool AllowJudge { get; set; }

        /// <summary>
        /// 气球颜色
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// CF模式题目分数
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// [Ignore] 题目标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// [Ignore] 题目时间限制
        /// </summary>
        public int TimeLimit { get; set; }

        /// <summary>
        /// [Ignore] 题目内存限制
        /// </summary>
        public int MemoryLimit { get; set; }

        /// <summary>
        /// [Ignore] 测试样例组数
        /// </summary>
        public int TestcaseCount { get; set; }

        /// <summary>
        /// [Ignore] 是否为交互题
        /// </summary>
        public bool Interactive { get; set; }

        /// <summary>
        /// [Ignore] 是否共享
        /// </summary>
        public bool Shared { get; set; }

        /// <summary>
        /// 导航属性
        /// </summary>
        public Problem p { get; set; }

        /// <summary>
        /// 导航属性
        /// </summary>
        public Contest c { get; set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ContestProblem() { }

        /// <summary>
        /// 拷贝构造函数
        /// </summary>
        /// <param name="cp">拷贝源</param>
        public ContestProblem(ContestProblem cp, string tit, int time, int mem, bool ia, bool shared, bool alj)
        {
            AllowJudge = alj;
            AllowSubmit = cp.AllowSubmit;
            Color = cp.Color;
            ContestId = cp.ContestId;
            ProblemId = cp.ProblemId;
            Rank = cp.Rank;
            ShortName = cp.ShortName;
            Title = tit;
            TimeLimit = time;
            MemoryLimit = mem;
            Interactive = ia;
            Shared = shared;
            Score = cp.Score;
        }
    }
}
