using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 题目集与编号
    /// </summary>
    public class ProblemArchive
    {
        /// <summary>
        /// 公开的题目ID
        /// </summary>
        [Key(ValueGeneratedNever = true)]
        public int PublicId { get; set; }

        /// <summary>
        /// 内部题目ID
        /// </summary>
        [Index]
        [HasOneWithMany(typeof(Problem), DeleteBehavior.Restrict)]
        public int ProblemId { get; set; }

        /// <summary>
        /// 标签名，以半角逗号分隔
        /// </summary>
        [IsRequired]
        public string TagName { get; set; }
    }
}
