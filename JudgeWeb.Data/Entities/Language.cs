using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 编程语言
    /// </summary>
    public class Language
    {
        /// <summary>
        /// 语言编号
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 语言正式名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 保存的文件后缀名
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// 是否允许提交
        /// </summary>
        public bool AllowSubmit { get; set; }

        /// <summary>
        /// 是否允许评测
        /// </summary>
        public bool AllowJudge { get; set; }

        /// <summary>
        /// 时间倍数
        /// </summary>
        public double TimeFactor { get; set; }

        /// <summary>
        /// 编译脚本
        /// </summary>
        public string CompileScript { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Language>
    {
        public void Configure(EntityTypeBuilder<Language> entity)
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(16);

            entity.Property(e => e.Name)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(32);

            entity.Property(e => e.FileExtension)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(32);

            entity.Property(e => e.CompileScript)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(64);

            entity.HasOne<Executable>()
                .WithMany()
                .HasForeignKey(e => e.CompileScript)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
