using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 评测记录
    /// </summary>
    public class Judging
    {
        /// <summary>
        /// 评测记录编号
        /// </summary>
        public int JudgingId { get; set; }

        /// <summary>
        /// 是否为有效评测记录
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// 提交编号
        /// </summary>
        public int SubmissionId { get; set; }

        /// <summary>
        /// 是否完整测试每个样例
        /// </summary>
        public bool FullTest { get; set; }

        /// <summary>
        /// 评测开始时间
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// 评测结束时间
        /// </summary>
        public DateTimeOffset? StopTime { get; set; }

        /// <summary>
        /// 评测服务器名称
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// 评测结果
        /// </summary>
        public Verdict Status { get; set; }

        /// <summary>
        /// 执行时间，以ms为单位
        /// </summary>
        public int? ExecuteTime { get; set; }

        /// <summary>
        /// 执行内存，以kb为单位
        /// </summary>
        public int? ExecuteMemory { get; set; }

        /// <summary>
        /// 编译错误内容，以BASE64编码
        /// </summary>
        public string CompileError { get; set; }

        /// <summary>
        /// 重测请求编号
        /// </summary>
        public int? RejudgeId { get; set; }

        /// <summary>
        /// 重测时前一个活跃评测编号
        /// </summary>
        public int? PreviousJudgingId { get; set; }

        /// <summary>
        /// 评测点分数总和
        /// </summary>
        public int? TotalScore { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Judging>
    {
        public void Configure(EntityTypeBuilder<Judging> entity)
        {
            entity.HasKey(e => e.JudgingId);

            entity.HasOne<Submission>()
                .WithMany()
                .HasForeignKey(e => e.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<JudgeHost>()
                .WithMany()
                .HasForeignKey(e => e.Server)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Server)
                .IsUnicode(false)
                .HasMaxLength(64);

            entity.HasIndex(e => e.Status);

            entity.Property(e => e.CompileError)
                .IsUnicode(false)
                .HasMaxLength(131072);

            entity.HasOne<Rejudge>()
                .WithMany()
                .HasForeignKey(e => e.RejudgeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<Judging>()
                .WithMany()
                .HasForeignKey(e => e.PreviousJudgingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
