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

        /// <summary>
        /// AC提交数
        /// </summary>
        public int Accepted { get; set; }

        /// <summary>
        /// 总提交数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 题目名称
        /// </summary>
        [Ignore]
        public string Title { get; set; }

        /// <summary>
        /// 题目来源
        /// </summary>
        [Ignore]
        public string Source { get; set; }

        public ProblemArchive() { }

        public ProblemArchive(ProblemArchive src, string title, string source)
        {
            Title = title;
            Source = source;
            TagName = src.TagName;
            PublicId = src.PublicId;
            Accepted = src.Accepted;
            Total = src.Total;
            ProblemId = src.ProblemId;
        }
    }
}
