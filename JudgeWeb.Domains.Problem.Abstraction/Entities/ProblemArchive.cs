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
        public int PublicId { get; set; }

        /// <summary>
        /// 内部题目ID
        /// </summary>
        public int ProblemId { get; set; }

        /// <summary>
        /// 标签名，以半角逗号分隔
        /// </summary>
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
        /// [Ignore] 题目名称
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// [Ignore] 题目来源
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// [Ignore] 提交后是否AC
        /// </summary>
        public bool? Submitted { get; set; }

        /// <summary>
        /// [Ignore] 是否允许提交
        /// </summary>
        public bool? AllowSubmit { get; set; }

        public ProblemArchive() { }

        public ProblemArchive(ProblemArchive src, string title, string source, int? ac, int? su)
        {
            Title = title;
            Source = source;
            TagName = src.TagName;
            PublicId = src.PublicId;
            Accepted = src.Accepted;
            Total = src.Total;
            ProblemId = src.ProblemId;
            if (su.HasValue) Submitted = (ac ?? 0) > 0;
        }

        public ProblemArchive(ProblemArchive src, string title, string source, bool allowSubmit)
        {
            Title = title;
            Source = source;
            TagName = src.TagName;
            PublicId = src.PublicId;
            Accepted = src.Accepted;
            Total = src.Total;
            ProblemId = src.ProblemId;
            AllowSubmit = allowSubmit;
        }
    }
}
