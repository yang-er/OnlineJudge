using System.Collections.Generic;
using System.Linq;

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
        public int ProblemId { get; set; }

        /// <summary>
        /// 题目名称
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 题目来源
        /// </summary>
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
        public string RunScript { get; set; }

        /// <summary>
        /// 比较脚本
        /// </summary>
        public string CompareScript { get; set; }

        /// <summary>
        /// 比较参数
        /// </summary>
        public string ComapreArguments { get; set; }

        /// <summary>
        /// 是否运行和比较使用同一个脚本
        /// </summary>
        public bool CombinedRunCompare { get; set; }

        /// <summary>
        /// 提供部分共享
        /// </summary>
        public bool Shared { get; set; }

        /// <summary>
        /// 用于表示存档的内部导航属性
        /// </summary>
        public ProblemArchive Archive { get; set; }
    }
}
