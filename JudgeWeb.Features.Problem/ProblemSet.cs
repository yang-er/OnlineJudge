using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace JudgeWeb.Features.Problem
{
    /// <summary>
    /// 对应的问题
    /// </summary>
    public class ProblemSet
    {
        /// <summary>
        /// 问题编号
        /// </summary>
        public int ProblemId { get; set; }

        /// <summary>
        /// 问题名称
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 描述内容
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 输入说明
        /// </summary>
        public string InputHint { get; set; }

        /// <summary>
        /// 输出说明
        /// </summary>
        public string OutputHint { get; set; }

        /// <summary>
        /// 提示与说明
        /// </summary>
        public string HintAndNote { get; set; }

        /// <summary>
        /// 题目来源
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// 内存限制
        /// </summary>
        public int MemoryLimit { get; set; }

        /// <summary>
        /// 运行时间限制
        /// </summary>
        public int ExecuteTimeLimit { get; set; }

        /// <summary>
        /// 评测类型
        /// </summary>
        public string JudgeType { get; set; }

        /// <summary>
        /// 运行脚本
        /// </summary>
        public string RunScript { get; set; }

        /// <summary>
        /// 比较脚本
        /// </summary>
        public string CompareScript { get; set; }

        /// <summary>
        /// 判断答案对错
        /// </summary>
        public IList<TestCase> Samples { get; set; } = new List<TestCase>();

        /// <summary>
        /// 判断答案对错
        /// </summary>
        public IList<TestCase> TestCases { get; set; } = new List<TestCase>();

        /// <summary>
        /// 创建问题的一个空实例。
        /// </summary>
        public ProblemSet() { }

        /// <summary>
        /// 从文件中读取问题。
        /// </summary>
        /// <param name="path">文件地址</param>
        /// <returns>问题</returns>
        public static ProblemSet FromStream(Stream stream, bool judger, bool web)
        {
            var cd = XDocument.Load(stream);
            return cd.ToProblem(web, judger);
        }
    }
}
