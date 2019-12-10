using Microsoft.EntityFrameworkCore;

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
        [Key]
        public int TestId { get; set; }

        /// <summary>
        /// 测试结果
        /// </summary>
        public Verdict Status { get; set; }

        /// <summary>
        /// 评测编号
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(Judging), DeleteBehavior.Cascade)]
        public int JudgingId { get; set; }

        /// <summary>
        /// 测试样例编号
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(Testcase), DeleteBehavior.Restrict)]
        public int TestcaseId { get; set; }

        /// <summary>
        /// 执行内存，以kb为单位
        /// </summary>
        public int ExecuteMemory { get; set; }

        /// <summary>
        /// 执行时间，以ms为单位
        /// </summary>
        public int ExecuteTime { get; set; }

        /*
        /// <summary>
        /// 交互输入大小
        /// </summary>
        public int StdinBytes { get; set; }

        /// <summary>
        /// 标准输出大小
        /// </summary>
        public int StdoutBytes { get; set; }

        /// <summary>
        /// 标准错误输出大小
        /// </summary>
        public int StderrBytes { get; set; }

        /// <summary>
        /// 退出代码
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// 是否爆了WallTime
        /// </summary>
        public bool BreakingWallTime { get; set; }
        */

        /// <summary>
        /// 系统输出，以BASE64编码
        /// </summary>
        [NonUnicode(MaxLength = 131072)]
        public string OutputSystem { get; set; }

        /// <summary>
        /// 比较脚本输出，以BASE64编码
        /// </summary>
        [NonUnicode(MaxLength = 131072)]
        public string OutputDiff { get; set; }
    }
}
