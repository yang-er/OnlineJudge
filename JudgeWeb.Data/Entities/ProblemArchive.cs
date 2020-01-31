﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

    public partial class AppDbContext : IEntityTypeConfiguration<ProblemArchive>
    {
        public void Configure(EntityTypeBuilder<ProblemArchive> entity)
        {
            entity.HasKey(e => e.PublicId);

            entity.Property(e => e.PublicId)
                .ValueGeneratedNever();

            entity.HasIndex(e => e.ProblemId);

            entity.HasOne<Problem>()
                .WithMany()
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.TagName)
                .IsRequired();

            entity.Ignore(e => e.Title);

            entity.Ignore(e => e.Source);
        }
    }
}