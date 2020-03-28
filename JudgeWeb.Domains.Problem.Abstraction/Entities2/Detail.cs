using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 测试细节
    /// </summary>
    public class Detail
    {
        /// <summary>
        /// 测试编号
        /// </summary>
        public int TestId { get; set; }

        /// <summary>
        /// 测试结果
        /// </summary>
        public Verdict Status { get; set; }

        /// <summary>
        /// 评测编号
        /// </summary>
        public int JudgingId { get; set; }

        /// <summary>
        /// 测试样例编号
        /// </summary>
        public int TestcaseId { get; set; }

        /// <summary>
        /// 执行内存，以kb为单位
        /// </summary>
        public int ExecuteMemory { get; set; }

        /// <summary>
        /// 执行时间，以ms为单位
        /// </summary>
        public int ExecuteTime { get; set; }

        /// <summary>
        /// 完成此测试点时间
        /// </summary>
        public DateTimeOffset CompleteTime { get; set; }

        /// <summary>
        /// 其他评测信息
        /// </summary>
        public string MetaData { get; set; }

        /// <summary>
        /// 系统输出，以BASE64编码
        /// </summary>
        public string OutputSystem { get; set; }

        /// <summary>
        /// 比较脚本输出，以BASE64编码
        /// </summary>
        public string OutputDiff { get; set; }

        /// <summary>
        /// 导航属性
        /// </summary>
        public Judging j { get; set; }
    }
}
