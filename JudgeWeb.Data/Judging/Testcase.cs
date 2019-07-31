using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 测试用例
    /// </summary>
    public class Testcase
    {
        /// <summary>
        /// 用例编号
        /// </summary>
        [Key]
        public int TestcaseId { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(Problem), DeleteBehavior.Restrict)]
        public int ProblemId { get; set; }

        /// <summary>
        /// 是否为保密数据
        /// </summary>
        public bool IsSecret { get; set; }

        /// <summary>
        /// 输入的MD5
        /// </summary>
        [IsRequired]
        [NonUnicode(MaxLength = 32)]
        public string Md5sumInput { get; set; }

        /// <summary>
        /// 输出的MD5
        /// </summary>
        [IsRequired]
        [NonUnicode(MaxLength = 32)]
        public string Md5sumOutput { get; set; }

        /// <summary>
        /// 输入内容，不大于32MB
        /// </summary>
        [IsRequired]
        [MaxLength(1 << 25)]
        public byte[] Input { get; set; }

        /// <summary>
        /// 输入长度
        /// </summary>
        public int InputLength { get; set; }

        /// <summary>
        /// 输出内容，不大于4MB
        /// </summary>
        [IsRequired]
        [MaxLength(1 << 22)]
        public byte[] Output { get; set; }

        /// <summary>
        /// 输出长度
        /// </summary>
        public int OutputLength { get; set; }

        /// <summary>
        /// 用例顺序
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 测试点分数
        /// </summary>
        public int Point { get; set; }

        /// <summary>
        /// 测试点描述
        /// </summary>
        [IsRequired]
        [MaxLength(1 << 9)]
        public string Description { get; set; }
    }
}
