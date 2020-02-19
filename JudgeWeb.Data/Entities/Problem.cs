using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
        /// 题目信息的标志
        /// </summary>
        public int Flag { get; set; }

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
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Problem>
    {
        public void Configure(EntityTypeBuilder<Problem> entity)
        {
            entity.HasKey(e => e.ProblemId);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(e => e.Source)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.RunScript)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(64);

            entity.HasOne<Executable>()
                .WithMany()
                .HasForeignKey(e => e.RunScript)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CompareScript)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(64);

            entity.HasOne<Executable>()
                .WithMany()
                .HasForeignKey(e => e.CompareScript)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.ComapreArguments)
                .IsUnicode(false)
                .HasMaxLength(128);
        }
    }
}
